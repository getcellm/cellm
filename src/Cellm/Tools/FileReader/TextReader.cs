using System.Text;

internal class TextReader : IFileReader
{
    private readonly List<string> _extensions;

    public TextReader()
    {
        _extensions = new List<string> { ".c", ".cpp", ".cs", ".csv", ".cxx", ".h", ".hxx", ".html", ".java", ".json", ".jsonl", ".md", ".php", ".py", ".rb", ".txt", ".xml" };
    }

    public bool CanRead(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"File not found: {filePath}");
        }

        return _extensions.Contains(Path.GetExtension(filePath).ToLowerInvariant());
    }

    public async Task<string> ReadFile(string filePath, CancellationToken cancellationToken)
    {
        using (var stream = File.OpenRead(filePath))
        using (var reader = new StreamReader(stream, Encoding.UTF8, true))
        {
            return await reader.ReadToEndAsync(cancellationToken);
        }
    }
}