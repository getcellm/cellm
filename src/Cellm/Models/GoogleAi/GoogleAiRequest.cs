using Cellm.Prompts;

namespace Cellm.Models.GoogleAi;

internal record GoogleAiRequest(Prompt Prompt, string? Provider, Uri? BaseAddress) : IModelRequest<GoogleAiResponse>;