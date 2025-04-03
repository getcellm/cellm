using Cellm.Models.Prompts;
using Microsoft.Extensions.AI;

namespace Cellm.Models.Providers.Anthropic;

internal record AnthropicResponse(Prompt Prompt, ChatResponse ChatResponse) : IModelResponse;