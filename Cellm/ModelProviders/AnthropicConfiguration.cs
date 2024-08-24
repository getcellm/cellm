namespace Cellm.ModelProviders;

internal class AnthropicConfiguration
{
    public Uri BaseAddress { get; set; }
    public string Model { get; set; }
    public Dictionary<string, string> Headers { get; set; }

    public AnthropicConfiguration()
    {
    }

    public AnthropicConfiguration(Uri baseAddress, string model, Dictionary<string, string> headers)
    {
        BaseAddress = baseAddress;
        Model = model;
        Headers = headers ?? new Dictionary<string, string>();
    }
}