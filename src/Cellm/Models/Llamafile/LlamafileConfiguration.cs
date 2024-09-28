using Cellm.Services.Configuration;

namespace Cellm.Models.Llamafile;

internal class LlamafileConfiguration : IProviderConfiguration
{
    public Uri LlamafileUrl { get; init; }

    public Dictionary<string, Uri> Models { get; init; }

    public string DefaultModel { get; init; }

    public ushort Port { get; init; }

    public bool Gpu { get; init; }

    public int GpuLayers { get; init; }

    public LlamafileConfiguration()
    {
        LlamafileUrl = default!;
        Models = default!;
        DefaultModel = default!;
        Gpu = false;
        GpuLayers = 999;
    }
}
