using MediatR;

namespace Cellm.Models.PipelineBehavior;

internal class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IModelRequest<TResponse>
{
    private readonly ICache _cache;

    public CachingBehavior(ICache cache)
    {
        _cache = cache;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(request, out object? value) && value is TResponse response)
        {
            return response;
        }

        response = await next();

        // Tool results depend on state external to prompt and should not be cached
        if (!request.Prompt.Messages.Any(x => x.Role == Prompts.Roles.Tool))
        {
            _cache.Set(request, response);
        }

        return response;
    }
}