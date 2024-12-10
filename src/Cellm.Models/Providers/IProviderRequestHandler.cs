using MediatR;

namespace Cellm.Models;

internal interface IProviderRequestHandler<TRequest, TResponse> : IRequestHandler<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
}
