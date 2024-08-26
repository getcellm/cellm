using System.Security.Cryptography;
using Cellm.AddIn;
using Cellm.Exceptions;
using Cellm.ModelProviders;
using Cellm.Models;
using ExcelDna.Integration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cellm;

internal static class ServiceLocator
{
    private static readonly Lazy<IServiceProvider> _serviceProvider = new(() => ConfigureServices(new ServiceCollection()).BuildServiceProvider());
    public static IServiceProvider ServiceProvider => _serviceProvider.Value;

    internal static IServiceCollection ConfigureServices(IServiceCollection services)
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

        var anthropicConfiguration = configuration.GetRequiredSection(nameof(AnthropicConfiguration)).Get<AnthropicConfiguration>() 
            ?? throw new CellmException($"Missing {nameof(AnthropicConfiguration)}");

        return services
            .Configure<CellmConfiguration>(configuration.GetSection(nameof(CellmConfiguration)))
            .Configure<AnthropicConfiguration>(configuration.GetSection(nameof(AnthropicConfiguration)))
            .AddHttpClient<AnthropicClient>(anthropicHttpClient =>
            {
                anthropicHttpClient.BaseAddress = anthropicConfiguration.BaseAddress;

                foreach (var header in anthropicConfiguration.Headers)
                {
                    anthropicHttpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
                }
            }).Services
            .AddSingleton<IClientFactory, ClientFactory>();
    }
}
