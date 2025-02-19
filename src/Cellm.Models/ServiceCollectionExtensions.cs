using System.ClientModel.Primitives;
using System.ClientModel;
using Cellm.Models.Providers;
using Cellm.Models.Providers.Anthropic;
using Cellm.Models.Providers.Ollama;
using Cellm.Models.Providers.OpenAiCompatible;
using Cellm.Models.Resilience;
using MediatR;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using OpenAI;
using Microsoft.Extensions.DependencyInjection;
using Cellm.Models.Providers.OpenAi;
using Cellm.Models.Providers.Mistral;
using Cellm.Models.Providers.Llamafile;
using Cellm.Models.Providers.DeepSeek;

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

    public static IServiceCollection AddResilientHttpClient(this IServiceCollection services, IConfiguration configuration)
    {
        var resiliencePipelineConfigurator = new ResiliencePipelineConfigurator(configuration);

        services
            .AddHttpClient("ResilientHttpClient", resilientHttpClient =>
            {
                resilientHttpClient.Timeout = TimeSpan.FromSeconds(configuration
                    .GetSection(nameof(ProviderConfiguration))
                    .GetValue<int>(nameof(ProviderConfiguration.HttpTimeoutInSeconds)));
            })
            .AddAsKeyed()
            .AddResilienceHandler("ResilientHttpClientHandler", resiliencePipelineConfigurator.ConfigureResiliencePipeline);

        return services;
    }

    public static IServiceCollection AddAnthropicChatClient(this IServiceCollection services, IConfiguration configuration)
    {

        var resiliencePipelineConfigurator = new ResiliencePipelineConfigurator(configuration);

        var anthropicConfiguration = configuration.GetRequiredSection(nameof(AnthropicConfiguration)).Get<AnthropicConfiguration>()
            ?? throw new NullReferenceException(nameof(AnthropicConfiguration));

        services
            .AddHttpClient<IRequestHandler<AnthropicRequest, AnthropicResponse>, AnthropicRequestHandler>(anthropicHttpClient =>
            {
                anthropicHttpClient.BaseAddress = anthropicConfiguration.BaseAddress;
                anthropicHttpClient.DefaultRequestHeaders.Add("anthropic-version", anthropicConfiguration.Version);
                anthropicHttpClient.Timeout = TimeSpan.FromSeconds(configuration
                    .GetSection(nameof(ProviderConfiguration))
                    .GetValue<int>(nameof(ProviderConfiguration.HttpTimeoutInSeconds)));
            })
            .AddResilienceHandler($"{nameof(AnthropicRequestHandler)}{nameof(ResiliencePipelineConfigurator)}", resiliencePipelineConfigurator.ConfigureResiliencePipeline);

        // TODO: Add IChatClient-compatible Anthropic client

        return services;
    }

    public static IServiceCollection AddOllamaChatClient(this IServiceCollection services)
    {
        services
            .AddTransientKeyedChatClient(Provider.Ollama, serviceProvider => {
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
            .AddTransientKeyedChatClient(Provider.DeepSeek, serviceProvider => {
                var deepSeekConfiguration = serviceProvider.GetRequiredService<IOptionsMonitor<DeepSeekConfiguration>>();
                var resilientHttpClient = serviceProvider.GetKeyedService<HttpClient>("ResilientHttpClient") ?? throw new NullReferenceException("ResilientHttpClient");

                var openAiClient = new OpenAIClient(
                    new ApiKeyCredential(deepSeekConfiguration.CurrentValue.ApiKey),
                    new OpenAIClientOptions
                    {
                        Transport = new HttpClientPipelineTransport(resilientHttpClient),
                        Endpoint = deepSeekConfiguration.CurrentValue.BaseAddress
                    });

                return openAiClient.AsChatClient(deepSeekConfiguration.CurrentValue.DefaultModel);
            })
            .UseFunctionInvocation();

        return services;
    }

    public static IServiceCollection AddLlamafileChatClient(this IServiceCollection services)
    {
        services
            .AddTransientKeyedChatClient(Provider.Llamafile, serviceProvider => {
                var llamafileConfiguration = serviceProvider.GetRequiredService<IOptionsMonitor<LlamafileConfiguration>>();
                var resilientHttpClient = serviceProvider.GetKeyedService<HttpClient>("ResilientHttpClient") ?? throw new NullReferenceException("ResilientHttpClient");

                var openAiClient = new OpenAIClient(
                    new ApiKeyCredential(llamafileConfiguration.CurrentValue.ApiKey),
                    new OpenAIClientOptions
                    {
                        Transport = new HttpClientPipelineTransport(resilientHttpClient),
                        Endpoint = llamafileConfiguration.CurrentValue.BaseAddress
                    });

                return openAiClient.AsChatClient(llamafileConfiguration.CurrentValue.DefaultModel);
            })
            .UseFunctionInvocation();

        return services;
    }

    public static IServiceCollection AddMistralChatClient(this IServiceCollection services)
    {
        services
            .AddTransientKeyedChatClient(Provider.Mistral, serviceProvider => {
                var mistralConfiguration = serviceProvider.GetRequiredService<IOptionsMonitor<MistralConfiguration>>();
                var resilientHttpClient = serviceProvider.GetKeyedService<HttpClient>("ResilientHttpClient") ?? throw new NullReferenceException("ResilientHttpClient");

                var openAiClient = new OpenAIClient(
                    new ApiKeyCredential(mistralConfiguration.CurrentValue.ApiKey),
                    new OpenAIClientOptions
                    {
                        Transport = new HttpClientPipelineTransport(resilientHttpClient),
                        Endpoint = mistralConfiguration.CurrentValue.BaseAddress
                    });

                return openAiClient.AsChatClient(mistralConfiguration.CurrentValue.DefaultModel);
            })
            .UseFunctionInvocation();

        return services;
    }

    public static IServiceCollection AddOpenAiChatClient(this IServiceCollection services)
    {
        services
            .AddTransientKeyedChatClient(Provider.OpenAi, serviceProvider => {
                var openAiConfiguration = serviceProvider.GetRequiredService<IOptionsMonitor<OpenAiConfiguration>>();

                return new OpenAIClient(new ApiKeyCredential(openAiConfiguration.CurrentValue.ApiKey))
                    .AsChatClient(openAiConfiguration.CurrentValue.DefaultModel);
            })
            .UseFunctionInvocation();

        return services;
    }

    public static IServiceCollection AddOpenAiCompatibleChatClient(this IServiceCollection services)
    {
        services
            .AddTransientKeyedChatClient(Provider.OpenAiCompatible, serviceProvider => {
                var openAiCompatibleConfiguration = serviceProvider.GetRequiredService<IOptionsMonitor<OpenAiCompatibleConfiguration>>();
                var resilientHttpClient = serviceProvider.GetKeyedService<HttpClient>("ResilientHttpClient") ?? throw new NullReferenceException("ResilientHttpClient");

                var openAiClient = new OpenAIClient(
                    new ApiKeyCredential(openAiCompatibleConfiguration.CurrentValue.ApiKey),
                    new OpenAIClientOptions
                    {
                        Transport = new HttpClientPipelineTransport(resilientHttpClient),
                        Endpoint = openAiCompatibleConfiguration.CurrentValue.BaseAddress
                    });

                return openAiClient.AsChatClient(openAiCompatibleConfiguration.CurrentValue.DefaultModel);
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
