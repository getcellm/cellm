using Cellm.Prompts;

namespace Cellm.Models.GoogleAi;

internal record GoogleAiResponse(Prompt Prompt) : IModelResponse;