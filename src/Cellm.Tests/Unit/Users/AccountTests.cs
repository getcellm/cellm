using System.Net.Http;
using Cellm.AddIn.Exceptions;
using Cellm.Users;
using Cellm.Users.Exceptions;
using Xunit;

namespace Cellm.Tests.Unit.Users;

/// <summary>
/// Tests for Account error handling scenarios.
/// Note: Full integration tests would require mocking HttpClient and HybridCache,
/// so these tests focus on the exception types and message formats.
/// </summary>
public class AccountTests
{
    [Fact]
    public void PermissionDeniedException_IsCellmException()
    {
        // Arrange & Act
        var ex = new PermissionDeniedException(Entitlement.EnableAnthropicProvider);

        // Assert
        Assert.IsAssignableFrom<CellmException>(ex);
    }

    [Fact]
    public void PermissionDeniedException_AnthropicProvider_HasHelpfulMessage()
    {
        // Arrange & Act
        var ex = new PermissionDeniedException(Entitlement.EnableAnthropicProvider);

        // Assert
        Assert.Contains("Anthropic", ex.Message);
        Assert.Contains("Sign in", ex.Message);
    }

    [Fact]
    public void PermissionDeniedException_OpenAiProvider_HasHelpfulMessage()
    {
        // Arrange & Act
        var ex = new PermissionDeniedException(Entitlement.EnableOpenAiProvider);

        // Assert
        Assert.Contains("OpenAi", ex.Message);
        Assert.Contains("Sign in", ex.Message);
    }

    [Fact]
    public void PermissionDeniedException_OllamaProvider_HasHelpfulMessage()
    {
        // Arrange & Act
        var ex = new PermissionDeniedException(Entitlement.EnableOllamaProvider);

        // Assert
        Assert.Contains("Ollama", ex.Message);
        Assert.Contains("Sign in", ex.Message);
    }

    [Fact]
    public void PermissionDeniedException_AwsProvider_HasHelpfulMessage()
    {
        var ex = new PermissionDeniedException(Entitlement.EnableAwsProvider);
        Assert.Contains("AWS", ex.Message);
        Assert.Contains("Sign in", ex.Message);
    }

    [Fact]
    public void PermissionDeniedException_GeminiProvider_HasHelpfulMessage()
    {
        var ex = new PermissionDeniedException(Entitlement.EnableGeminiProvider);
        Assert.Contains("Google", ex.Message);
        Assert.Contains("Sign in", ex.Message);
    }

    [Fact]
    public void PermissionDeniedException_WithInnerException_PreservesInner()
    {
        // Arrange
        var inner = new InvalidOperationException("Inner error");

        // Act
        var ex = new PermissionDeniedException(Entitlement.EnableAnthropicProvider, inner);

        // Assert
        Assert.Same(inner, ex.InnerException);
    }

    [Theory]
    [InlineData(Entitlement.EnableAnthropicProvider)]
    [InlineData(Entitlement.EnableAwsProvider)]
    [InlineData(Entitlement.EnableAzureProvider)]
    [InlineData(Entitlement.EnableCellmProvider)]
    [InlineData(Entitlement.EnableDeepSeekProvider)]
    [InlineData(Entitlement.EnableGeminiProvider)]
    [InlineData(Entitlement.EnableMistralProvider)]
    [InlineData(Entitlement.EnableOllamaProvider)]
    [InlineData(Entitlement.EnableOpenAiProvider)]
    [InlineData(Entitlement.EnableOpenAiCompatibleProvider)]
    public void PermissionDeniedException_AllProviders_HaveNonEmptyMessage(Entitlement entitlement)
    {
        // Arrange & Act
        var ex = new PermissionDeniedException(entitlement);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(ex.Message));
        Assert.DoesNotContain("#CELLM_ERROR?", ex.Message); // Should not use default
    }

    [Fact]
    public void CellmException_ForTokenError_CanWrapHttpException()
    {
        // Arrange - Simulating what happens when GetTokenAsync catches an exception
        var httpError = new HttpRequestException("Connection refused");

        // Act
        var ex = new CellmException("Could not get token", httpError);

        // Assert
        Assert.Equal("Could not get token", ex.Message);
        Assert.IsType<HttpRequestException>(ex.InnerException);
    }

    [Fact]
    public void CellmException_ForTokenError_CanWrapJsonException()
    {
        // Arrange - Simulating invalid JSON response
        var jsonError = new System.Text.Json.JsonException("Invalid JSON");

        // Act
        var ex = new CellmException("Could not get token", jsonError);

        // Assert
        Assert.Equal("Could not get token", ex.Message);
        Assert.IsType<System.Text.Json.JsonException>(ex.InnerException);
    }

    [Fact]
    public void Entitlement_HasExpectedValues()
    {
        // Assert - Verify key entitlements exist
        Assert.True(Enum.IsDefined(typeof(Entitlement), Entitlement.EnableAnthropicProvider));
        Assert.True(Enum.IsDefined(typeof(Entitlement), Entitlement.EnableAwsProvider));
        Assert.True(Enum.IsDefined(typeof(Entitlement), Entitlement.EnableOllamaProvider));
        Assert.True(Enum.IsDefined(typeof(Entitlement), Entitlement.EnableOpenAiProvider));
        Assert.True(Enum.IsDefined(typeof(Entitlement), Entitlement.EnableModelContextProtocol));
    }
}
