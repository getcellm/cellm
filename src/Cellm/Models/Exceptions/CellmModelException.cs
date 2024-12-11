namespace Cellm.Models.Exceptions;

public class CellmModelException : Exception
{
    public CellmModelException(string message = "#CELLM_ERROR?")
        : base(message) { }

    public CellmModelException(string message, Exception inner)
        : base(message, inner) { }
}
