namespace Cellm.AddIn;

public record AddIn
{
    public string ApiKey { get; init; } = "API_KEY";
    public string ApiUrl { get; init; } = "https://api.anthropic.com/v1/messages";
}
