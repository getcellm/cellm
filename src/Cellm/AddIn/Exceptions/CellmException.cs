namespace Cellm.AddIn.Exceptions;

public class CellmException : Exception
{
    public CellmException(string message = "#CELLM_ERROR?")
        : base(message) { }

    public CellmException(string message, Exception inner)
        : base(message, inner) { }
}
