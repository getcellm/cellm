namespace Cellm.Models.Providers.Llamafile;

public class LlamafileConfiguration : IProviderConfiguration
{
    public Uri LlamafileUrl { get; init; }

    public Uri BaseAddress { get; init; }

    public Dictionary<string, Uri> Models { get; init; }

    public string DefaultModel { get; init; }

    public bool Gpu { get; init; }

    public int GpuLayers { get; init; }

    public LlamafileConfiguration()
    {
        LlamafileUrl = default!;
        BaseAddress = default!;
        Models = default!;
        DefaultModel = default!;
        Gpu = false;
        GpuLayers = 999;
    }
}
