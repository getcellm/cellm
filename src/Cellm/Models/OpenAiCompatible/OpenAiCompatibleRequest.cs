using Cellm.Prompts;

namespace Cellm.Models.OpenAiCompatible;

internal record OpenAiCompatibleRequest(Prompt Prompt, string Provider, Uri BaseAddress) : IModelRequest<OpenAiCompatibleResponse>;
