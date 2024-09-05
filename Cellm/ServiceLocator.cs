using Cellm.AddIn;
using Cellm.Exceptions;
using Cellm.ModelProviders;
using ExcelDna.Integration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;
using System.Net;
using System.Threading.RateLimiting;

namespace Cellm;

internal static class ServiceLocator
{
    private static readonly Lazy<IServiceProvider> _serviceProvider = new(() => ConfigureServices(new ServiceCollection()).BuildServiceProvider());
    public static IServiceProvider ServiceProvider => _serviceProvider.Value;

    private static IServiceCollection ConfigureServices(IServiceCollection services)
    {
        // Configurations
        var basePath = ExcelDnaUtil.XllPathInfo?.Directory?.FullName ??
            throw new CellmException($"Unable to configure app, invalid value for ExcelDnaUtil.XllPathInfo='{ExcelDnaUtil.XllPathInfo}'");

        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.Local.json", true)
            .Build();

        services
            .Configure<CellmAddInConfiguration>(configuration.GetRequiredSection(nameof(CellmAddInConfiguration)))
            .Configure<AnthropicConfiguration>(configuration.GetRequiredSection(nameof(AnthropicConfiguration)))
            .Configure<GoogleConfiguration>(configuration.GetRequiredSection(nameof(GoogleConfiguration)))
            .Configure<OpenAiConfiguration>(configuration.GetRequiredSection(nameof(OpenAiConfiguration)));

        // Internals
        services
            .AddTransient<ArgumentParser>()
            .AddSingleton<IClientFactory, ClientFactory>()
            .AddSingleton<IClient, Client>()
            .AddSingleton<ICache, Cache>()
            .AddMemoryCache();

        // Model Providers
        var anthropicConfiguration = configuration.GetRequiredSection(nameof(AnthropicConfiguration)).Get<AnthropicConfiguration>()
            ?? throw new NullReferenceException(nameof(AnthropicConfiguration));

        services.AddHttpClient<AnthropicClient>(anthropicHttpClient =>
        {
            anthropicHttpClient.BaseAddress = anthropicConfiguration.BaseAddress;
            anthropicHttpClient.DefaultRequestHeaders.Add("x-api-key", anthropicConfiguration.ApiKey);
            anthropicHttpClient.DefaultRequestHeaders.Add("anthropic-version", anthropicConfiguration.Version);
        }).AddResilienceHandler("AnthropicResiliencePipeline", ConfigureResiliencePipeline);

        var googleGeminiConfiguration = configuration.GetRequiredSection(nameof(GoogleConfiguration)).Get<GoogleConfiguration>()
            ?? throw new NullReferenceException(nameof(GoogleConfiguration));

        services.AddHttpClient<GoogleClient>(googleGeminiHttpClient =>
        {
            googleGeminiHttpClient.BaseAddress = googleGeminiConfiguration.BaseAddress;
        }).AddResilienceHandler("GoogleGeminiResiliencePipeline", ConfigureResiliencePipeline);

        var openAiConfiguration = configuration.GetRequiredSection(nameof(OpenAiConfiguration)).Get<OpenAiConfiguration>()
            ?? throw new NullReferenceException(nameof(OpenAiConfiguration));

        services.AddHttpClient<OpenAiClient>(openAiHttpClient =>
        {
            openAiHttpClient.BaseAddress = openAiConfiguration.BaseAddress;
            openAiHttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {openAiConfiguration.ApiKey}");
        }).AddResilienceHandler("OpenAiResiliencePipeline", ConfigureResiliencePipeline);

        return services;
    }

    static void ConfigureResiliencePipeline(ResiliencePipelineBuilder<HttpResponseMessage> builder)
    {
        _ = builder
            .AddRateLimiter(new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = 60,
                QueueLimit = int.MaxValue,
                ReplenishmentPeriod = TimeSpan.FromSeconds(1),
                TokensPerPeriod = 1,
            }))
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
            {
                ShouldHandle = args => ValueTask.FromResult(ShouldBreakCircuit(args.Outcome)),
                SamplingDuration = TimeSpan.FromSeconds(30),
                FailureRatio = 0.5,
                MinimumThroughput = 10,
                BreakDuration = TimeSpan.FromSeconds(5),
            })
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                ShouldHandle = args => ValueTask.FromResult(ShouldRetry(args.Outcome)),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                MaxRetryAttempts = 8,
                Delay = TimeSpan.FromSeconds(1),
            })
            // Timeout for a single request
            .AddTimeout(TimeSpan.FromSeconds(10));
    }

    private static bool ShouldBreakCircuit(Outcome<HttpResponseMessage> outcome) => outcome switch
    {
        { Result: HttpResponseMessage response } => IsCircuitBreakerError(response),
        { Exception: Exception exception } => IsCircuitBreakerException(exception),
        _ => false
    };

    private static bool IsCircuitBreakerError(HttpResponseMessage response) =>
        response.StatusCode >= HttpStatusCode.InternalServerError ||
        response.StatusCode == HttpStatusCode.ServiceUnavailable ||
        response.StatusCode == HttpStatusCode.TooManyRequests;

    private static bool IsCircuitBreakerException(Exception exception) =>
        IsRetryableException(exception) || IsCatastrophicException(exception);

    private static bool IsCatastrophicException(Exception exception) =>
        exception is OutOfMemoryException or ThreadAbortException;

    private static bool ShouldRetry(Outcome<HttpResponseMessage> outcome) => outcome switch
    {
        { Result: HttpResponseMessage response } => IsRetryableError(response),
        { Exception: Exception exception } => IsRetryableException(exception),
        _ => false
    };

    private static bool IsRetryableError(HttpResponseMessage response) =>
        response.StatusCode >= HttpStatusCode.InternalServerError ||
        response.StatusCode == HttpStatusCode.RequestTimeout ||
        response.StatusCode == HttpStatusCode.TooManyRequests;

    private static bool IsRetryableException(Exception exception) =>
        exception is HttpRequestException or TimeoutRejectedException;

    public static T Get<T>() where T : notnull
    {
        return ServiceProvider.GetRequiredService<T>();
    }
}
