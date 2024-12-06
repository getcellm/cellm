using Cellm.Models.Local.Utilities;
using Cellm.Models.Providers;
using Cellm.Services.Configuration;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cellm.Models.Ollama;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOpenOllamaChatClient(this IServiceCollection services, IConfiguration configuration)
    {
        var resiliencePipelineConfigurator = new ResiliencePipelineConfigurator(configuration);

        var ollamaConfiguration = configuration.GetRequiredSection(nameof(OllamaConfiguration)).Get<OllamaConfiguration>()
            ?? throw new NullReferenceException(nameof(OllamaConfiguration));

        services
            .AddHttpClient(nameof(Provider.Ollama), ollamaHttpClient =>
            {
                ollamaHttpClient.BaseAddress = ollamaConfiguration.BaseAddress;
                ollamaHttpClient.Timeout = TimeSpan.FromHours(1);
            })
            .AddResilienceHandler(
                $"{nameof(OllamaRequestHandler)}",
                resiliencePipelineConfigurator.ConfigureResiliencePipeline);

        services
            .AddKeyedChatClient(Provider.Ollama, serviceProvider => new OllamaChatClient(
                ollamaConfiguration.BaseAddress,
                ollamaConfiguration.DefaultModel,
                serviceProvider
                    .GetRequiredService<IHttpClientFactory>()
                    .CreateClient(nameof(Provider.Ollama))))
            .UseFunctionInvocation();

        services.TryAddSingleton<FileManager>();
        services.TryAddSingleton<ProcessManager>();
        services.TryAddSingleton<ServerManager>();

        return services;
    }
}
