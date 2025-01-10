using Cellm.Models.Prompts;

namespace Cellm.Models.Providers.OpenAi;

internal record OpenAiStreamRequest(Prompt Prompt) : IModelStreamRequest<OpenAiStreamResponse>;