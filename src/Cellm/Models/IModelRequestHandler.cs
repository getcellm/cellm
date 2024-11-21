using MediatR;

namespace Cellm.Models;

internal interface IModelRequestHandler<TRequest, TResponse> : IRequestHandler<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
}
