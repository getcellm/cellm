using Cellm.Prompts;

namespace Cellm.Models.OpenAi;

internal record OpenAiRequest(Prompt Prompt, string? Provider, Uri? BaseAddress) : IModelRequest<OpenAiResponse>;
