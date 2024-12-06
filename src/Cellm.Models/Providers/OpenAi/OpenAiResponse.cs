using Cellm.Models.Prompts;

namespace Cellm.Models.OpenAi;

internal record OpenAiResponse(Prompt Prompt) : IModelResponse;
