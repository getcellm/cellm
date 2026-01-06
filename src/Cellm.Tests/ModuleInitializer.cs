using System.Runtime.CompilerServices;

namespace Cellm.Tests;

/// <summary>
/// Module initializer that runs before any test code.
/// Sets DOTNET_ENVIRONMENT to "Testing" so the add-in loads appsettings.Testing.json.
/// </summary>
internal static class ModuleInitializer
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        // Set before any test runs, before Excel add-in loads
        // This ensures appsettings.Testing.json is loaded with EnableCache=false
        Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", "Testing");
    }
}
