namespace Cellm.Tools;

internal class Tools
{
    private readonly Glob _glob;

    public Tools(Glob glob)
    {
        _glob = glob;
    }

    public List<string> Serialize()
    {
        return new List<string> { _glob.Serialize() };
    }
}
