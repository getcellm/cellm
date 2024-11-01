public class FileReaderException : Exception
{
    public FileReaderException(string message) : base(message)
    {
    }

    public FileReaderException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}