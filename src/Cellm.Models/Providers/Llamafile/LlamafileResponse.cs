using Cellm.Models.Prompts;

namespace Cellm.Models.Llamafile;

internal record LlamafileResponse(Prompt Prompt) : IModelResponse;