using System.Reflection;
using Cellm.AddIn;
using Cellm.AddIn.Exceptions;
using Cellm.Models;
using Cellm.Models.Anthropic;
using Cellm.Models.GoogleAi;
using Cellm.Models.Llamafile;
using Cellm.Models.PipelineBehavior;
using Cellm.Models.OpenAi;
using Cellm.Services.Configuration;
using Cellm.Tools;
using ExcelDna.Integration;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sentry.Profiling;

namespace Cellm.Services;

internal static class ServiceLocator
{
    private static readonly Lazy<IServiceProvider> _serviceProvider = new(() => ConfigureServices(new ServiceCollection()).BuildServiceProvider());

    public static IServiceProvider ServiceProvider => _serviceProvider.Value;

    public static T Get<T>() where T : notnull
    {
        return ServiceProvider.GetRequiredService<T>();
    }

    private static IServiceCollection ConfigureServices(IServiceCollection services)
    {
        // Configurations
        var XllPath = ExcelDnaUtil.XllPathInfo?.Directory?.FullName ??
            throw new CellmException($"Unable to configure app, invalid value for ExcelDnaUtil.XllPathInfo='{ExcelDnaUtil.XllPathInfo}'");

        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(XllPath)
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.Local.json", true)
            .Build();

        services
            .Configure<CellmConfiguration>(configuration.GetRequiredSection(nameof(CellmConfiguration)))
            .Configure<AnthropicConfiguration>(configuration.GetRequiredSection(nameof(AnthropicConfiguration)))
            .Configure<GoogleAiConfiguration>(configuration.GetRequiredSection(nameof(GoogleAiConfiguration)))
            .Configure<OpenAiConfiguration>(configuration.GetRequiredSection(nameof(OpenAiConfiguration)))
            .Configure<LlamafileConfiguration>(configuration.GetRequiredSection(nameof(LlamafileConfiguration)))
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
                      sentryLoggingOptions.ExperimentalMetrics = new ExperimentalMetricsOptions { EnableCodeLocations = true };
                      sentryLoggingOptions.AddIntegration(new ProfilingIntegration());
                  });
          });

        // Internals
        services
            .AddSingleton(configuration)
            .AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()))
            .AddTransient(typeof(IPipelineBehavior<,>), typeof(ToolBehavior<,>))
            .AddMemoryCache()
            .AddTransient<ArgumentParser>()
            .AddSingleton<IClientFactory, ClientFactory>()
            .AddSingleton<IClient, Client>()
            .AddSingleton<ITools, Tools.Tools>()
            .AddSingleton<ICache, Cache>()
            .AddSingleton<ISerde, Serde>()
            .AddSingleton<LLamafileProcessManager>();

        // Model Providers
        var rateLimiterConfiguration = configuration.GetRequiredSection(nameof(RateLimiterConfiguration)).Get<RateLimiterConfiguration>()
            ?? throw new NullReferenceException(nameof(RateLimiterConfiguration));

        var circuitBreakerConfiguration = configuration.GetRequiredSection(nameof(CircuitBreakerConfiguration)).Get<CircuitBreakerConfiguration>()
            ?? throw new NullReferenceException(nameof(CircuitBreakerConfiguration));

        var retryConfiguration = configuration.GetRequiredSection(nameof(RetryConfiguration)).Get<RetryConfiguration>()
            ?? throw new NullReferenceException(nameof(RetryConfiguration));

        var resiliencePipelineConfigurator = new ResiliencePipelineConfigurator(
            rateLimiterConfiguration, circuitBreakerConfiguration, retryConfiguration);

        var anthropicConfiguration = configuration.GetRequiredSection(nameof(AnthropicConfiguration)).Get<AnthropicConfiguration>()
            ?? throw new NullReferenceException(nameof(AnthropicConfiguration));

        services.AddHttpClient<AnthropicRequestHandler>(anthropicHttpClient =>
        {
            anthropicHttpClient.BaseAddress = anthropicConfiguration.BaseAddress;
            anthropicHttpClient.DefaultRequestHeaders.Add("x-api-key", anthropicConfiguration.ApiKey);
            anthropicHttpClient.DefaultRequestHeaders.Add("anthropic-version", anthropicConfiguration.Version);
        }).AddResilienceHandler($"{nameof(AnthropicRequestHandler)}ResiliencePipeline", resiliencePipelineConfigurator.ConfigureResiliencePipeline);

        var googleAiConfiguration = configuration.GetRequiredSection(nameof(GoogleAiConfiguration)).Get<GoogleAiConfiguration>()
            ?? throw new NullReferenceException(nameof(GoogleAiConfiguration));

        services.AddHttpClient<GoogleAiClient>(googleHttpClient =>
        {
            googleHttpClient.BaseAddress = googleAiConfiguration.BaseAddress;
        }).AddResilienceHandler($"{nameof(GoogleAiClient)}ResiliencePipeline", resiliencePipelineConfigurator.ConfigureResiliencePipeline);

        var openAiConfiguration = configuration.GetRequiredSection(nameof(OpenAiConfiguration)).Get<OpenAiConfiguration>()
            ?? throw new NullReferenceException(nameof(OpenAiConfiguration));

        services.AddHttpClient<IRequestHandler<OpenAiRequest, OpenAiResponse>, OpenAiRequestHandler>(openAiHttpClient =>
        {
            openAiHttpClient.BaseAddress = openAiConfiguration.BaseAddress;
            openAiHttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {openAiConfiguration.ApiKey}");
        }).AddResilienceHandler($"{nameof(OpenAiRequestHandler)}ResiliencePipeline", resiliencePipelineConfigurator.ConfigureResiliencePipeline);

        services.AddSingleton<LlamafileClient>();

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
