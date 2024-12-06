using Cellm.Models.Providers;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenAI;

namespace Cellm.Models.OpenAi;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOpenAiChatClient(this IServiceCollection services, IConfiguration configuration)
    {
        var openAiConfiguration = configuration.GetRequiredSection(nameof(OpenAiConfiguration)).Get<OpenAiConfiguration>()
            ?? throw new NullReferenceException(nameof(OpenAiConfiguration));

        services
            .AddKeyedChatClient(Provider.OpenAi, new OpenAIClient(openAiConfiguration.ApiKey).AsChatClient(openAiConfiguration.DefaultModel))
            .UseFunctionInvocation();

        return services;
    }
}
