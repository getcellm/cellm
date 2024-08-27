using Cellm.AddIn;
using Cellm.Exceptions;
using Cellm.ModelProviders;
using Cellm.Models;
using Cellm.Prompts;
using ExcelDna.Integration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cellm;

internal static class ServiceLocator
{
    private static readonly Lazy<IServiceProvider> _serviceProvider = new(() => ConfigureServices(new ServiceCollection()).BuildServiceProvider());
    public static IServiceProvider ServiceProvider => _serviceProvider.Value;

    private static IServiceCollection ConfigureServices(IServiceCollection services)
    {
        // Configurations
        var basePath = ExcelDnaUtil.XllPathInfo?.Directory?.FullName ??
            throw new CellmException($"Unable to configure app, invalid value for ExcelDnaUtil.XllPathInfo='{ExcelDnaUtil.XllPathInfo}'");

        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json")
#if DEBUG
            .AddJsonFile("appsettings.Local.json", true)
#endif
            .Build();

        services
            .Configure<CellmConfiguration>(configuration.GetRequiredSection(nameof(CellmConfiguration)))
            .Configure<AnthropicConfiguration>(configuration.GetRequiredSection(nameof(AnthropicConfiguration)));

        // Internals
        services
            .AddTransient<ArgumentParser>()
            .AddSingleton<IClientFactory, ClientFactory>();

        // Model Providers
        var anthropicConfiguration = configuration.GetRequiredSection(nameof(AnthropicConfiguration)).Get<AnthropicConfiguration>()
            ?? throw new NullReferenceException(nameof(AnthropicConfiguration));

        services.AddHttpClient<AnthropicClient>(anthropicHttpClient =>
        {
            anthropicHttpClient.BaseAddress = anthropicConfiguration.BaseAddress;

            foreach (var header in anthropicConfiguration.Headers)
            {
                anthropicHttpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
            }
        });

        return services;
    }

    public static T Get<T>() where T : notnull
    {
        return ServiceProvider.GetRequiredService<T>();
    }
}
