﻿using System.ComponentModel;
using MediatR;
using Microsoft.Extensions.FileSystemGlobbing;

namespace Cellm.Tools;

//internal record GlobRequest(
//    [Description("The root directory to start the glob search from")] string RootPath,
//    [Description("List of patterns to include in the search")] List<string> IncludePatterns,
//    [Description("Optional list of patterns to exclude from the search")] List<string>? ExcludePatterns) : IRequest<GlobResponse>;

internal record GlobRequest : IRequest<GlobResponse>
{
    public GlobRequest(string rootPath, List<string> includePatterns, List<string>? excludePatterns = null)
    {
        RootPath = rootPath;
        IncludePatterns = includePatterns;
        ExcludePatterns = excludePatterns;
    }

    [Description("The root directory to start the glob search from")]
    public string RootPath { get; set; }

    [Description("List of patterns to include in the search")]
    public List<string> IncludePatterns { get; set; }

    [Description("Optional list of patterns to exclude from the search")]
    public List<string>? ExcludePatterns { get; set; }
}

internal record GlobResponse(
    [Description("List of file paths matching the glob patterns")] List<string> FilePaths);

[Description("Search for files on the user's disk using glob patterns. Useful when user asks you to find files.")]
internal class Glob : IRequestHandler<GlobRequest, GlobResponse>
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

// https://medium.com/@kmorpex/quick-guide-mediatr-in-net-8-e3e2730bcc08
