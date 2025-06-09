namespace Cellm.Models.Providers.Aws;

internal class AwsConfiguration
{
    public Uri BaseAddress { get; set; } = default!;

    public string DefaultModel { get; init; } = string.Empty;

    public string ApiKey { get; init; } = string.Empty;
}
