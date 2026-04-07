using System.IO;
using System.Linq;
using Cellm.Tools.FileSearch;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using Xunit;

namespace Cellm.Tests.Unit.Tools;

public class IgnoreInaccessibleDirectoryInfoWrapperTests
{
    [Fact]
    public void EnumerateFileSystemInfos_ReturnsFilesInAccessibleDirectory()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            File.WriteAllText(Path.Combine(tempDir, "test.txt"), "hello");

            var wrapper = new IgnoreInaccessibleDirectoryInfoWrapper(new DirectoryInfo(tempDir));
            var entries = wrapper.EnumerateFileSystemInfos().ToList();

            Assert.Single(entries);
            Assert.Equal("test.txt", entries[0].Name);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void EnumerateFileSystemInfos_SkipsInaccessibleDirectories()
    {
        // C:\ProgramData contains junction points (Application Data, Desktop, etc.)
        // that throw when enumerated with the stock DirectoryInfoWrapper.
        var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

        // Stock wrapper throws on problematic junction points:
        // - IOException when recursive junctions cause path-too-long
        // - UnauthorizedAccessException when junctions deny access
        var stockWrapper = new DirectoryInfoWrapper(new DirectoryInfo(programData));
        var ex = Record.Exception(() =>
            new Matcher().AddInclude("**/*").Execute(stockWrapper).Files.ToList());
        Assert.True(ex is IOException or UnauthorizedAccessException,
            $"Expected IOException or UnauthorizedAccessException, got {ex?.GetType().Name}: {ex?.Message}");

        // Our wrapper skips them
        var safeWrapper = new IgnoreInaccessibleDirectoryInfoWrapper(new DirectoryInfo(programData));
        var result = new Matcher().AddInclude("**/*").Execute(safeWrapper);

        // ProgramData has files, so we should get matches without throwing
        Assert.True(result.HasMatches);
    }

    [Fact]
    public void Execute_WithMatcher_ReturnsMatchingFiles()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var subDir = Path.Combine(tempDir, "sub");
        Directory.CreateDirectory(subDir);

        try
        {
            File.WriteAllText(Path.Combine(subDir, "match.cs"), "code");
            File.WriteAllText(Path.Combine(subDir, "skip.txt"), "text");

            var matcher = new Matcher();
            matcher.AddInclude("**/*.cs");

            var result = matcher.Execute(new IgnoreInaccessibleDirectoryInfoWrapper(new DirectoryInfo(tempDir)));

            Assert.True(result.HasMatches);
            Assert.Single(result.Files);
            Assert.EndsWith("match.cs", result.Files.First().Path);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }
}
