using Cellm.Services.Configuration;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cellm.Models.Ollama;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOpenOllamaChatClient(this IServiceCollection services, IConfiguration configuration)
    {
        var resiliencePipelineConfigurator = new ResiliencePipelineConfigurator(configuration);

        var ollamaConfiguration = configuration.GetRequiredSection(nameof(OllamaConfiguration)).Get<OllamaConfiguration>()
            ?? throw new NullReferenceException(nameof(OllamaConfiguration));

        services
            .AddHttpClient(nameof(Providers.Ollama), ollamaHttpClient =>
            {
                ollamaHttpClient.BaseAddress = ollamaConfiguration.BaseAddress;
                ollamaHttpClient.Timeout = TimeSpan.FromHours(1);
            })
            .AddResilienceHandler(
                $"{nameof(OllamaRequestHandler)}ResiliencePipeline",
                resiliencePipelineConfigurator.ConfigureResiliencePipeline);

        services
            .AddKeyedChatClient(Providers.Ollama, serviceProvider => new OllamaChatClient(
                ollamaConfiguration.BaseAddress,
                ollamaConfiguration.DefaultModel,
                serviceProvider
                    .GetRequiredService<IHttpClientFactory>()
                    .CreateClient(nameof(Providers.Ollama))))
            .UseFunctionInvocation();

        return services;
    }
}
