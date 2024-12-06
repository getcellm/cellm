using Cellm.Models.Prompts;

namespace Cellm.Models.OpenAiCompatible;

internal record OpenAiCompatibleResponse(Prompt Prompt) : IModelResponse;
