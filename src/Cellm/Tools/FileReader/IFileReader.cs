internal interface IFileReader
{
    public bool CanRead(string filePath);

    public Task<string> ReadContent(string filePath, CancellationToken cancellationToken);
}
