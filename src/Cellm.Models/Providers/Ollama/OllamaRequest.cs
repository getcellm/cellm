using Cellm.Models.Prompts;

namespace Cellm.Models.Providers.Ollama;

internal record OllamaRequest(Prompt Prompt) : IModelRequest<OllamaResponse>;
