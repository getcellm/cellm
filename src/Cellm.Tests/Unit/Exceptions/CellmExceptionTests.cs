using Cellm.AddIn.Exceptions;
using Xunit;

namespace Cellm.Tests.Unit.Exceptions;

public class CellmExceptionTests
{
    [Fact]
    public void DefaultMessage_ReturnsErrorMarker()
    {
        // Arrange & Act
        var ex = new CellmException();

        // Assert
        Assert.Equal("#CELLM_ERROR?", ex.Message);
    }

    [Fact]
    public void CustomMessage_IsPreserved()
    {
        // Arrange
        var customMessage = "Custom error message";

        // Act
        var ex = new CellmException(customMessage);

        // Assert
        Assert.Equal(customMessage, ex.Message);
    }

    [Fact]
    public void WithInnerException_PreservesInner()
    {
        // Arrange
        var innerMessage = "Inner exception message";
        var inner = new InvalidOperationException(innerMessage);

        // Act
        var ex = new CellmException("Outer message", inner);

        // Assert
        Assert.Same(inner, ex.InnerException);
        Assert.Equal(innerMessage, ex.InnerException?.Message);
    }

    [Fact]
    public void WithInnerException_PreservesOuterMessage()
    {
        // Arrange
        var outerMessage = "Outer message";
        var inner = new InvalidOperationException("Inner");

        // Act
        var ex = new CellmException(outerMessage, inner);

        // Assert
        Assert.Equal(outerMessage, ex.Message);
    }

    [Fact]
    public void IsExceptionType()
    {
        // Arrange & Act
        var ex = new CellmException();

        // Assert
        Assert.IsAssignableFrom<Exception>(ex);
    }
}
