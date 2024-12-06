using Cellm.Models.Prompts;

namespace Cellm.Models.Anthropic;

internal record AnthropicRequest(Prompt Prompt, string? Provider, Uri? BaseAddress) : IModelRequest<AnthropicResponse>;