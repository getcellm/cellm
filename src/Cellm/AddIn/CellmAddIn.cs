using System.Reflection;
using Cellm.AddIn.Configuration;
using Cellm.AddIn.Exceptions;
using Cellm.Models;
using Cellm.Models.Behaviors;
using Cellm.Models.Providers;
using Cellm.Models.Providers.Anthropic;
using Cellm.Models.Providers.Aws;
using Cellm.Models.Providers.Azure;
using Cellm.Models.Providers.Behaviors;
using Cellm.Models.Providers.Cellm;
using Cellm.Models.Providers.DeepSeek;
using Cellm.Models.Providers.Google;
using Cellm.Models.Providers.Mistral;
using Cellm.Models.Providers.Ollama;
using Cellm.Models.Providers.OpenAi;
using Cellm.Models.Providers.OpenAiCompatible;
using Cellm.Models.Resilience;
using Cellm.Tools;
using Cellm.Tools.FileReader;
using Cellm.Tools.ModelContextProtocol;
using Cellm.Users;
using ExcelDna.Integration;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;

namespace Cellm.AddIn;

public class CellmAddIn : IExcelAddIn
{
    internal static string ConfigurationPath => ExcelDnaUtil.XllPathInfo?.Directory?.FullName ?? throw new NullReferenceException("Could not get Cellm path");

    private static readonly Lazy<ServiceProvider> _serviceProvider = new(() => ConfigureServices(new ServiceCollection()).BuildServiceProvider());

    public static ServiceProvider Services => _serviceProvider.Value;


    public void AutoOpen()
    {
        ExcelIntegration.RegisterUnhandledExceptionHandler(obj =>
        {
            var e = (Exception)obj;
            SentrySdk.CaptureException(e);
            return e.Message;
        });
    }

    public void AutoClose()
    {
        SentrySdk.Flush();
        DisposeServices();
    }

    private static ServiceCollection ConfigureServices(ServiceCollection services)
    {
        if (string.IsNullOrEmpty(ConfigurationPath))
        {
            throw new CellmException($"Unable to configure app, invalid value for ExcelDnaUtil.XllPathInfo='{ConfigurationPath}'");
        }

        // Configurations
        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(ConfigurationPath)
            .AddJsonFile("appsettings.json", reloadOnChange: true, optional: false)
            .AddJsonFile("appsettings.Local.json", reloadOnChange: true, optional: true)
            .Build();

        services
            .Configure<AccountConfiguration>(configuration.GetRequiredSection(nameof(AccountConfiguration)))
            .Configure<AnthropicConfiguration>(configuration.GetRequiredSection(nameof(AnthropicConfiguration)))
            .Configure<AwsConfiguration>(configuration.GetRequiredSection(nameof(AwsConfiguration)))
            .Configure<AzureConfiguration>(configuration.GetRequiredSection(nameof(AzureConfiguration)))
            .Configure<CellmConfiguration>(configuration.GetRequiredSection(nameof(CellmConfiguration)))
            .Configure<GeminiConfiguration>(configuration.GetRequiredSection(nameof(GeminiConfiguration)))
            .Configure<CellmAddInConfiguration>(configuration.GetRequiredSection(nameof(CellmAddInConfiguration)))
            .Configure<DeepSeekConfiguration>(configuration.GetRequiredSection(nameof(DeepSeekConfiguration)))
            .Configure<MistralConfiguration>(configuration.GetRequiredSection(nameof(MistralConfiguration)))
            .Configure<ModelContextProtocolConfiguration>(configuration.GetRequiredSection(nameof(ModelContextProtocolConfiguration)))
            .Configure<OllamaConfiguration>(configuration.GetRequiredSection(nameof(OllamaConfiguration)))
            .Configure<OpenAiConfiguration>(configuration.GetRequiredSection(nameof(OpenAiConfiguration)))
            .Configure<OpenAiCompatibleConfiguration>(configuration.GetRequiredSection(nameof(OpenAiCompatibleConfiguration)))
            .Configure<ResilienceConfiguration>(configuration.GetRequiredSection(nameof(ResilienceConfiguration)))
            .Configure<SentryConfiguration>(configuration.GetRequiredSection(nameof(SentryConfiguration)));

        // Logging
        var sentryConfiguration = configuration.GetRequiredSection(nameof(SentryConfiguration)).Get<SentryConfiguration>()
            ?? throw new NullReferenceException(nameof(SentryConfiguration));

        var cellmAddInConfiguration = configuration.GetRequiredSection(nameof(CellmAddInConfiguration)).Get<CellmAddInConfiguration>()
            ?? throw new NullReferenceException(nameof(CellmAddInConfiguration));

        services
          .AddLogging(loggingBuilder =>
          {
              loggingBuilder
                  .AddConfiguration(configuration.GetSection("Logging"))
                  .AddConsole()
                  .AddDebug();

              if (cellmAddInConfiguration.EnableFileLogging)
              {
                  var logPath = Path.Combine(ConfigurationPath, "logs", "cellm-.log");
                  var serilogLogger = new LoggerConfiguration()
                      .MinimumLevel.Debug()
                      .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
                      .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Warning)
                      .WriteTo.File(
                          path: logPath,
                          rollingInterval: RollingInterval.Day,
                          fileSizeLimitBytes: cellmAddInConfiguration.LogFileSizeLimitMegabyte * 1024 * 1024,
                          retainedFileCountLimit: cellmAddInConfiguration.LogFileRetainedCount,
                          rollOnFileSizeLimit: true)
                      .CreateLogger();

                  loggingBuilder.AddSerilog(serilogLogger);
              }

              loggingBuilder.AddSentry(sentryLoggingOptions =>
              {
                  sentryLoggingOptions.InitializeSdk = sentryConfiguration.IsEnabled;
                  sentryLoggingOptions.Release = GetReleaseVersion();
                  sentryLoggingOptions.Environment = sentryConfiguration.Environment;
                  sentryLoggingOptions.Dsn = sentryConfiguration.Dsn;
                  sentryLoggingOptions.Debug = sentryConfiguration.Debug;
                  sentryLoggingOptions.TracesSampleRate = sentryConfiguration.TracesSampleRate;
                  sentryLoggingOptions.ProfilesSampleRate = sentryConfiguration.ProfilesSampleRate;
                  sentryLoggingOptions.Environment = sentryConfiguration.Environment;
                  sentryLoggingOptions.AutoSessionTracking = true;
                  sentryLoggingOptions.AddIntegration(new ProfilingIntegration());
              });
          });

        // Mediatr
        services
            .AddMediatR(cfg =>
            {
                cfg.LicenseKey = cellmAddInConfiguration.MediatrLicenseKey;
                cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
                cfg.AddBehavior<SentryBehavior<ProviderRequest, ProviderResponse>>(ServiceLifetime.Singleton);
                cfg.AddBehavior<ToolBehavior<ProviderRequest, ProviderResponse>>(ServiceLifetime.Singleton);
                cfg.AddBehavior<ProviderBehavior<ProviderRequest, ProviderResponse>>(ServiceLifetime.Singleton);
                cfg.AddBehavior<CacheBehavior<ProviderRequest, ProviderResponse>>(ServiceLifetime.Singleton);
                cfg.AddBehavior<UsageBehavior<ProviderRequest, ProviderResponse>>(ServiceLifetime.Singleton);
            })
            .AddSingleton<IProviderBehavior, AdditionalPropertiesBehavior>()
            .AddSingleton<IProviderBehavior, GeminiTemperatureBehavior>()
            .AddSingleton<IProviderBehavior, OpenAiTemperatureBehavior>()
            .AddSingleton<IProviderBehavior, MistralThinkingBehavior>();

        // Internals
        var resilienceConfiguration = configuration.GetRequiredSection(nameof(ResilienceConfiguration)).Get<ResilienceConfiguration>()
            ?? throw new NullReferenceException(nameof(SentryConfiguration));

        services
            .AddSingleton(configuration)
            .AddTransient<ArgumentParser>()
            .AddSingleton<Account>()
            .AddSingleton<Client>()
            .AddRateLimiter(resilienceConfiguration)
            .AddResilientHttpClient(resilienceConfiguration, cellmAddInConfiguration, Provider.Anthropic)
            .AddResilientHttpClient(resilienceConfiguration, cellmAddInConfiguration, Provider.Cellm)
            .AddResilientHttpClient(resilienceConfiguration, cellmAddInConfiguration, Provider.DeepSeek)
            .AddResilientHttpClient(resilienceConfiguration, cellmAddInConfiguration, Provider.Gemini)
            .AddResilientHttpClient(resilienceConfiguration, cellmAddInConfiguration, Provider.Mistral)
            .AddResilientHttpClient(resilienceConfiguration, cellmAddInConfiguration, Provider.OpenAiCompatible);

#pragma warning disable EXTEXP0018 // Type is for evaluation purposes only and is subject to change or removal in future updates.
        services
            .AddHybridCache();
#pragma warning restore EXTEXP0018 // Type is for evaluation purposes only and is subject to change or removal in future updates.

        // Add providers
        services
            .AddSingleton<IChatClientFactory, ChatClientFactory>()
            .AddAnthropicChatClient()
            .AddAwsChatClient()
            .AddAzureChatClient()
            .AddCellmChatClient()
            .AddDeepSeekChatClient()
            .AddGeminiChatClient()
            .AddMistralChatClient()
            .AddOllamaChatClient()
            .AddOpenAiChatClient()
            .AddOpenAiCompatibleChatClient();

        // Add tools
        services
            .AddSingleton<FileReaderFactory>()
            .AddSingleton<IFileReader, PdfReader>()
            .AddSingleton<IFileReader, TextReader>()
            .AddSingleton<NativeTools>()
            .AddTools(
                serviceProvider => AIFunctionFactory.Create(serviceProvider.GetRequiredService<NativeTools>().FileSearchRequest),
                serviceProvider => AIFunctionFactory.Create(serviceProvider.GetRequiredService<NativeTools>().FileReaderRequest))
            .AddSingleton<IMcpConfigurationService, McpConfigurationService>();

        // Workarounds

        // https://github.com/openai/openai-dotnet/issues/297
        var metricsFilterDescriptor = services.FirstOrDefault(descriptor =>
            descriptor.ImplementationType?.ToString() == $"Microsoft.Extensions.Http.MetricsFactoryHttpMessageHandlerFilter");

        if (metricsFilterDescriptor is not null)
        {
            services.Remove(metricsFilterDescriptor);
        }

        return services;
    }

    internal static IProviderConfiguration GetProviderConfiguration(Provider provider)
    {
        return GetProviderConfigurations().Single(providerConfigurations => providerConfigurations.Id == provider);
    }

    internal static IEnumerable<IProviderConfiguration> GetProviderConfigurations()
    {
        return
        [
            // Retrieve the current, up-to-date configuration for each provider
            // Until we find a better way to inject up-to-date configuration
            Services.GetRequiredService<IOptionsMonitor<AnthropicConfiguration>>().CurrentValue,
            Services.GetRequiredService<IOptionsMonitor<AwsConfiguration>>().CurrentValue,
            Services.GetRequiredService<IOptionsMonitor<AzureConfiguration>>().CurrentValue,
            Services.GetRequiredService<IOptionsMonitor<CellmConfiguration>>().CurrentValue,
            Services.GetRequiredService<IOptionsMonitor<DeepSeekConfiguration>>().CurrentValue,
            Services.GetRequiredService<IOptionsMonitor<GeminiConfiguration>>().CurrentValue,
            Services.GetRequiredService<IOptionsMonitor<MistralConfiguration>>().CurrentValue,
            Services.GetRequiredService<IOptionsMonitor<OllamaConfiguration>>().CurrentValue,
            Services.GetRequiredService<IOptionsMonitor<OpenAiConfiguration>>().CurrentValue,
            Services.GetRequiredService<IOptionsMonitor<OpenAiCompatibleConfiguration>>().CurrentValue
        ];
    }

    public static string GetReleaseVersion()
    {
        return Assembly.GetExecutingAssembly()?
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion ?? "unknown";
    }

    public static void DisposeServices()
    {
        if (_serviceProvider.IsValueCreated)
        {
            (_serviceProvider.Value as IDisposable)?.Dispose();
        }
    }

}
