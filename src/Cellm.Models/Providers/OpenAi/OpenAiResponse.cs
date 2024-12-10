using Cellm.Models.Prompts;

namespace Cellm.Models.Providers.OpenAi;

internal record OpenAiResponse(Prompt Prompt) : IModelResponse;
