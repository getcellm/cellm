using MediatR;

namespace Cellm.Tools.FileSearch;

internal record FileSearchRequest(string RootPath, List<string> IncludePatterns, List<string>? ExcludePatterns) : IRequest<FileSearchResponse>;
