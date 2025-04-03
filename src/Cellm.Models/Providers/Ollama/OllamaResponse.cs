using Cellm.Models.Prompts;
using Microsoft.Extensions.AI;

namespace Cellm.Models.Providers.Ollama;

internal record OllamaResponse(Prompt Prompt, ChatResponse ChatResponse) : IModelResponse;