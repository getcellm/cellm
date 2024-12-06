using Cellm.Models.Anthropic;
using Cellm.Models.Local.Utilities;
using Cellm.Models.Providers.Anthropic;
using Cellm.Services.Configuration;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cellm.Models.Providers.Llamafile;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLlamafileChatClient(this IServiceCollection services, IConfiguration configuration)
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

        services.TryAddSingleton<FileManager>();
        services.TryAddSingleton<ProcessManager>();
        services.TryAddSingleton<ServerManager>();

        return services;
    }
}
