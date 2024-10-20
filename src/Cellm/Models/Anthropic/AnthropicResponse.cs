using Cellm.Prompts;

namespace Cellm.Models.Anthropic;

internal record AnthropicResponse(Prompt Prompt) : IModelResponse;