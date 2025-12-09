using System.ClientModel;
using System.ClientModel.Primitives;
using System.Threading.RateLimiting;
using Amazon;
using Amazon.BedrockRuntime;
using Anthropic.SDK;
using Azure;
using Azure.AI.Inference;
using Cellm.AddIn;
using Cellm.AddIn.Exceptions;
using Cellm.AddIn.Logging;
using Cellm.Models.Prompts;
using Cellm.Models.Providers;
using Cellm.Models.Providers.Anthropic;
using Cellm.Models.Providers.Aws;
using Cellm.Models.Providers.Azure;
using Cellm.Models.Providers.Cellm;
using Cellm.Models.Providers.DeepSeek;
using Cellm.Models.Providers.Google;
using Cellm.Models.Providers.Mistral;
using Cellm.Models.Providers.Ollama;
using Cellm.Models.Providers.OpenAi;
using Cellm.Models.Providers.OpenAiCompatible;
using Cellm.Models.Providers.Vertex;
using Cellm.Models.Resilience;
using Cellm.Users;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mistral.SDK;
using OllamaSharp;
using OpenAI;
using Polly;
using Polly.Retry;
using Polly.Telemetry;
using Polly.Timeout;

namespace Cellm.Models;

internal static class ServiceCollectionExtensions
{
    internal static IServiceCollection AddRateLimiter(this IServiceCollection services, ResilienceConfiguration resilienceConfiguration)
    {
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
                    QueueLimit = resilienceConfiguration.RateLimiterConfiguration.RateLimiterQueueLimit,
                    TokenLimit = resilienceConfiguration.RateLimiterConfiguration.TokenLimit,
                    ReplenishmentPeriod = TimeSpan.FromSeconds(resilienceConfiguration.RateLimiterConfiguration.ReplenishmentPeriodInSeconds),
                    TokensPerPeriod = resilienceConfiguration.RateLimiterConfiguration.TokensPerPeriod,
                }))
                .AddConcurrencyLimiter(new ConcurrencyLimiterOptions
                {
                    QueueLimit = resilienceConfiguration.RateLimiterConfiguration.ConcurrencyLimiterQueueLimit,
                    PermitLimit = resilienceConfiguration.RateLimiterConfiguration.ConcurrencyLimit,

                })
                .AddRetry(new RetryStrategyOptions<Prompt>
                {
                    ShouldHandle = args => ValueTask.FromResult(RateLimiterHelpers.ShouldRetry(args.Outcome)),
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true,
                    MaxRetryAttempts = resilienceConfiguration.RetryConfiguration.MaxRetryAttempts,
                    Delay = TimeSpan.FromSeconds(resilienceConfiguration.RetryConfiguration.DelayInSeconds),
                })
                .ConfigureTelemetry(telemetryOptions)
                .Build();
        });
    }

    public static IServiceCollection AddResilientHttpClient(this IServiceCollection services, ResilienceConfiguration resilienceConfiguration, CellmAddInConfiguration cellmAddInConfiguration, Provider provider)
    {
        var httpClientBuilder = services
            .AddHttpClient(provider.ToString(), resilientHttpClient =>
            {
                // Delegate timeout to resilience pipeline
                resilientHttpClient.Timeout = Timeout.InfiniteTimeSpan;
            })
            .AddAsKeyed();

        // Only add the logging handler if body logging is enabled
        if (cellmAddInConfiguration.EnableHttpBodyLogging)
        {
            httpClientBuilder.AddHttpMessageHandler(serviceProvider =>
                new HttpBodyLoggingHandler(
                    serviceProvider.GetRequiredService<ILogger<HttpBodyLoggingHandler>>(),
                    cellmAddInConfiguration.HttpBodyLogMaxLengthBytes));
        }

        httpClientBuilder
            .AddResilienceHandler("ResilientHttpClientHandler", (builder, context) =>
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
                    .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
                    {
                        ShouldHandle = args => ValueTask.FromResult(RetryHttpClientHelpers.ShouldRetry(args.Outcome)),
                        BackoffType = DelayBackoffType.Exponential,
                        UseJitter = true,
                        MaxRetryAttempts = resilienceConfiguration.RetryConfiguration.MaxRetryAttempts,
                        Delay = TimeSpan.FromSeconds(resilienceConfiguration.RetryConfiguration.DelayInSeconds),
                    })
                    .AddTimeout(new TimeoutStrategyOptions
                    {
                        Timeout = TimeSpan.FromSeconds(resilienceConfiguration.RetryConfiguration.HttpTimeoutInSeconds),
                    })
                    .ConfigureTelemetry(telemetryOptions)
                    .Build();
            });

        return services;
    }

    public static HttpClient GetResilientHttpClient(this IServiceProvider serviceProvider, Provider provider)
    {
        return serviceProvider.GetKeyedService<HttpClient>(provider.ToString())
            ?? throw new InvalidOperationException($"No HttpClient registered for {provider}. Ensure AddRetryHttpClient was called for this provider.");
    }

    public static IServiceCollection AddAnthropicChatClient(this IServiceCollection services)
    {
        services
            .AddKeyedChatClient(Provider.Anthropic, serviceProvider =>
            {
                var account = serviceProvider.GetRequiredService<Account>();
                account.ThrowIfNotEntitled(Entitlement.EnableAzureProvider);

                var anthropicConfiguration = serviceProvider.GetRequiredService<IOptionsMonitor<AnthropicConfiguration>>();
                var resilientHttpClient = serviceProvider.GetResilientHttpClient(Provider.Anthropic);

                if (string.IsNullOrWhiteSpace(anthropicConfiguration.CurrentValue.ApiKey))
                {
                    throw new CellmException($"Empty {nameof(AnthropicConfiguration.ApiKey)} for {Provider.Anthropic}. Please set your API key.");
                }

                return new AnthropicClient(anthropicConfiguration.CurrentValue.ApiKey, resilientHttpClient)
                    .Messages
                    .AsBuilder()
                    .Build();
            }, ServiceLifetime.Transient)
            .UseFunctionInvocation();

        return services;
    }

    public static IServiceCollection AddAwsChatClient(this IServiceCollection services)
    {
        services
            .AddKeyedChatClient(Provider.Aws, serviceProvider =>
            {
                var account = serviceProvider.GetRequiredService<Account>();
                account.ThrowIfNotEntitled(Entitlement.EnableAwsProvider);

                var awsConfiguration = serviceProvider.GetRequiredService<IOptionsMonitor<AwsConfiguration>>();

                if (string.IsNullOrWhiteSpace(awsConfiguration.CurrentValue.ApiKey))
                {
                    throw new CellmException($"Empty {nameof(AwsConfiguration.ApiKey)} {Provider.Aws}. Please set your API key.");
                }

                var parts = awsConfiguration.CurrentValue.ApiKey.Split(':');

                if (parts.Length < 3)
                {
                    throw new CellmException("Invalid AWS API key or invalid format (must be \"Region:AccessKeyId:SecretAccessKey\", e.g. us-east-1:AKIAIOSFODNN7EXAMPLE:wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY)");
                }

                var region = parts[0];
                var accessKeyId = parts[1];
                var secretAccessKey = string.Join(':', parts[2..]);

                return new AmazonBedrockRuntimeClient(accessKeyId, secretAccessKey, RegionEndpoint.GetBySystemName(region))
                    .AsIChatClient(awsConfiguration.CurrentValue.DefaultModel);
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

                if (string.IsNullOrWhiteSpace(azureConfiguration.CurrentValue.ApiKey))
                {
                    throw new CellmException($"Empty {nameof(AzureConfiguration.ApiKey)} for {Provider.Azure}. Please set your API key.");
                }

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

                var accountConfiguration = serviceProvider.GetRequiredService<IOptionsMonitor<AccountConfiguration>>();

                if (string.IsNullOrWhiteSpace(accountConfiguration.CurrentValue.ApiKey))
                {
                    throw new CellmException($"Invalid {Provider.Cellm} credentials. Please login again.");
                }

                var cellmConfiguration = serviceProvider.GetRequiredService<IOptionsMonitor<CellmConfiguration>>();
                var resilientHttpClient = serviceProvider.GetResilientHttpClient(Provider.Cellm);

                var apiAuthentication = new Mistral.SDK.APIAuthentication(accountConfiguration.CurrentValue.ApiKey);
                var cellmClient = new MistralClient(apiAuthentication, resilientHttpClient)
                {
                    ApiUrlFormat = $"{cellmConfiguration.CurrentValue.BaseAddress.ToString().TrimEnd('/')}/{{1}}"
                };

                return cellmClient.Completions;
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
                var resilientHttpClient = serviceProvider.GetResilientHttpClient(Provider.DeepSeek);

                if (string.IsNullOrWhiteSpace(deepSeekConfiguration.CurrentValue.ApiKey))
                {
                    throw new CellmException($"Empty {nameof(DeepSeekConfiguration.ApiKey)} for {Provider.DeepSeek}. Please set your API key.");
                }

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
                var resilientHttpClient = serviceProvider.GetResilientHttpClient(Provider.Gemini);

                if (string.IsNullOrWhiteSpace(geminiConfiguration.CurrentValue.ApiKey))
                {
                    throw new CellmException($"Empty {nameof(GeminiConfiguration.ApiKey)} for {Provider.Gemini}. Please set your API key.");
                }

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
                var resilientHttpClient = serviceProvider.GetResilientHttpClient(Provider.Mistral);

                if (string.IsNullOrWhiteSpace(mistralConfiguration.CurrentValue.ApiKey))
                {
                    throw new CellmException($"Empty {nameof(MistralConfiguration.ApiKey)} for {Provider.Mistral}. Please set your API key.");
                }

                var apiAuthentication = new Mistral.SDK.APIAuthentication(mistralConfiguration.CurrentValue.ApiKey);
                var mistralClient = new MistralClient(apiAuthentication, resilientHttpClient);
                return mistralClient.Completions;
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

                if (string.IsNullOrWhiteSpace(openAiConfiguration.CurrentValue.ApiKey))
                {
                    throw new CellmException($"Empty {nameof(OpenAiConfiguration.ApiKey)} for {Provider.OpenAi}. Please set your API key.");
                }

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
                var resilientHttpClient = serviceProvider.GetResilientHttpClient(Provider.OpenAiCompatible);

                if (openAiCompatibleConfiguration.CurrentValue.BaseAddress.IsLoopback)
                {
                    account.ThrowIfNotEntitled(Entitlement.EnableOpenAiCompatibleProviderLocalModels);
                }
                else
                {
                    account.ThrowIfNotEntitled(Entitlement.EnableOpenAiCompatibleProviderHostedModels);
                }

                if (string.IsNullOrWhiteSpace(openAiCompatibleConfiguration.CurrentValue.ApiKey))
                {
                    throw new CellmException($"Empty {nameof(OpenAiCompatibleConfiguration.ApiKey)} for {Provider.OpenAiCompatible}. Please set your API key.");
                }

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

    public static IServiceCollection AddVertexChatClient(this IServiceCollection services)
    {
        services
            .AddKeyedChatClient(Provider.Vertex, serviceProvider =>
            {
                var account = serviceProvider.GetRequiredService<Account>();
                account.ThrowIfNotEntitled(Entitlement.EnableVertexProvider);

                var vertexConfiguration = serviceProvider.GetRequiredService<IOptionsMonitor<VertexConfiguration>>();
                var resilientHttpClient = serviceProvider.GetResilientHttpClient(Provider.Vertex);

                if (string.IsNullOrWhiteSpace(vertexConfiguration.CurrentValue.ApiKey))
                {
                    throw new CellmException($"Empty {nameof(VertexConfiguration.ApiKey)} for {Provider.Vertex}. Please set your API key.");
                }

                var openAiClient = new OpenAIClient(
                    new ApiKeyCredential(vertexConfiguration.CurrentValue.ApiKey),
                    new OpenAIClientOptions
                    {
                        Transport = new HttpClientPipelineTransport(resilientHttpClient),
                        Endpoint = vertexConfiguration.CurrentValue.BaseAddress
                    });

                return openAiClient.GetChatClient(vertexConfiguration.CurrentValue.DefaultModel).AsIChatClient();
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
