using Cellm.Prompts;
using MediatR;

namespace Cellm.Models.Middleware;

internal class ToolsMiddleware<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IHasPrompt, IRequest<TResponse>
    where TResponse : IHasPrompt
{
    private readonly ISender _sender;

    public ToolsMiddleware(ISender sender)
    {
        _sender = sender;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var response = await next();

        var toolCalls = response.Prompt.Messages.LastOrDefault()?.ToolCalls;

        if (toolCalls is not null)
        {
            request.Prompt.Messages.AddRange(await RunTools(toolCalls));
            response = await _sender.Send(request, cancellationToken);
        }

        return response;
    }

    private async Task<IEnumerable<Message>> RunTools(List<ToolCall> toolCalls)
    {
        var results = await Task.WhenAll(toolCalls.Select(x => _sender.Send(ToolRequestFactory(x))));
        return results.Zip(toolCalls, (result, toolCall) => new Message(result, Role.Tool, new List<ToolCall> { toolCall }));
    }

    private IRequest<TResult> ToolRequestFactory(ToolCall x)
    {
        throw new NotImplementedException();
    }
}