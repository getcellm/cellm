namespace Cellm.Models.Providers.Azure;

internal class AzureConfiguration
{
    public Uri BaseAddress { get; set; } = default!;

    public string DefaultModel { get; init; } = string.Empty;

    public string ApiKey { get; init; } = string.Empty;
}
