using Cellm.Models.Prompts;

namespace Cellm.Models.OpenAiCompatible;

internal record OpenAiCompatibleRequest(Prompt Prompt, Uri? BaseAddress) : IModelRequest<OpenAiCompatibleResponse>;
