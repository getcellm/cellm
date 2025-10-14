using Cellm.AddIn.Exceptions;

namespace Cellm.Tools.ModelContextProtocol.Exceptions;

public class McpServerException : CellmException
{
    public McpServerException(string message) : base(message)
    {
    }

    public McpServerException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
