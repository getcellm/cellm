using MediatR;

namespace Cellm.Models;

internal interface IModelStreamRequestHandler<TRequest, TResponse> : IStreamRequestHandler<TRequest, TResponse>
    where TRequest : IStreamRequest<TResponse>
{
}
