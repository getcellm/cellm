using Microsoft.Extensions.AI;

namespace Cellm.Models.Providers.OpenAi;

internal record OpenAiStreamResponse(StreamingChatCompletionUpdate Update) : IModelStreamResponse;
