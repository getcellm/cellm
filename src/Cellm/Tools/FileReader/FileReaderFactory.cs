namespace Cellm.Tools.FileReader;

internal class FileReaderFactory
{
    private readonly IEnumerable<IFileReader> _readers;

    public FileReaderFactory(IEnumerable<IFileReader> readers)
    {
        _readers = readers;
    }

    public IFileReader GetFileReader(string filePath)
    {
        return _readers.FirstOrDefault(r => r.CanRead(filePath))
            ?? throw new NotSupportedException($"No reader found for file: {filePath}");
    }
}