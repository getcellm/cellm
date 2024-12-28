using System.IO.Compression;

namespace Cellm.Models.Local.Utilities;

internal class FileManager(HttpClient httpClient)
{
    public async Task<string> DownloadFileIfNotExists(Uri uri, string filePath)
    {
        if (File.Exists(filePath))
        {
            return filePath;
        }

        var filePathPart = $"{filePath}.part";

        if (File.Exists(filePathPart))
        {
            File.Delete(filePathPart);
        }

        var response = await httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        using (var fileStream = File.Create(filePathPart))
        using (var httpStream = await response.Content.ReadAsStreamAsync())
        {

            await httpStream.CopyToAsync(fileStream);
        }

        File.Move(filePathPart, filePath);

        return filePath;
    }

    public string CreateCellmDirectory(params string[] subFolders)
    {
        var folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), nameof(Cellm));

        if (subFolders.Length > 0)
        {
            folderPath = Path.Combine(subFolders.Prepend(folderPath).ToArray());
        }

        Directory.CreateDirectory(folderPath);
        return folderPath;
    }

    public string CreateCellmFilePath(string fileName, params string[] subFolders)
    {
        return Path.Combine(CreateCellmDirectory(subFolders), fileName);
    }

    public string ExtractZipFileIfNotExtracted(string zipFilePath, string targetDirectory)
    {
        if (Directory.Exists(targetDirectory))
        {
            return targetDirectory;
        }

        ZipFile.ExtractToDirectory(zipFilePath, targetDirectory, true);

        return targetDirectory;
    }
}
