using Cellm.Models.Prompts;

namespace Cellm.Models.Providers.OpenAiCompatible;

internal record OpenAiCompatibleRequest(Prompt Prompt, Provider Provider) : IModelRequest<OpenAiCompatibleResponse>;
