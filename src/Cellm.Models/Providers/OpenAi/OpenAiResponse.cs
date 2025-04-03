using Cellm.Models.Prompts;
using Microsoft.Extensions.AI;

namespace Cellm.Models.Providers.OpenAi;

internal record OpenAiResponse(Prompt Prompt, ChatResponse ChatResponse) : IModelResponse;
