using Xunit;

namespace Cellm.Tests;

public class UnitTests
{
    [Fact]
    public void TestSanity()
    {
        int a = 3;
        int b = 5;

        int result = a + b;

        Assert.Equal(8, result);
    }
}
