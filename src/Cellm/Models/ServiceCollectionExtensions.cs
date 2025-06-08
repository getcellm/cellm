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
using System.Net.Http.Headers;
using System.Threading.RateLimiting;
using Anthropic.SDK;
using Azure;
using Azure.AI.Inference;
using Cellm.Models.Prompts;
using Cellm.Models.Providers;
using Cellm.Models.Providers.Anthropic;
using Cellm.Models.Providers.Azure;
using Cellm.Models.Providers.Cellm;
using Cellm.Models.Providers.DeepSeek;
using Cellm.Models.Providers.Google;
using Cellm.Models.Providers.Mistral;
using Cellm.Models.Providers.Ollama;
using Cellm.Models.Providers.OpenAi;
using Cellm.Models.Providers.OpenAiCompatible;
using Cellm.Models.Resilience;
using Cellm.Users;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Mistral.SDK;
using OllamaSharp;
using OpenAI;
using Polly;
using Polly.Retry;
using Polly.Telemetry;

namespace Cellm.Models;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRateLimiter(this IServiceCollection services, IServiceProvider configurationProvider)
    {
        var resilienceConfiguration = configurationProvider.GetRequiredService<IOptions<ResilienceConfiguration>>();

        return services.AddResiliencePipeline<string, Prompt>("RateLimiter", (builder, context) =>
        {
            // Decrease severity of most Polly events
            var telemetryOptions = new TelemetryOptions(context.GetOptions<TelemetryOptions>())
            {
                SeverityProvider = args => args.Event.EventName switch
                {
                    "OnRetry" => ResilienceEventSeverity.Information,
                    _ => ResilienceEventSeverity.Debug
                }
            };

            builder
                .AddRateLimiter(new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
                {
                    QueueLimit = resilienceConfiguration.Value.RateLimiterConfiguration.RateLimiterQueueLimit,
                    TokenLimit = resilienceConfiguration.Value.RateLimiterConfiguration.TokenLimit,
                    ReplenishmentPeriod = TimeSpan.FromSeconds(resilienceConfiguration.Value.RateLimiterConfiguration.ReplenishmentPeriodInSeconds),
                    TokensPerPeriod = resilienceConfiguration.Value.RateLimiterConfiguration.TokensPerPeriod,
                }))
                .AddConcurrencyLimiter(new ConcurrencyLimiterOptions
                {
                    QueueLimit = resilienceConfiguration.Value.RateLimiterConfiguration.ConcurrencyLimiterQueueLimit,
                    PermitLimit = resilienceConfiguration.Value.RateLimiterConfiguration.ConcurrencyLimit,

                })
                .AddRetry(new RetryStrategyOptions<Prompt>
                {
                    ShouldHandle = args => ValueTask.FromResult(RateLimiterHelpers.ShouldRetry(args.Outcome)),
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true,
                    MaxRetryAttempts = resilienceConfiguration.Value.RetryConfiguration.MaxRetryAttempts,
                    Delay = TimeSpan.FromSeconds(resilienceConfiguration.Value.RetryConfiguration.DelayInSeconds),
                })
                .ConfigureTelemetry(telemetryOptions)
                .Build();
        });
    }

    public static IServiceCollection AddRetryHttpClient(this IServiceCollection services, IServiceProvider configurationProvider)
    {
        var resilienceConfiguration = configurationProvider.GetRequiredService<IOptions<ResilienceConfiguration>>();

        services
            .AddRedaction()
            .AddExtendedHttpClientLogging()
            .AddHttpClient("ResilientHttpClient", resilientHttpClient =>
            {
                // Delegate timeout to resilience pipeline
                resilientHttpClient.Timeout = Timeout.InfiniteTimeSpan;
            })
            .AddAsKeyed()
            .AddResilienceHandler("RetryHttpClientHandler", (builder, context) =>
            {
                // Decrease severity of most Polly events
                var telemetryOptions = new TelemetryOptions(context.GetOptions<TelemetryOptions>())
                {
                    SeverityProvider = args => args.Event.EventName switch
                    {
                        // Decrease severity of specific events
                        "OnRetry" => ResilienceEventSeverity.Information,
                        _ => ResilienceEventSeverity.Debug
                    }
                };

                builder
                    .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
                    {
                        ShouldHandle = args => ValueTask.FromResult(RetryHttpClientHelpers.ShouldRetry(args.Outcome)),
                        BackoffType = DelayBackoffType.Exponential,
                        UseJitter = true,
                        MaxRetryAttempts = resilienceConfiguration.Value.RetryConfiguration.MaxRetryAttempts,
                        Delay = TimeSpan.FromSeconds(resilienceConfiguration.Value.RetryConfiguration.DelayInSeconds),
                    })
                    .AddTimeout(TimeSpan.FromSeconds(resilienceConfiguration.Value.RetryConfiguration.HttpTimeoutInSeconds))
                    .ConfigureTelemetry(telemetryOptions)
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
                account.ThrowIfNotEntitled(Entitlement.EnableAzureProvider);

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

    public static IServiceCollection AddAzureChatClient(this IServiceCollection services)
    {
        services
            .AddKeyedChatClient(Provider.Azure, serviceProvider =>
            {
                var account = serviceProvider.GetRequiredService<Account>();
                account.ThrowIfNotEntitled(Entitlement.EnableAzureProvider);

                var azureConfiguration = serviceProvider.GetRequiredService<IOptionsMonitor<AzureConfiguration>>();
                var resilientHttpClient = serviceProvider.GetKeyedService<HttpClient>("ResilientHttpClient") ?? throw new NullReferenceException("ResilientHttpClient");

                return new ChatCompletionsClient(
                    azureConfiguration.CurrentValue.BaseAddress,
                    new AzureKeyCredential(azureConfiguration.CurrentValue.ApiKey))
                    .AsIChatClient(azureConfiguration.CurrentValue.DefaultModel);
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
                account.ThrowIfNotEntitled(Entitlement.EnableCellmProvider);

                var cellmConfiguration = serviceProvider.GetRequiredService<IOptionsMonitor<CellmConfiguration>>();
                var resilientHttpClient = serviceProvider.GetKeyedService<HttpClient>("ResilientHttpClient") ?? throw new NullReferenceException("ResilientHttpClient");
                resilientHttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", account.GetBasicAuthCredentials());

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
                account.ThrowIfNotEntitled(Entitlement.EnableDeepSeekProvider);

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

    public static IServiceCollection AddGeminiChatClient(this IServiceCollection services)
    {
        services
            .AddKeyedChatClient(Provider.Gemini, serviceProvider =>
            {
                var account = serviceProvider.GetRequiredService<Account>();
                account.ThrowIfNotEntitled(Entitlement.EnableGeminiProvider);

                var geminiConfiguration = serviceProvider.GetRequiredService<IOptionsMonitor<GeminiConfiguration>>();
                var resilientHttpClient = serviceProvider.GetKeyedService<HttpClient>("ResilientHttpClient") ?? throw new NullReferenceException("ResilientHttpClient");

                var openAiClient = new OpenAIClient(
                    new ApiKeyCredential(geminiConfiguration.CurrentValue.ApiKey),
                    new OpenAIClientOptions
                    {
                        Transport = new HttpClientPipelineTransport(resilientHttpClient),
                        Endpoint = geminiConfiguration.CurrentValue.BaseAddress
                    });

                return openAiClient.GetChatClient(geminiConfiguration.CurrentValue.DefaultModel).AsIChatClient();
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
                account.ThrowIfNotEntitled(Entitlement.EnableMistralProvider);

                var mistralConfiguration = serviceProvider.GetRequiredService<IOptionsMonitor<MistralConfiguration>>();
                var resilientHttpClient = serviceProvider.GetKeyedService<HttpClient>("ResilientHttpClient") ?? throw new NullReferenceException("ResilientHttpClient");

                return new MistralClient(mistralConfiguration.CurrentValue.ApiKey, resilientHttpClient).Completions;
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
                account.ThrowIfNotEntitled(Entitlement.EnableOllamaProvider);

                var ollamaConfiguration = serviceProvider.GetRequiredService<IOptionsMonitor<OllamaConfiguration>>();

                return new OllamaApiClient(
                    ollamaConfiguration.CurrentValue.BaseAddress,
                    ollamaConfiguration.CurrentValue.DefaultModel);
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
                account.ThrowIfNotEntitled(Entitlement.EnableOpenAiProvider);

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
                account.ThrowIfNotEntitled(Entitlement.EnableOpenAiCompatibleProvider);

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
