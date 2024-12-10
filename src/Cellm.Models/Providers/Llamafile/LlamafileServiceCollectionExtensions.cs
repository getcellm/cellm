using System.Reflection;
using Cellm.Models.Local.Utilities;
using Cellm.Models.Providers.Anthropic;
using Cellm.Models.Providers.Llamafile;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

internal static class LlamafileServiceCollectionExtensions
{
    public static IServiceCollection AddLlamafileChatClient(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMediatR(mediatrConfiguration => mediatrConfiguration.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        services.TryAddSingleton<FileManager>();
        services.TryAddSingleton<ProcessManager>();
        services.TryAddSingleton<ServerManager>();

        return services;
    }
}
