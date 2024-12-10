using Cellm.Models.Prompts;

namespace Cellm.Models.Providers.Ollama;

internal record OllamaResponse(Prompt Prompt) : IModelResponse;