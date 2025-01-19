using Cellm.Models.Behaviors;
using Cellm.Models.Providers;
using Cellm.Models.Providers.Anthropic;
using Cellm.Models.Providers.Ollama;
using Cellm.Models.Providers.OpenAi;
using Cellm.Models.Providers.OpenAiCompatible;
using Cellm.Models.Resilience;
using Cellm.Models.Tools;
using MediatR;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenAI;

namespace Cellm.Models;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAnthropicChatClient(this IServiceCollection services, IConfiguration configuration)
    {
        var resiliencePipelineConfigurator = new ResiliencePipelineConfigurator(configuration);

        var anthropicConfiguration = configuration.GetRequiredSection(nameof(AnthropicConfiguration)).Get<AnthropicConfiguration>()
            ?? throw new NullReferenceException(nameof(AnthropicConfiguration));

        services
            .AddHttpClient<IRequestHandler<AnthropicRequest, AnthropicResponse>, AnthropicRequestHandler>(anthropicHttpClient =>
            {
                anthropicHttpClient.BaseAddress = anthropicConfiguration.BaseAddress;
                anthropicHttpClient.DefaultRequestHeaders.Add("x-api-key", anthropicConfiguration.ApiKey);
                anthropicHttpClient.DefaultRequestHeaders.Add("anthropic-version", anthropicConfiguration.Version);
                anthropicHttpClient.Timeout = TimeSpan.FromHours(1);
            })
            .AddResilienceHandler($"{nameof(AnthropicRequestHandler)}", resiliencePipelineConfigurator.ConfigureResiliencePipeline);

        // TODO: Add IChatClient-compatible Anthropic client

        return services;
    }

    public static IServiceCollection AddOpenOllamaChatClient(this IServiceCollection services, IConfiguration configuration)
    {
        var resiliencePipelineConfigurator = new ResiliencePipelineConfigurator(configuration);

        var ollamaConfiguration = configuration.GetRequiredSection(nameof(OllamaConfiguration)).Get<OllamaConfiguration>()
            ?? throw new NullReferenceException(nameof(OllamaConfiguration));

        services
            .AddKeyedChatClient(Provider.Ollama, serviceProvider => new OllamaChatClient(
                ollamaConfiguration.BaseAddress,
                ollamaConfiguration.DefaultModel))
            .UseFunctionInvocation();

        return services;
    }

    public static IServiceCollection AddOpenAiChatClient(this IServiceCollection services, IConfiguration configuration)
    {
        var openAiConfiguration = configuration.GetRequiredSection(nameof(OpenAiConfiguration)).Get<OpenAiConfiguration>()
            ?? throw new NullReferenceException(nameof(OpenAiConfiguration));

        services
            .AddKeyedChatClient(Provider.OpenAi, new OpenAIClient(openAiConfiguration.ApiKey).AsChatClient(openAiConfiguration.DefaultModel))
            .UseFunctionInvocation();

        return services;
    }

    public static IServiceCollection AddOpenAiCompatibleChatClient(this IServiceCollection services, IConfiguration configuration)
    {
        var resiliencePipelineConfigurator = new ResiliencePipelineConfigurator(configuration);

        services
            .AddSingleton<OpenAiCompatibleChatClientFactory>()
            .AddHttpClient<OpenAiCompatibleChatClientFactory>(openAiCompatibleHttpClient =>
            {
                openAiCompatibleHttpClient.Timeout = Timeout.InfiniteTimeSpan;
            })
            .AddResilienceHandler(nameof(OpenAiCompatibleChatClientFactory), resiliencePipelineConfigurator.ConfigureResiliencePipeline);


        return services;
    }

    public static IServiceCollection AddSentryBehavior(this IServiceCollection services)
    {
        services
            .AddSingleton(typeof(IPipelineBehavior<,>), typeof(SentryBehavior<,>));

        return services;
    }

    public static IServiceCollection AddCachingBehavior(this IServiceCollection services)
    {
#pragma warning disable EXTEXP0018 // Type is for evaluation purposes only and is subject to change or removal in future updates.
        services
            .AddHybridCache();
#pragma warning restore EXTEXP0018 // Type is for evaluation purposes only and is subject to change or removal in future updates.

        services
            .AddSingleton(typeof(IPipelineBehavior<,>), typeof(CacheBehavior<,>));

        return services;
    }

    public static IServiceCollection AddToolBehavior(this IServiceCollection services)
    {
        return services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(ToolBehavior<,>));
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
