using Cellm.Models.Prompts;

namespace Cellm.Models.Providers.OpenAiCompatible;

internal record OpenAiCompatibleResponse(Prompt Prompt) : IModelResponse;
