using System.IO;
using System.Reflection;
using Cellm.AddIn;
using Cellm.Models;
using Path = System.IO.Path;
using Cellm.Models.Providers;
using Cellm.Models.Providers.Anthropic;
using Cellm.Models.Providers.DeepSeek;
using Cellm.Models.Providers.Google;
using Cellm.Models.Providers.Mistral;
using Cellm.Models.Providers.Ollama;
using Cellm.Models.Providers.OpenAi;
using Cellm.Models.Providers.OpenAiCompatible;
using Cellm.Models.Providers.OpenRouter;
using Cellm.AddIn.UserInterface.Ribbon;
using Cellm.Models.Behaviors;
using Cellm.Tools.ModelContextProtocol;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cellm.Tests.Integration.Helpers;

public class ProviderTestFixture : IDisposable
{
    public ServiceProvider ServiceProvider { get; }
    internal Client Client { get; }

    private static readonly string AppsettingsDir = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Cellm", "bin", "Debug", "net9.0-windows"));

    public ProviderTestFixture()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppsettingsDir)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Local.json", optional: true)
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["CellmAddInConfiguration:EnableCache"] = "false",
                ["CellmAddInConfiguration:EnableFileLogging"] = "false",
                ["CellmAddInConfiguration:EnableTools:FileSearchRequest"] = "true",
                ["CellmAddInConfiguration:EnableTools:FileReaderRequest"] = "true",
                ["CellmAddInConfiguration:EnableModelContextProtocolServers:Playwright"] = "true",
                ["AccountConfiguration:IsEnabled"] = "false",
                ["SentryConfiguration:IsEnabled"] = "false",
            })
            .Build();

        var services = CellmAddIn.ConfigureServices(new ServiceCollection(), configuration);

        // Replace McpConfigurationService with test version that doesn't depend on Excel/RibbonMain
        var mcpDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IMcpConfigurationService));
        if (mcpDescriptor != null) 
        { 
            services.Remove(mcpDescriptor); 
        }

        services.AddSingleton<IMcpConfigurationService, TestMcpConfigurationService>();

        // Remove UsageNotificationHandler which depends on RibbonMain (Excel UI)
        var usageHandler = services.FirstOrDefault(d =>
            d.ServiceType == typeof(INotificationHandler<UsageNotification>) &&
            d.ImplementationType == typeof(UsageNotificationHandler));
        if (usageHandler != null) services.Remove(usageHandler);

        ServiceProvider = services.BuildServiceProvider();
        Client = ServiceProvider.GetRequiredService<Client>();

        // Set the static CellmAddIn.Services to our test service provider so that
        // static references in behaviors (AdditionalPropertiesBehavior, ProviderRequestHandler)
        // use our test configuration instead of trying to initialize from Excel
        SetStaticServiceProvider(ServiceProvider);
    }

    public bool IsProviderAvailable(Provider provider)
    {
        return provider switch
        {
            Provider.Ollama => true,
            Provider.Anthropic => HasApiKey<AnthropicConfiguration>(),
            Provider.DeepSeek => HasApiKey<DeepSeekConfiguration>(),
            Provider.Gemini => HasApiKey<GeminiConfiguration>(),
            Provider.Mistral => HasApiKey<MistralConfiguration>(),
            Provider.OpenAi => HasApiKey<OpenAiConfiguration>(),
            Provider.OpenAiCompatible => HasApiKey<OpenAiCompatibleConfiguration>(),
            Provider.OpenRouter => HasApiKey<OpenRouterConfiguration>(),
            _ => false
        };
    }

    public string GetDefaultModel(Provider provider)
    {
        return provider switch
        {
            Provider.Anthropic => GetConfig<AnthropicConfiguration>().DefaultModel,
            Provider.DeepSeek => GetConfig<DeepSeekConfiguration>().DefaultModel,
            Provider.Gemini => GetConfig<GeminiConfiguration>().DefaultModel,
            Provider.Mistral => GetConfig<MistralConfiguration>().DefaultModel,
            Provider.Ollama => GetConfig<OllamaConfiguration>().DefaultModel,
            Provider.OpenAi => GetConfig<OpenAiConfiguration>().DefaultModel,
            Provider.OpenAiCompatible => GetConfig<OpenAiCompatibleConfiguration>().DefaultModel,
            Provider.OpenRouter => GetConfig<OpenRouterConfiguration>().DefaultModel,
            _ => throw new ArgumentException($"Unknown provider: {provider}")
        };
    }

    private bool HasApiKey<T>() where T : class
    {
        var config = ServiceProvider.GetRequiredService<IOptionsMonitor<T>>().CurrentValue;
        var apiKeyProp = typeof(T).GetProperty("ApiKey");
        var value = apiKeyProp?.GetValue(config) as string;
        return !string.IsNullOrWhiteSpace(value);
    }

    private T GetConfig<T>() where T : class =>
        ServiceProvider.GetRequiredService<IOptionsMonitor<T>>().CurrentValue;

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
}
