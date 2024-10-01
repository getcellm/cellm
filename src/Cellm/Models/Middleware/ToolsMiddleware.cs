using Cellm.AddIn.Exceptions;
using Cellm.Prompts;
using MediatR;
using Sentry.Protocol;

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

        var lastMessage = response.Prompt.Messages.LastOrDefault();

        if (lastMessage?.Role == Role.Tool)
        {
            request.Prompt.Messages.Add(RunTool(lastMessage));
            response = await _sender.Send(request, cancellationToken);
        }

        return response;
    }

    private Message RunTool(Message message)
    {
        message.ToolCalls.Select(x => _tools.Run(x));
        var response = await _sender.Send(_tools.GetRequest(message.ToolCalls) ) 
        return new Message()
    }
}