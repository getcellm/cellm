using Cellm.Exceptions;
using Cellm.ModelProviders;
using Cellm.Models;
using ExcelDna.Integration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cellm;

internal static class ServiceLocator
{
    private static readonly Lazy<IServiceProvider> _serviceProvider = new(() => Register());
    public static IServiceProvider ServiceProvider => _serviceProvider.Value;

    internal static IServiceProvider Register()
    {
        var basePath = ExcelDnaUtil.XllPathInfo?.Directory?.FullName ??
            throw new CellmException($"Unable to configure app, invalid value for ExcelDnaUtil.XllPathInfo='{ExcelDnaUtil.XllPathInfo}'");

        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json")
#if DEBUG
            .AddJsonFile("appsettings.Local.json", true)
#endif
            .Build();

        var services = new ServiceCollection();

        return services
            .AddSingleton(configuration)
            .AddHttpClient()
            .Configure<AnthropicConfiguration>(configuration.GetSection("Anthropic"))
            .AddTransient<AnthropicClient>()
            .AddSingleton<IClientFactory, ClientFactory>()
            .BuildServiceProvider();
    }
}
