using System.ComponentModel;
using MediatR;
using Microsoft.Extensions.FileSystemGlobbing;

namespace Cellm.Tools;

internal record GlobRequest(
    [Description("The root directory to start the glob search from")] string Path,
    [Description("List of patterns to include in the search")] List<string> IncludePatterns,
    [Description("Optional list of patterns to exclude from the search")] List<string>? ExcludePatterns) : IRequest<GlobResponse>;

internal record GlobResponse(
    [Description("List of file paths matching the glob patterns")] List<string> FileNames);

internal class Glob : IRequestHandler<GlobRequest, GlobResponse>
{
    [Description("Search in the specified directory based on include exclude glob patterns.")]
    public Task<GlobResponse> Handle(GlobRequest request, CancellationToken cancellationToken)
    {
        var matcher = new Matcher();
        matcher.AddIncludePatterns(request.IncludePatterns);
        matcher.AddExcludePatterns(request.ExcludePatterns ?? new List<string>());
        var fileNames = matcher.GetResultsInFullPath(request.Path);

        return Task.FromResult(new GlobResponse(fileNames.ToList()));
    }
}

// https://medium.com/@kmorpex/quick-guide-mediatr-in-net-8-e3e2730bcc08
