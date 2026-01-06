using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cellm.Tests.Unit.Helpers;

/// <summary>
/// Minimal DI setup for unit tests that need basic services like logging and caching.
/// </summary>
public static class TestServices
{
    public static IServiceProvider Create()
    {
        var services = new ServiceCollection();

        services.AddLogging(builder => builder.AddDebug());

#pragma warning disable EXTEXP0018 // Type is for evaluation purposes only
        services.AddHybridCache();
#pragma warning restore EXTEXP0018

        return services.BuildServiceProvider();
    }
}
