using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

namespace Cellm.Tools.FileSearch;

internal class IgnoreInaccessibleDirectoryInfoWrapper(DirectoryInfo directoryInfo) : DirectoryInfoBase
{
    public override string Name => directoryInfo.Name;

    public override string FullName => directoryInfo.FullName;

    public override DirectoryInfoBase? ParentDirectory =>
        directoryInfo.Parent is not null ? new IgnoreInaccessibleDirectoryInfoWrapper(directoryInfo.Parent) : null;

    public override IEnumerable<FileSystemInfoBase> EnumerateFileSystemInfos()
    {
        var options = new EnumerationOptions { IgnoreInaccessible = true };

        foreach (var info in directoryInfo.EnumerateFileSystemInfos("*", options))
        {
            if (info is DirectoryInfo dir)
                yield return new IgnoreInaccessibleDirectoryInfoWrapper(dir);
            else if (info is FileInfo file)
                yield return new FileInfoWrapper(file);
        }
    }

    public override DirectoryInfoBase GetDirectory(string path) =>
        new IgnoreInaccessibleDirectoryInfoWrapper(new DirectoryInfo(Path.Combine(directoryInfo.FullName, path)));

    public override FileInfoBase GetFile(string path) =>
        new FileInfoWrapper(new FileInfo(Path.Combine(directoryInfo.FullName, path)));
}
