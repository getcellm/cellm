namespace Cellm.ModelProviders;

internal class AnthropicConfiguration
{
    public Uri BaseAddress { get; init; }
    public string DefaultModel { get; init; }
    public Dictionary<string, string> Headers { get; init; }

    public AnthropicConfiguration()
    {
        BaseAddress = default!;
        DefaultModel = default!;
        Headers = new Dictionary<string, string>();
    }
}