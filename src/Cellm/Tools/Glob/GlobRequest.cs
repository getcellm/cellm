using MediatR;

namespace Cellm.Tools.Glob;

internal record GlobRequest(string RootPath, List<string> IncludePatterns, List<string>? ExcludePatterns) : IRequest<GlobResponse>;
