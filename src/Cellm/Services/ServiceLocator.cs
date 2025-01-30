using System.Reflection;
using Cellm.AddIn;
using Cellm.AddIn.Exceptions;
using Cellm.Models;
using Cellm.Models.Providers;
using Cellm.Models.Providers.Anthropic;
using Cellm.Models.Providers.DeepSeek;
using Cellm.Models.Providers.Llamafile;
using Cellm.Models.Providers.Mistral;
using Cellm.Models.Providers.Ollama;
using Cellm.Models.Providers.OpenAi;
using Cellm.Models.Providers.OpenAiCompatible;
using Cellm.Models.Resilience;
using Cellm.Services.Configuration;
using Cellm.Tools;
using Cellm.Tools.FileReader;
using ExcelDna.Integration;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cellm.Services;

internal static class ServiceLocator
{
    private static readonly Lazy<IServiceProvider> _serviceProvider = new(() => ConfigureServices(new ServiceCollection()).BuildServiceProvider());

    internal static string ConfigurationPath { get; set; } = ExcelDnaUtil.XllPathInfo?.Directory?.FullName ?? throw new NullReferenceException("Could not get Cellm path");

    public static IServiceProvider ServiceProvider => _serviceProvider.Value;

    public static T Get<T>() where T : notnull
    {
        return ServiceProvider.GetRequiredService<T>();
    }

    private static IServiceCollection ConfigureServices(IServiceCollection services)
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
            .Configure<CellmConfiguration>(configuration.GetRequiredSection(nameof(CellmConfiguration)))
            .Configure<ProviderConfiguration>(configuration.GetRequiredSection(nameof(ProviderConfiguration)))
            .Configure<AnthropicConfiguration>(configuration.GetRequiredSection(nameof(AnthropicConfiguration)))
            .Configure<DeepSeekConfiguration>(configuration.GetRequiredSection(nameof(DeepSeekConfiguration)))
            .Configure<LlamafileConfiguration>(configuration.GetRequiredSection(nameof(LlamafileConfiguration)))
            .Configure<OllamaConfiguration>(configuration.GetRequiredSection(nameof(OllamaConfiguration)))
            .Configure<OpenAiConfiguration>(configuration.GetRequiredSection(nameof(OpenAiConfiguration)))
            .Configure<OpenAiCompatibleConfiguration>(configuration.GetRequiredSection(nameof(OpenAiCompatibleConfiguration)))
            .Configure<MistralConfiguration>(configuration.GetRequiredSection(nameof(MistralConfiguration)))
            .Configure<RateLimiterConfiguration>(configuration.GetRequiredSection(nameof(RateLimiterConfiguration)))
            .Configure<CircuitBreakerConfiguration>(configuration.GetRequiredSection(nameof(CircuitBreakerConfiguration)))
            .Configure<RetryConfiguration>(configuration.GetRequiredSection(nameof(RetryConfiguration)))
            .Configure<SentryConfiguration>(configuration.GetRequiredSection(nameof(SentryConfiguration)));

        // Logging
        var cellmConfiguration = configuration.GetRequiredSection(nameof(CellmConfiguration)).Get<CellmConfiguration>()
            ?? throw new NullReferenceException(nameof(CellmConfiguration));

        var sentryConfiguration = configuration.GetRequiredSection(nameof(SentryConfiguration)).Get<SentryConfiguration>()
            ?? throw new NullReferenceException(nameof(SentryConfiguration));

        services
          .AddLogging(loggingBuilder =>
          {
              loggingBuilder
                  .AddConsole()
                  .AddDebug()
                  .AddSentry(sentryLoggingOptions =>
                  {
                      sentryLoggingOptions.InitializeSdk = sentryConfiguration.IsEnabled;
                      sentryLoggingOptions.Dsn = sentryConfiguration.Dsn;
                      sentryLoggingOptions.Debug = cellmConfiguration.Debug;
                      sentryLoggingOptions.DiagnosticLevel = SentryLevel.Debug;
                      sentryLoggingOptions.TracesSampleRate = sentryConfiguration.TracesSampleRate;
                      sentryLoggingOptions.ProfilesSampleRate = sentryConfiguration.ProfilesSampleRate;
                      sentryLoggingOptions.Environment = sentryConfiguration.Environment;
                      sentryLoggingOptions.AutoSessionTracking = true;
                      sentryLoggingOptions.IsGlobalModeEnabled = true;
                      sentryLoggingOptions.AddIntegration(new ProfilingIntegration());
                  });
          });

        // Internals
        services
            .AddMediatR(mediatrConfiguration => mediatrConfiguration.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()))
            .AddSingleton(configuration)
            .AddTransient<ArgumentParser>()
            .AddSingleton<Client>()
            .AddSingleton<Serde>();

        // Add providers
        services
            .AddAnthropicChatClient(configuration)
            .AddOpenAiChatClient(configuration)
            .AddOpenAiCompatibleChatClient(configuration)
            .AddOpenOllamaChatClient(configuration);

        // Add provider middleware
        services
            .AddSentryBehavior()
            .AddCachingBehavior()
            .AddToolBehavior();

        // Add tools
        services
            .AddSingleton<FileReaderFactory>()
            .AddSingleton<IFileReader, PdfReader>()
            .AddSingleton<IFileReader, TextReader>()
            .AddSingleton<Functions>()
            .AddTools(
                serviceProvider => AIFunctionFactory.Create(serviceProvider.GetRequiredService<Functions>().GlobRequest),
                serviceProvider => AIFunctionFactory.Create(serviceProvider.GetRequiredService<Functions>().FileReaderRequest));

        return services;
    }

    public static void Dispose()
    {
        if (_serviceProvider.IsValueCreated)
        {
            (_serviceProvider.Value as IDisposable)?.Dispose();
        }
    }
}
