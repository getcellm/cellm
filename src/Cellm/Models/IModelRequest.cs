using Cellm.Prompts;
using MediatR;

namespace Cellm.Models;

public interface IModelRequest<TResponse> : IRequest<TResponse>
{
    Prompt Prompt { get; }
}