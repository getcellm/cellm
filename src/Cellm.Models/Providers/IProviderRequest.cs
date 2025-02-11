using MediatR;

namespace Cellm.Models;

internal interface IProviderRequest<TResponse> : IRequest<TResponse>
{
}
