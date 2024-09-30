using System.ComponentModel;
using Microsoft.Extensions.FileSystemGlobbing;

namespace Cellm.Tools;

internal record GlobRequest(
    [Description("The root directory to start the glob search from")] string Directory,
    [Description("List of patterns to include in the search")] List<string> IncludePatterns,
    [Description("Optional list of patterns to exclude from the search")] List<string>? ExcludesPatterns);

internal record GlobResponse(
    [Description("List of file paths matching the glob patterns")] List<string> FileNames);

internal class Glob : IFunction<GlobRequest, GlobResponse>
{
    [Description("Search in the specified directory based on include glob patterns and exclude glob patterns.")]
    public GlobResponse Handle(GlobRequest request)
    {
        var matcher = new Matcher();
        matcher.AddIncludePatterns(request.IncludePatterns);
        matcher.AddExcludePatterns(request.ExcludesPatterns ?? new List<string>());
        var fileNames = matcher.GetResultsInFullPath(request.Directory);
        return new GlobResponse(fileNames.ToList());
    }

    public string Serialize()
    {
        return OpenAiFunctionSerializer.Serialize<GlobRequest, GlobResponse>(nameof(Glob));
    }
}
