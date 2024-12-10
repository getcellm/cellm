using System.Reflection;
using Cellm.Models.Resilience;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cellm.Models.Providers.OpenAiCompatible;

internal static class OpenAiCompatibleServiceCollectionExtensions
{
    public static IServiceCollection AddOpenAiCompatibleChatClient(this IServiceCollection services, IConfiguration configuration)
    {
        var resiliencePipelineConfigurator = new ResiliencePipelineConfigurator(configuration);

        services
            .AddMediatR(mediatrConfiguration => mediatrConfiguration.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()))
            .AddHttpClient<IRequestHandler<OpenAiCompatibleRequest, OpenAiCompatibleResponse>, OpenAiCompatibleRequestHandler>(openAiCompatibleHttpClient =>
            {
                openAiCompatibleHttpClient.Timeout = TimeSpan.FromHours(1);
            })
            .AddResilienceHandler(nameof(OpenAiCompatibleRequestHandler), resiliencePipelineConfigurator.ConfigureResiliencePipeline);

        return services;
    }
}
