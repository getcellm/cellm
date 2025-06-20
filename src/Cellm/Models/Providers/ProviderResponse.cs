using Cellm.Models.Behaviors;
using Cellm.Models.Prompts;
using Microsoft.Extensions.AI;

namespace Cellm.Models.Providers;

internal record ProviderResponse(Prompt Prompt, ChatResponse ChatResponse) : IGetPrompt, IChatResponse;
