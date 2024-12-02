using Cellm.Services.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cellm.Models.OpenAiCompatible;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOpenAiCompatibleChatClient(this IServiceCollection services, string provider, IConfiguration configuration)
    {
        var resiliencePipelineConfigurator = new ResiliencePipelineConfigurator(configuration);

        services
            .AddHttpClient(provider, openAiCompatibleHttpClient =>
            {
                openAiCompatibleHttpClient.Timeout = TimeSpan.FromHours(1);
            })
            .AddResilienceHandler($"{nameof(OpenAiCompatibleRequestHandler)}ResiliencePipeline", resiliencePipelineConfigurator.ConfigureResiliencePipeline);

        // This is probably not needed, because we would send a OpenAiCompatibleRequestHandler(Prompt prompt, Uri BaseAddress) and instantiate a client on each call
        //var openAiCompatibleConfiguration = configuration.GetRequiredSection($"{provider}Configuration").Get<OpenAiCompatibleConfiguration>()
        //    ?? throw new NullReferenceException(nameof(provider));

        //services
        //    .AddKeyedChatClient(Providers.OpenAiCompatible, serviceProvider =>
        //    {
        //        var openAiCompatibleHttpClient = serviceProvider
        //            .GetRequiredService<IHttpClientFactory>()
        //            .CreateClient(provider);

        //        var openAiClient = new OpenAIClient(
        //            new ApiKeyCredential(openAiCompatibleConfiguration.ApiKey),
        //            new OpenAIClientOptions
        //            {
        //                Transport = new HttpClientPipelineTransport(openAiCompatibleHttpClient),
        //            });

        //        return openAiClient.AsChatClient(openAiCompatibleConfiguration.DefaultModel);
        //    })
        //    .UseFunctionInvocation();

        return services;
    }
}
