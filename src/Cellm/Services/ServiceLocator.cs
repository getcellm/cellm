using Cellm.AddIn;
using Cellm.AddIn.Exceptions;
using Cellm.Models;
using Cellm.Models.Anthropic;
using Cellm.Models.Google;
using Cellm.Models.OpenAi;
using Cellm.Services.Configuration;
using ExcelDna.Integration;
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
            .Configure<GoogleConfiguration>(configuration.GetRequiredSection(nameof(GoogleConfiguration)))
            .Configure<OpenAiConfiguration>(configuration.GetRequiredSection(nameof(OpenAiConfiguration)))
            .Configure<RateLimiterConfiguration>(configuration.GetRequiredSection(nameof(RateLimiterConfiguration)))
            .Configure<CircuitBreakerConfiguration>(configuration.GetRequiredSection(nameof(CircuitBreakerConfiguration)))
            .Configure<RetryConfiguration>(configuration.GetRequiredSection(nameof(RetryConfiguration)))
            .Configure<SentryConfiguration>(configuration.GetRequiredSection(nameof(SentryConfiguration)));

        // Logging
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
                      sentryLoggingOptions.Debug = sentryConfiguration.Debug;
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
            .AddMemoryCache()
            .AddTransient<ArgumentParser>()
            .AddSingleton<IClientFactory, ClientFactory>()
            .AddSingleton<IClient, Client>()
            .AddSingleton<ICache, Cache>()
            .AddSingleton<ISerde, Serde>();

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

        services.AddHttpClient<AnthropicClient>(anthropicHttpClient =>
        {
            anthropicHttpClient.BaseAddress = anthropicConfiguration.BaseAddress;
            anthropicHttpClient.DefaultRequestHeaders.Add("x-api-key", anthropicConfiguration.ApiKey);
            anthropicHttpClient.DefaultRequestHeaders.Add("anthropic-version", anthropicConfiguration.Version);
        }).AddResilienceHandler("AnthropicResiliencePipeline", resiliencePipelineConfigurator.ConfigureResiliencePipeline);

        var googleConfiguration = configuration.GetRequiredSection(nameof(GoogleConfiguration)).Get<GoogleConfiguration>()
            ?? throw new NullReferenceException(nameof(GoogleConfiguration));

        services.AddHttpClient<GoogleClient>(googleHttpClient =>
        {
            googleHttpClient.BaseAddress = googleConfiguration.BaseAddress;
        }).AddResilienceHandler("GoogleResiliencePipeline", resiliencePipelineConfigurator.ConfigureResiliencePipeline);

        var openAiConfiguration = configuration.GetRequiredSection(nameof(OpenAiConfiguration)).Get<OpenAiConfiguration>()
            ?? throw new NullReferenceException(nameof(OpenAiConfiguration));

        services.AddHttpClient<OpenAiClient>(openAiHttpClient =>
        {
            openAiHttpClient.BaseAddress = openAiConfiguration.BaseAddress;
            openAiHttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {openAiConfiguration.ApiKey}");
        }).AddResilienceHandler("OpenAiResiliencePipeline", resiliencePipelineConfigurator.ConfigureResiliencePipeline);

        return services;
    }
}
