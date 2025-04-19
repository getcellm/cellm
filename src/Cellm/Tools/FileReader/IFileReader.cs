internal interface IFileReader
{
    public bool CanRead(string filePath);

    public Task<string> ReadFileAsync(string filePath, CancellationToken cancellationToken);
}
