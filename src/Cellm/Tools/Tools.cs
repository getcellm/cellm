namespace Cellm.Tools;

internal class Tools
{
    private readonly Glob _glob;

    public Tools(Glob glob)
    {
        _glob = glob;
    }

    public List<Type> ToList()
    {
        return new List<Type> { typeof(GlobRequest) };
    }
}
