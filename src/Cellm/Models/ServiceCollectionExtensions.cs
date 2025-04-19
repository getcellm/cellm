// Circumventing limitations anywhere in the codebase is a direct violation of the
// Fair Core License, Version 1.0 until a commit transitions to the Apache 2.0 Future License.
// Once a commit transitions to the Apache 2.0 Future License, you can checkout out
// that commit and use the code under the Apache 2.0 License. A commit transitions to
// the Apache 2.0 Future License on the second anniversary of the date the git commit
// became available. 
//
// The relevant section of the Fair Core License, Version 1.0 is:
//
// > ### Limitations
// > You must not move, change, disable, or circumvent the license key functionality
// > in the Software; or modify any portion of the Software protected by the license
// > key to:
//
// > 1. enable access to the protected functionality without a valid license key; or
//
// > 2. remove the protected functionality.
//
// You can checkout the latest commit licensed under the Apache 2.0 License like this:
// 
// $ git checkout $(git rev-list -n 1 --before="2 years ago" HEAD)
//
// For more details, go to https://github.com/getcellm/cellm/blob/main/LICENSE.

using System.ClientModel;
using System.ClientModel.Primitives;
using System.Threading.RateLimiting;
using Anthropic.SDK;
using Cellm.Models.Providers;
using Cellm.Models.Providers.Anthropic;
using Cellm.Models.Providers.Cellm;
using Cellm.Models.Providers.DeepSeek;
using Cellm.Models.Providers.Mistral;
using Cellm.Models.Providers.Ollama;
using Cellm.Models.Providers.OpenAi;
using Cellm.Models.Providers.OpenAiCompatible;
using Cellm.Models.Resilience;
using Cellm.Users;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Mistral.SDK;
using OpenAI;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace Cellm.Models;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRateLimiter(this IServiceCollection services, IConfiguration configuration)
    {
        var resilienceConfiguration = configuration
            .GetSection(nameof(ResilienceConfiguration))
            .Get<ResilienceConfiguration>()
            ?? throw new ArgumentException(nameof(ResilienceConfiguration));

        return services.AddResiliencePipeline("RateLimiter", builder =>
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

    public static IServiceCollection AddAnthropicChatClient(this IServiceCollection services)
    {
        services
            .AddKeyedChatClient(Provider.Anthropic, serviceProvider =>
            {
                var account = serviceProvider.GetRequiredService<Account>();
                account.RequireEntitlement(Entitlement.EnableAnthropicProvider);

                var anthropicConfiguration = serviceProvider.GetRequiredService<IOptionsMonitor<AnthropicConfiguration>>();
                var resilientHttpClient = serviceProvider.GetKeyedService<HttpClient>("ResilientHttpClient") ?? throw new NullReferenceException("ResilientHttpClient");

                return new AnthropicClient(anthropicConfiguration.CurrentValue.ApiKey, resilientHttpClient)
                    .Messages
                    .AsBuilder()
                    .Build();
            }, ServiceLifetime.Transient)
            .UseFunctionInvocation();

        return services;
    }

    public static IServiceCollection AddCellmChatClient(this IServiceCollection services)
    {
        services
            .AddKeyedChatClient(Provider.Cellm, serviceProvider =>
            {
                var account = serviceProvider.GetRequiredService<Account>();
                account.RequireEntitlement(Entitlement.EnableCellmProvider);

                var cellmConfiguration = serviceProvider.GetRequiredService<IOptionsMonitor<CellmConfiguration>>();
                var resilientHttpClient = serviceProvider.GetKeyedService<HttpClient>("ResilientHttpClient") ?? throw new NullReferenceException("ResilientHttpClient");
                resilientHttpClient.DefaultRequestHeaders.Add("Authorization", $"Basic {account.GetBasicAuthCredentials()}");

                var openAiClient = new OpenAIClient(
                    new ApiKeyCredential(string.Empty),
                    new OpenAIClientOptions
                    {
                        Transport = new HttpClientPipelineTransport(resilientHttpClient),
                        Endpoint = cellmConfiguration.CurrentValue.BaseAddress
                    });

                return openAiClient.GetChatClient(cellmConfiguration.CurrentValue.DefaultModel).AsIChatClient();
            }, ServiceLifetime.Transient)
            .UseFunctionInvocation();

        return services;
    }

    public static IServiceCollection AddDeepSeekChatClient(this IServiceCollection services)
    {
        services
            .AddKeyedChatClient(Provider.DeepSeek, serviceProvider =>
            {
                var account = serviceProvider.GetRequiredService<Account>();
                account.RequireEntitlement(Entitlement.EnableDeepSeekProvider);

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
            }, ServiceLifetime.Transient)
            .UseFunctionInvocation();

        return services;
    }

    public static IServiceCollection AddMistralChatClient(this IServiceCollection services)
    {
        services
            .AddKeyedChatClient(Provider.Mistral, serviceProvider =>
            {
                var account = serviceProvider.GetRequiredService<Account>();
                account.RequireEntitlement(Entitlement.EnableMistralProvider);

                var mistralConfiguration = serviceProvider.GetRequiredService<IOptionsMonitor<MistralConfiguration>>();
                var resilientHttpClient = serviceProvider.GetKeyedService<HttpClient>("ResilientHttpClient") ?? throw new NullReferenceException("ResilientHttpClient");

                return new MistralClient(mistralConfiguration.CurrentValue.ApiKey, resilientHttpClient)
                    .Completions
                    .AsBuilder()
                    .Build();
            }, ServiceLifetime.Transient)
            .UseFunctionInvocation();

        return services;
    }

    public static IServiceCollection AddOllamaChatClient(this IServiceCollection services)
    {
        services
            .AddKeyedChatClient(Provider.Ollama, serviceProvider =>
            {
                var account = serviceProvider.GetRequiredService<Account>();
                account.RequireEntitlement(Entitlement.EnableOllamaProvider);

                var ollamaConfiguration = serviceProvider.GetRequiredService<IOptionsMonitor<OllamaConfiguration>>();
                var resilientHttpClient = serviceProvider.GetKeyedService<HttpClient>("ResilientHttpClient") ?? throw new NullReferenceException("ResilientHttpClient");

                return new OllamaChatClient(
                    ollamaConfiguration.CurrentValue.BaseAddress,
                    ollamaConfiguration.CurrentValue.DefaultModel,
                    resilientHttpClient);
            }, ServiceLifetime.Transient)
            .UseFunctionInvocation();

        return services;
    }

    public static IServiceCollection AddOpenAiChatClient(this IServiceCollection services)
    {
        services
            .AddKeyedChatClient(Provider.OpenAi, serviceProvider =>
            {
                var account = serviceProvider.GetRequiredService<Account>();
                account.RequireEntitlement(Entitlement.EnableOpenAiProvider);

                var openAiConfiguration = serviceProvider.GetRequiredService<IOptionsMonitor<OpenAiConfiguration>>();

                return new OpenAIClient(new ApiKeyCredential(openAiConfiguration.CurrentValue.ApiKey))
                    .GetChatClient(openAiConfiguration.CurrentValue.DefaultModel)
                    .AsIChatClient();
            }, ServiceLifetime.Transient)
            .UseFunctionInvocation();

        return services;
    }

    public static IServiceCollection AddOpenAiCompatibleChatClient(this IServiceCollection services)
    {
        services
            .AddKeyedChatClient(Provider.OpenAiCompatible, serviceProvider =>
            {
                var account = serviceProvider.GetRequiredService<Account>();
                account.RequireEntitlement(Entitlement.EnableOpenAiCompatibleProvider);

                var openAiCompatibleConfiguration = serviceProvider.GetRequiredService<IOptionsMonitor<OpenAiCompatibleConfiguration>>();
                var resilientHttpClient = serviceProvider.GetKeyedService<HttpClient>("ResilientHttpClient") ?? throw new NullReferenceException("ResilientHttpClient");

                var openAiClient = new OpenAIClient(
                    new ApiKeyCredential(openAiCompatibleConfiguration.CurrentValue.ApiKey),
                    new OpenAIClientOptions
                    {
                        Transport = new HttpClientPipelineTransport(resilientHttpClient),
                        Endpoint = openAiCompatibleConfiguration.CurrentValue.BaseAddress
                    });

                return openAiClient
                    .GetChatClient(openAiCompatibleConfiguration.CurrentValue.DefaultModel)
                    .AsIChatClient();
            }, ServiceLifetime.Transient)
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
