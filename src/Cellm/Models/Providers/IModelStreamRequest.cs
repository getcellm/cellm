using Cellm.Models.Prompts;
using MediatR;

namespace Cellm.Models;

internal interface IModelStreamRequest<IModelStreamResponse> : IStreamRequest<IModelStreamResponse>
{
    Prompt Prompt { get; }
}