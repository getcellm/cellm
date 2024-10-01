using Cellm.Prompts;
using MediatR;

namespace Cellm.Models.Middleware;

internal class CachingMiddleware<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : IHasPrompt
{
    private readonly ICache _cache;

    public CachingMiddleware(ICache cache)
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

        // Tool responses must not be cached because their results depend on external state
        if (response.Prompt.Messages.All(x => x.Role != Role.Tool))
        {
            _cache.Set(request, response);
        }

        return response;
    }
}