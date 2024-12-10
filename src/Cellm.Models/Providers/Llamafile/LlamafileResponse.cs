using Cellm.Models.Prompts;

namespace Cellm.Models.Providers.Llamafile;

internal record LlamafileResponse(Prompt Prompt) : IModelResponse;