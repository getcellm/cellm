using Cellm.Models.Tools;
using MediatR;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace Cellm.Models.Behaviors;

internal static class ServiceCollectionExtensions
{
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

    public static IServiceCollection AddToolBehavior(this IServiceCollection services)
    {
        return services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(ToolBehavior<,>));
    }

    public static IServiceCollection AddTools(this IServiceCollection services, params Delegate[] tools)
    {
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
