namespace Cellm.Models.Providers.Aws;

internal class AwsConfiguration : IProviderConfiguration
{
    public Provider Id { get => Provider.Aws; }

    public string Name { get => "AWS Bedrock"; }

    public string Icon { get => $"AddIn/UserInterface/Resources/{nameof(Provider.Aws)}"; }

    public Uri BaseAddress { get; set; } = default!;

    public string DefaultModel { get; init; } = string.Empty;

    public string SmallModel { get; init; } = string.Empty;

    public string MediumModel { get; init; } = string.Empty;

    public string LargeModel { get; init; } = string.Empty;

    public string ApiKey { get; init; } = string.Empty;

    public bool IsEnabled { get; init; } = false;
}
