using Cellm.Models.Prompts;

namespace Cellm.Models.Providers.Anthropic;

internal record AnthropicResponse(Prompt Prompt) : IModelResponse;