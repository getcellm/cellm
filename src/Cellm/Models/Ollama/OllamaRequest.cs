using Cellm.Prompts;

namespace Cellm.Models.Ollama;

internal record OllamaRequest(Prompt Prompt, string? Provider, Uri? BaseAddress) : IModelRequest<OllamaResponse>;
