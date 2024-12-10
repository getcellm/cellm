using System.Reflection;
using Cellm.Models;
using Cellm.Models.Providers.OpenAi;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI;

namespace Microsoft.Extensions.DependencyInjection;

internal static class OpenAiServiceCollectionExtensions
{
    public static IServiceCollection AddOpenAiChatClient(this IServiceCollection services, IConfiguration configuration)
    {
        var openAiConfiguration = configuration.GetRequiredSection(nameof(OpenAiConfiguration)).Get<OpenAiConfiguration>()
            ?? throw new NullReferenceException(nameof(OpenAiConfiguration));

        services
            .AddMediatR(mediatrConfiguration => mediatrConfiguration.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()))
            .AddKeyedChatClient(Provider.OpenAi, new OpenAIClient(openAiConfiguration.ApiKey).AsChatClient(openAiConfiguration.DefaultModel))
            .UseFunctionInvocation();

        return services;
    }
}
