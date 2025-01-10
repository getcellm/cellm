using Microsoft.Extensions.AI;

namespace Cellm.Models;

public interface IModelStreamResponse
{
    StreamingChatCompletionUpdate Update { get; }
}