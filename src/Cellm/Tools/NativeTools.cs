﻿using System.ComponentModel;
using Cellm.Tools.FileReader;
using Cellm.Tools.FileSearch;
using MediatR;

namespace Cellm.Tools;

/// <summary>
/// Provides an adapter between MediatR and Microsoft.Extensions.AI by wrapping 
/// request handlers in function definitions suitable for the AIFunctionFactory.
/// </summary>
internal class NativeTools(ISender sender)
{
    [Description("Uses glob patterns to search for files on the user's disk and returns matching file paths.")]
    [return: Description($"The list of file paths that matches {nameof(includePatterns)} and do not match {nameof(excludePatterns)}")]
    public async Task<FileSearchResponse> FileSearchRequest(
    [Description("The root directory to start the glob search from")] string rootPath,
    [Description("The list of glob patterns whose matches will be included in the result")] List<string> includePatterns,
    [Description("An optional list of glob patterns whose matches will be excluded from the result")] List<string>? excludePatterns,
    CancellationToken cancellationToken)
    {
        return await sender.Send(new FileSearchRequest(rootPath, includePatterns, excludePatterns), cancellationToken);
    }

    [Description("Reads a file and returns its content as plain text.")]
    [return: Description("The content of the file as plain text")]
    public async Task<FileReaderResponse> FileReaderRequest(
        [Description("The absolute path to the file.")] string filePath,
        CancellationToken cancellationToken)
    {
        return await sender.Send(new FileReaderRequest(filePath), cancellationToken);
    }
}
