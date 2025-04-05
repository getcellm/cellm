using MediatR;
using Microsoft.Extensions.FileSystemGlobbing;

namespace Cellm.Tools.FileSearch;

internal class FileSearchRequestHandler : IRequestHandler<FileSearchRequest, FileSearchResponse>
{
    public Task<FileSearchResponse> Handle(FileSearchRequest request, CancellationToken cancellationToken)
    {
        var matcher = new Matcher();
        matcher.AddIncludePatterns(request.IncludePatterns);
        matcher.AddExcludePatterns(request.ExcludePatterns ?? []);
        var fileNames = matcher.GetResultsInFullPath(request.RootPath);

        return Task.FromResult(new FileSearchResponse(fileNames.ToList()));
    }
}
