using Cellm.Prompts;

namespace Cellm.Models.Ollama;

internal record OllamaRequest(Prompt Prompt) : IModelRequest<OllamaResponse>;
