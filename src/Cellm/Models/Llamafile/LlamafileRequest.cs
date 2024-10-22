using Cellm.Prompts;

namespace Cellm.Models.Llamafile;

internal record LlamafileRequest(Prompt Prompt) : IProviderRequest<LlamafileResponse>;