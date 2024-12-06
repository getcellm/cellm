using Cellm.Models.Prompts;

namespace Cellm.Models.OpenAi;

internal record OpenAiRequest(Prompt Prompt) : IModelRequest<OpenAiResponse>;
