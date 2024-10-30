using MediatR;
using Microsoft.Extensions.FileSystemGlobbing;

namespace Cellm.Tools.Glob;

internal class GlobRequestHandler : IRequestHandler<GlobRequest, GlobResponse>
{
    public Task<GlobResponse> Handle(GlobRequest request, CancellationToken cancellationToken)
    {
        var matcher = new Matcher();
        matcher.AddIncludePatterns(request.IncludePatterns);
        matcher.AddExcludePatterns(request.ExcludePatterns ?? new List<string>());
        var fileNames = matcher.GetResultsInFullPath(request.RootPath);

        return Task.FromResult(new GlobResponse(fileNames.ToList()));
    }
}
