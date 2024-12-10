using Cellm.Models.Prompts;

namespace Cellm.Models.Providers.Llamafile;

internal record LlamafileRequest(Prompt Prompt) : IProviderRequest<LlamafileResponse>;