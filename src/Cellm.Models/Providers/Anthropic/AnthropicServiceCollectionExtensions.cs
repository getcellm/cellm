using System.Reflection;
using Cellm.Models.Providers.Anthropic;
using Cellm.Models.Resilience;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

internal static class AnthropicServiceCollectionExtensions
{
    public static IServiceCollection AddAnthropicChatClient(this IServiceCollection services, IConfiguration configuration)
    {
        var resiliencePipelineConfigurator = new ResiliencePipelineConfigurator(configuration);

        var anthropicConfiguration = configuration.GetRequiredSection(nameof(AnthropicConfiguration)).Get<AnthropicConfiguration>()
            ?? throw new NullReferenceException(nameof(AnthropicConfiguration));

        services
            .AddMediatR(mediatrConfiguration => mediatrConfiguration.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()))
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
}
