using MediatR;

namespace Cellm.Models;

internal interface IModelRequestHandler<TRequest, TResponse> : IRequestHandler<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public string Serialize(TRequest request);

    public TResponse Deserialize(TRequest request, string response);
}
