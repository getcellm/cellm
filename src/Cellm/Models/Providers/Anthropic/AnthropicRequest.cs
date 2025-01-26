using Cellm.Models.Prompts;

namespace Cellm.Models.Providers.Anthropic;

internal record AnthropicRequest(Prompt Prompt, string? Provider, Uri? BaseAddress = null) : IModelRequest<AnthropicResponse>;