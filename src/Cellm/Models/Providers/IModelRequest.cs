using Cellm.Models.Prompts;
using MediatR;

namespace Cellm.Models;

internal interface IModelRequest<IModelResponse> : IRequest<IModelResponse>
{
    Prompt Prompt { get; }
}