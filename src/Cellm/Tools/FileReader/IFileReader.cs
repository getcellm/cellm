internal interface IFileReader
{
    public bool CanRead(string filePath);

    public Task<string> ReadFile(string filePath, CancellationToken cancellationToken);
}
