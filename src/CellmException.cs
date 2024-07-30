namespace Cellm.Exceptions;

public class CellmException : Exception
{
    public CellmException() { }

    public CellmException(string message)
        : base(message) { }

    public CellmException(string message, Exception inner)
        : base(message, inner) { }
}
