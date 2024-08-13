namespace Cellm;

public record Config
{
    public string ApiKey { get; init; } = "API_KEY";
    public string ApiUrl { get; init; } = "https://api.anthropic.com/v1/messages";
}
