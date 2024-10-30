using System.ComponentModel;

namespace Cellm.Tools.Glob;

internal record GlobResponse(
    [Description("List of file paths matching the glob patterns")] List<string> FilePaths);
