using System.ClientModel;
using System.ClientModel.Primitives;
using System.Threading.RateLimiting;
using Anthropic.SDK;
using Cellm.Models.Providers;
using Cellm.Models.Providers.Anthropic;
using Cellm.Models.Providers.DeepSeek;
using Cellm.Models.Providers.Llamafile;
using Cellm.Models.Providers.Mistral;
using Cellm.Models.Providers.Ollama;
using Cellm.Models.Providers.OpenAi;
using Cellm.Models.Providers.OpenAiCompatible;
using Cellm.Models.Resilience;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenAI;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace Cellm.Models;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers a factory for creating IChatClient instances with transient lifetime.
    /// Identical to AddKeyedChatClient from Microsft.Extensions.AI except this method 
    /// creates a new instance for each dependency injection request.
    /// 
    /// IChatClients are usually registered as singletons, but transient lifetimes are necessary 
    /// here because the user can change base address or api key at runtime and these settings  
    /// can only be passed via constructors.
    /// 
    /// The performance overhead of creating new instances is negligible, however, because
    /// execution time is completely dominated by model response time. 
    /// </summary>
    /// <param name="serviceCollection"></param>
    /// <param name="serviceKey"></param>
    /// <param name="innerClientFactory"></param>
    /// <returns>A ChatClientBuilder that can be further configured.</returns>
    public static ChatClientBuilder AddTransientKeyedChatClient(this IServiceCollection serviceCollection, object serviceKey, Func<IServiceProvider, IChatClient> innerClientFactory)
    {
        var builder = new ChatClientBuilder(innerClientFactory);
        serviceCollection.AddKeyedTransient(serviceKey, (IServiceProvider services, object? _) => builder.Build(services));
        return builder;
    }

    public static IServiceCollection AddRateLimiter(this IServiceCollection services, IConfiguration configuration)
    {
        var resilienceConfiguration = configuration
            .GetSection(nameof(ResilienceConfiguration))
            .Get<ResilienceConfiguration>()
            ?? throw new ArgumentException(nameof(ResilienceConfiguration));

        services.AddResiliencePipeline("RateLimiter", builder =>
        {
            builder
                .AddRateLimiter(new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
                {
                    QueueLimit = resilienceConfiguration.RateLimiterConfiguration.QueueLimit,
                    TokenLimit = resilienceConfiguration.RateLimiterConfiguration.TokenLimit,
                    ReplenishmentPeriod = TimeSpan.FromSeconds(resilienceConfiguration.RateLimiterConfiguration.ReplenishmentPeriodInSeconds),
                    TokensPerPeriod = resilienceConfiguration.RateLimiterConfiguration.TokensPerPeriod,
                }))
                .AddConcurrencyLimiter(new ConcurrencyLimiterOptions
                {
                    QueueLimit = resilienceConfiguration.RateLimiterConfiguration.QueueLimit,
                    PermitLimit = resilienceConfiguration.RateLimiterConfiguration.ConcurrencyLimit,

                })
                .Build();
        });

        return services;
    }

    public static IServiceCollection AddRetryHttpClient(this IServiceCollection services, IConfiguration configuration)
    {
        var resilienceConfiguration = configuration
            .GetSection(nameof(ResilienceConfiguration))
            .Get<ResilienceConfiguration>()
            ?? throw new ArgumentException(nameof(ResilienceConfiguration));

        services
            .AddHttpClient("ResilientHttpClient", resilientHttpClient =>
            {
                // Delegate timeout to resilience pipeline
                resilientHttpClient.Timeout = Timeout.InfiniteTimeSpan;
            })
            .AddAsKeyed()
            .AddResilienceHandler("RetryHttpClientHandler", builder =>
            {
                _ = builder
                    .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
                    {
                        ShouldHandle = args => ValueTask.FromResult(RetryHttpClientHelpers.ShouldRetry(args.Outcome)),
                        BackoffType = DelayBackoffType.Exponential,
                        UseJitter = true,
                        MaxRetryAttempts = resilienceConfiguration.RetryConfiguration.MaxRetryAttempts,
                        Delay = TimeSpan.FromSeconds(resilienceConfiguration.RetryConfiguration.DelayInSeconds),
                    })
                    .AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
                    {
                        ShouldHandle = args => ValueTask.FromResult(RetryHttpClientHelpers.ShouldBreakCircuit(args.Outcome)),
                        FailureRatio = resilienceConfiguration.CircuitBreakerConfiguration.FailureRatio,
                        SamplingDuration = TimeSpan.FromSeconds(resilienceConfiguration.CircuitBreakerConfiguration.SamplingDurationInSeconds),
                        MinimumThroughput = resilienceConfiguration.CircuitBreakerConfiguration.MinimumThroughput,
                        BreakDuration = TimeSpan.FromSeconds(resilienceConfiguration.CircuitBreakerConfiguration.BreakDurationInSeconds),
                    })
                    .AddTimeout(TimeSpan.FromSeconds(resilienceConfiguration.RetryConfiguration.HttpTimeoutInSeconds))
                    .Build();
            });

        return services;
    }

    public static IServiceCollection AddAnthropicChatClient(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddTransientKeyedChatClient(Provider.Anthropic, serviceProvider =>
            {
                var anthropicConfiguration = serviceProvider.GetRequiredService<IOptionsMonitor<AnthropicConfiguration>>();
                var resilientHttpClient = serviceProvider.GetKeyedService<HttpClient>("ResilientHttpClient") ?? throw new NullReferenceException("ResilientHttpClient");

                return new AnthropicClient(anthropicConfiguration.CurrentValue.ApiKey, resilientHttpClient)
                        .Messages
                        .AsBuilder()
                        .Build();
            })
            .UseFunctionInvocation();

        return services;
    }

    public static IServiceCollection AddOllamaChatClient(this IServiceCollection services)
    {
        services
            .AddTransientKeyedChatClient(Provider.Ollama, serviceProvider =>
            {
                var ollamaConfiguration = serviceProvider.GetRequiredService<IOptionsMonitor<OllamaConfiguration>>();
                var resilientHttpClient = serviceProvider.GetKeyedService<HttpClient>("ResilientHttpClient") ?? throw new NullReferenceException("ResilientHttpClient");

                return new OllamaChatClient(
                    ollamaConfiguration.CurrentValue.BaseAddress,
                    ollamaConfiguration.CurrentValue.DefaultModel,
                    resilientHttpClient);
            })
            .UseFunctionInvocation();

        return services;
    }

    public static IServiceCollection AddDeepSeekChatClient(this IServiceCollection services)
    {
        services
            .AddTransientKeyedChatClient(Provider.DeepSeek, serviceProvider =>
            {
                var deepSeekConfiguration = serviceProvider.GetRequiredService<IOptionsMonitor<DeepSeekConfiguration>>();
                var resilientHttpClient = serviceProvider.GetKeyedService<HttpClient>("ResilientHttpClient") ?? throw new NullReferenceException("ResilientHttpClient");

                var openAiClient = new OpenAIClient(
                    new ApiKeyCredential(deepSeekConfiguration.CurrentValue.ApiKey),
                    new OpenAIClientOptions
                    {
                        Transport = new HttpClientPipelineTransport(resilientHttpClient),
                        Endpoint = deepSeekConfiguration.CurrentValue.BaseAddress
                    });

                return openAiClient.GetChatClient(deepSeekConfiguration.CurrentValue.DefaultModel).AsIChatClient();
            })
            .UseFunctionInvocation();

        return services;
    }

    public static IServiceCollection AddLlamafileChatClient(this IServiceCollection services)
    {
        services
            .AddTransientKeyedChatClient(Provider.Llamafile, serviceProvider =>
            {
                var llamafileConfiguration = serviceProvider.GetRequiredService<IOptionsMonitor<LlamafileConfiguration>>();
                var resilientHttpClient = serviceProvider.GetKeyedService<HttpClient>("ResilientHttpClient") ?? throw new NullReferenceException("ResilientHttpClient");

                var openAiClient = new OpenAIClient(
                    new ApiKeyCredential(llamafileConfiguration.CurrentValue.ApiKey),
                    new OpenAIClientOptions
                    {
                        Transport = new HttpClientPipelineTransport(resilientHttpClient),
                        Endpoint = llamafileConfiguration.CurrentValue.BaseAddress
                    });

                return openAiClient.GetChatClient(llamafileConfiguration.CurrentValue.DefaultModel).AsIChatClient();
            })
            .UseFunctionInvocation();

        return services;
    }

    public static IServiceCollection AddMistralChatClient(this IServiceCollection services)
    {
        services
            .AddTransientKeyedChatClient(Provider.Mistral, serviceProvider =>
            {
                var mistralConfiguration = serviceProvider.GetRequiredService<IOptionsMonitor<MistralConfiguration>>();
                var resilientHttpClient = serviceProvider.GetKeyedService<HttpClient>("ResilientHttpClient") ?? throw new NullReferenceException("ResilientHttpClient");

                var openAiClient = new OpenAIClient(
                    new ApiKeyCredential(mistralConfiguration.CurrentValue.ApiKey),
                    new OpenAIClientOptions
                    {
                        Transport = new HttpClientPipelineTransport(resilientHttpClient),
                        Endpoint = mistralConfiguration.CurrentValue.BaseAddress
                    });

                return openAiClient.GetChatClient(mistralConfiguration.CurrentValue.DefaultModel).AsIChatClient();
            })
            .UseFunctionInvocation();

        return services;
    }

    public static IServiceCollection AddOpenAiChatClient(this IServiceCollection services)
    {
        services
            .AddTransientKeyedChatClient(Provider.OpenAi, serviceProvider =>
            {
                var openAiConfiguration = serviceProvider.GetRequiredService<IOptionsMonitor<OpenAiConfiguration>>();

                return new OpenAIClient(new ApiKeyCredential(openAiConfiguration.CurrentValue.ApiKey))
                    .GetChatClient(openAiConfiguration.CurrentValue.DefaultModel).AsIChatClient();
            })
            .UseFunctionInvocation();

        return services;
    }

    public static IServiceCollection AddOpenAiCompatibleChatClient(this IServiceCollection services)
    {
        services
            .AddTransientKeyedChatClient(Provider.OpenAiCompatible, serviceProvider =>
            {
                var openAiCompatibleConfiguration = serviceProvider.GetRequiredService<IOptionsMonitor<OpenAiCompatibleConfiguration>>();
                var resilientHttpClient = serviceProvider.GetKeyedService<HttpClient>("ResilientHttpClient") ?? throw new NullReferenceException("ResilientHttpClient");

                var openAiClient = new OpenAIClient(
                    new ApiKeyCredential(openAiCompatibleConfiguration.CurrentValue.ApiKey),
                    new OpenAIClientOptions
                    {
                        Transport = new HttpClientPipelineTransport(resilientHttpClient),
                        Endpoint = openAiCompatibleConfiguration.CurrentValue.BaseAddress
                    });

                return openAiClient.GetChatClient(openAiCompatibleConfiguration.CurrentValue.DefaultModel).AsIChatClient();
            })
            .UseFunctionInvocation();

        return services;
    }

    public static IServiceCollection AddTools(this IServiceCollection services, params Delegate[] tools)
    {

        foreach (var tool in tools)
        {
            services.AddSingleton(AIFunctionFactory.Create(tool));
        }

        return services;
    }

    public static IServiceCollection AddTools(this IServiceCollection services, params Func<IServiceProvider, AIFunction>[] toolBuilders)
    {
        foreach (var toolBuilder in toolBuilders)
        {
            services.AddSingleton((serviceProvider) => toolBuilder(serviceProvider));
        }

        return services;
    }
}
