using Cellm.Models.Prompts;

namespace Cellm.Models.Ollama;

internal record OllamaResponse(Prompt Prompt) : IModelResponse;