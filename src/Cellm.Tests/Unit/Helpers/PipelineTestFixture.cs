using System.IO;
using System.Reflection;
using Cellm.AddIn;
using Cellm.AddIn.UserInterface.Ribbon;
using Cellm.Models.Behaviors;
using Cellm.Models.Providers;
using Cellm.Tools.ModelContextProtocol;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cellm.Tests.Unit.Helpers;

public class PipelineTestFixture : IDisposable
{
    public ServiceProvider ServiceProvider { get; }
    public MockChatClient MockChatClient { get; } = new();

    private static readonly string AppsettingsDir = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Cellm", "bin", "Debug", "net9.0-windows"));

    public PipelineTestFixture()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppsettingsDir)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Local.json", optional: true)
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["CellmAddInConfiguration:EnableCache"] = "false",
                ["CellmAddInConfiguration:EnableFileLogging"] = "false",
                ["CellmAddInConfiguration:EnableTools:FileSearchRequest"] = "false",
                ["CellmAddInConfiguration:EnableTools:FileReaderRequest"] = "false",
                ["AccountConfiguration:IsEnabled"] = "false",
                ["SentryConfiguration:IsEnabled"] = "false",
            })
            .Build();

        var services = CellmAddIn.ConfigureServices(new ServiceCollection(), configuration);

        // Replace IChatClientFactory with one that returns our mock
        var factoryDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IChatClientFactory));
        if (factoryDescriptor != null)
        {
            services.Remove(factoryDescriptor);
        }

        services.AddSingleton<IChatClientFactory>(new MockChatClientFactory(MockChatClient));

        // Replace McpConfigurationService (depends on Excel/RibbonMain)
        var mcpDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IMcpConfigurationService));
        if (mcpDescriptor != null)
        {
            services.Remove(mcpDescriptor);
        }

        services.AddSingleton<IMcpConfigurationService, Integration.Helpers.TestMcpConfigurationService>();

        // Remove UsageNotificationHandler (depends on RibbonMain)
        var usageHandler = services.FirstOrDefault(d =>
            d.ServiceType == typeof(INotificationHandler<UsageNotification>) &&
            d.ImplementationType == typeof(UsageNotificationHandler));
        if (usageHandler != null) services.Remove(usageHandler);

        ServiceProvider = services.BuildServiceProvider();

        SetStaticServiceProvider(ServiceProvider);
    }

    private static void SetStaticServiceProvider(ServiceProvider serviceProvider)
    {
        var field = typeof(CellmAddIn).GetField("_serviceProvider", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Could not find CellmAddIn._serviceProvider field");
        field.SetValue(null, new Lazy<ServiceProvider>(() => serviceProvider));
    }

    public void Dispose()
    {
        ServiceProvider?.Dispose();
    }

    private class MockChatClientFactory(MockChatClient mockChatClient) : IChatClientFactory
    {
        public Microsoft.Extensions.AI.IChatClient GetClient(Provider provider) => mockChatClient;
    }
}
