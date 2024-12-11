using Cellm.Models.Prompts;

namespace Cellm.Models.Providers.OpenAi;

internal record OpenAiRequest(Prompt Prompt) : IModelRequest<OpenAiResponse>;
