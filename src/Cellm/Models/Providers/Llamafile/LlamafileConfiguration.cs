namespace Cellm.Models.Providers.Llamafile;

internal class LlamafileConfiguration : IProviderConfiguration
{
    public Uri BaseAddress { get; init; }

    public string DefaultModel { get; init; }

    public LlamafileConfiguration()
    {
        BaseAddress = default!;
        DefaultModel = default!;
    }
}
