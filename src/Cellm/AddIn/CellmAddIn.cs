using System.Reflection;
using Cellm.AddIn.Configuration;
using Cellm.AddIn.Exceptions;
using Cellm.Models;
using Cellm.Models.Behaviors;
using Cellm.Models.Providers;
using Cellm.Models.Providers.Anthropic;
using Cellm.Models.Providers.Cellm;
using Cellm.Models.Providers.DeepSeek;
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
using Sentry.Infrastructure;

namespace Cellm.AddIn;

public class CellmAddIn : IExcelAddIn
{
    internal static string ConfigurationPath => ExcelDnaUtil.XllPathInfo?.Directory?.FullName ?? throw new NullReferenceException("Could not get Cellm path");

    private static readonly Lazy<IServiceProvider> _serviceProvider = new(() => ConfigureServices(new ServiceCollection()).BuildServiceProvider());
    public static IServiceProvider Services => _serviceProvider.Value;


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
        DisposeServices();
        SentrySdk.Flush();
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
            .AddJsonFile("appsettings.json", reloadOnChange: true, optional: true)
            .AddJsonFile("appsettings.Local.json", reloadOnChange: true, optional: true)
            .Build();

        services
            .Configure<AccountConfiguration>(configuration.GetSection(nameof(AccountConfiguration)))
            .Configure<AnthropicConfiguration>(configuration.GetSection(nameof(AnthropicConfiguration)))
            .Configure<CellmConfiguration>(configuration.GetSection(nameof(CellmConfiguration)))
            .Configure<DeepSeekConfiguration>(configuration.GetSection(nameof(DeepSeekConfiguration)))
            .Configure<ProviderConfiguration>(configuration.GetSection(nameof(ProviderConfiguration)))
            .Configure<MistralConfiguration>(configuration.GetSection(nameof(MistralConfiguration)))
            .Configure<ModelContextProtocolConfiguration>(configuration.GetSection(nameof(ModelContextProtocolConfiguration)))
            .Configure<OllamaConfiguration>(configuration.GetSection(nameof(OllamaConfiguration)))
            .Configure<OpenAiConfiguration>(configuration.GetSection(nameof(OpenAiConfiguration)))
            .Configure<OpenAiCompatibleConfiguration>(configuration.GetSection(nameof(OpenAiCompatibleConfiguration)))
            .Configure<ResilienceConfiguration>(configuration.GetSection(nameof(ResilienceConfiguration)))
            .Configure<SentryConfiguration>(configuration.GetSection(nameof(SentryConfiguration)));

        // Build a temporary service provider to resolve registered configurations
        var configurationProvider = services.BuildServiceProvider();

        // Logging
        var sentryConfiguration = configurationProvider.GetRequiredService<IOptions<SentryConfiguration>>();

        services
          .AddLogging(loggingBuilder =>
          {
              loggingBuilder
                  .AddConfiguration(configuration.GetSection("Logging"))
                  .AddConsole()
                  .AddDebug()
                  .AddSentry(sentryLoggingOptions =>
                  {
                      sentryLoggingOptions.InitializeSdk = sentryConfiguration.Value.IsEnabled;
                      sentryLoggingOptions.Release = GetReleaseVersion();
                      sentryLoggingOptions.Environment = sentryConfiguration.Value.Environment;
                      sentryLoggingOptions.Dsn = sentryConfiguration.Value.Dsn;
                      sentryLoggingOptions.Debug = sentryConfiguration.Value.Debug;
                      sentryLoggingOptions.DiagnosticLevel = SentryLevel.Debug;
                      sentryLoggingOptions.DiagnosticLogger = new TraceDiagnosticLogger(SentryLevel.Debug);
                      sentryLoggingOptions.TracesSampleRate = sentryConfiguration.Value.TracesSampleRate;
                      sentryLoggingOptions.ProfilesSampleRate = sentryConfiguration.Value.ProfilesSampleRate;
                      sentryLoggingOptions.Environment = sentryConfiguration.Value.Environment;
                      sentryLoggingOptions.AutoSessionTracking = true;
                      sentryLoggingOptions.AddIntegration(new ProfilingIntegration());
                  });
          });

        // Internals
        services
            .AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
                cfg.AddBehavior(typeof(SentryBehavior<ProviderRequest, ProviderResponse>), ServiceLifetime.Singleton);
                cfg.AddBehavior(typeof(ToolBehavior<ProviderRequest, ProviderResponse>), ServiceLifetime.Singleton);
                cfg.AddBehavior(typeof(CacheBehavior<ProviderRequest, ProviderResponse>), ServiceLifetime.Singleton);
            })
            .AddSingleton(configuration)
            .AddTransient<ArgumentParser>()
            .AddSingleton<Account>()
            .AddSingleton<Client>()
            .AddRateLimiter(configurationProvider)
            .AddRetryHttpClient(configurationProvider);

#pragma warning disable EXTEXP0018 // Type is for evaluation purposes only and is subject to change or removal in future updates.
        services
            .AddHybridCache();
#pragma warning restore EXTEXP0018 // Type is for evaluation purposes only and is subject to change or removal in future updates.

        // Add providers
        services
            .AddSingleton<IChatClientFactory, ChatClientFactory>()
            .AddAnthropicChatClient()
            .AddCellmChatClient()
            .AddDeepSeekChatClient()
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
                serviceProvider => AIFunctionFactory.Create(serviceProvider.GetRequiredService<NativeTools>().FileReaderRequest));

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
