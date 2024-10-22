using Cellm.Prompts;
using MediatR;

namespace Cellm.Models;

internal interface IModelRequest<TResponse> : IRequest<TResponse>
{
    Prompt Prompt { get; }
}