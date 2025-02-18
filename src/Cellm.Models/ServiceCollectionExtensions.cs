using Cellm.Models.Behaviors;
using Cellm.Models.Providers;
using Cellm.Models.Providers.Anthropic;
using Cellm.Models.Providers.Ollama;
using Cellm.Models.Resilience;
using Cellm.Models.Tools;
using MediatR;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
                anthropicHttpClient.DefaultRequestHeaders.Add("anthropic-version", anthropicConfiguration.Version);
                anthropicHttpClient.Timeout = TimeSpan.FromSeconds(configuration
                    .GetSection(nameof(ProviderConfiguration))
                    .GetValue<int>(nameof(ProviderConfiguration.HttpTimeoutInSeconds)));
            })
            .AddResilienceHandler($"{nameof(AnthropicRequestHandler)}{nameof(ResiliencePipelineConfigurator)}", resiliencePipelineConfigurator.ConfigureResiliencePipeline);

        // TODO: Add IChatClient-compatible Anthropic client

        return services;
    }

    public static IServiceCollection AddOllamaChatClient(this IServiceCollection services, IConfiguration configuration)
    {
        var ollamaConfiguration = configuration.GetRequiredSection(nameof(OllamaConfiguration)).Get<OllamaConfiguration>()
            ?? throw new NullReferenceException(nameof(OllamaConfiguration));

        services
            .AddKeyedChatClient(Provider.Ollama, serviceProvider => new OllamaChatClient(
                ollamaConfiguration.BaseAddress,
                ollamaConfiguration.DefaultModel,
                serviceProvider.GetKeyedService<HttpClient>("ResilientHttpClient")))
            .UseFunctionInvocation();

        return services;
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

    public static IServiceCollection AddTools(this IServiceCollection services, params Delegate[] tools)
    {
        services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(ToolBehavior<,>));

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
