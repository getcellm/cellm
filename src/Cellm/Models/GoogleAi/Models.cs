using System.Text.Json.Serialization;

namespace Cellm.Models.GoogleAi;

public class GoogleAiRequestBody
{
    [JsonPropertyName("system_instruction")]
    public GoogleAiContent? SystemInstruction { get; set; }

    public List<GoogleAiContent>? Contents { get; set; }
}

public class GoogleAiResponseBody
{
    public List<GoogleAiCandidate>? Candidates { get; set; }

    public GoogleAiUsageMetadata? UsageMetadata { get; set; }
}

public class GoogleAiCandidate
{
    public GoogleAiContent? Content { get; set; }

    public string? FinishReason { get; set; }

    public int Index { get; set; }

    public List<GoogleAiSafetyRating>? SafetyRatings { get; set; }
}

public class GoogleAiContent
{
    public List<GoogleAiPart>? Parts { get; set; }

    public string? Role { get; set; }
}

public class GoogleAiPart
{
    public string? Text { get; set; }
}

public class GoogleAiSafetyRating
{
    public string? Category { get; set; }

    public string? Probability { get; set; }
}

public class GoogleAiUsageMetadata
{
    public int PromptTokenCount { get; set; }

    public int CandidatesTokenCount { get; set; }

    public int TotalTokenCount { get; set; }
}