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
        var result = matcher.Execute(new IgnoreInaccessibleDirectoryInfoWrapper(new DirectoryInfo(request.RootPath)));
        var fileNames = result.Files.Select(x => Path.GetFullPath(Path.Combine(request.RootPath, x.Path)));

        return Task.FromResult(new FileSearchResponse(fileNames.ToList()));
    }
}
