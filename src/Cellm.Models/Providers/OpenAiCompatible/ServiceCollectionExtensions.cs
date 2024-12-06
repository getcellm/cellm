using Cellm.Services.Configuration;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cellm.Models.OpenAiCompatible;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOpenAiCompatibleChatClient(this IServiceCollection services, IConfiguration configuration)
    {
        var resiliencePipelineConfigurator = new ResiliencePipelineConfigurator(configuration);

        services
            .AddHttpClient<IRequestHandler<OpenAiCompatibleRequest, OpenAiCompatibleResponse>, OpenAiCompatibleRequestHandler>(openAiCompatibleHttpClient =>
            {
                openAiCompatibleHttpClient.Timeout = TimeSpan.FromHours(1);
            })
            .AddResilienceHandler(nameof(OpenAiCompatibleRequestHandler), resiliencePipelineConfigurator.ConfigureResiliencePipeline);

        return services;
    }
}
