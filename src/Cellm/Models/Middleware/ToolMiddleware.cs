using Cellm.Prompts;
using Cellm.Tools;
using MediatR;
using Microsoft.Office.Core;

namespace Cellm.Models.Middleware;

internal class ToolMiddleware<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IHasPrompt, IRequest<TResponse>
    where TResponse : IHasPrompt
{
    private readonly ISender _sender;
    private readonly ITools _tools;

    public ToolMiddleware(ISender sender, ITools tools)
    {
        _sender = sender;
        _tools = tools;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var response = await next();

        var toolRequests = response.Prompt.Messages.LastOrDefault()?.ToolRequests;

        if (toolRequests is not null)
        {
            // Model called tools, run tools and call model again
            request.Prompt.Messages.Add(await RunTools(toolRequests));
            response = await _sender.Send(request, cancellationToken);
        }

        return response;
    }

    private async Task<Message> RunTools(List<ToolRequest> toolRequests)
    {
        var toolResponses = await Task.WhenAll(toolRequests.Select(x => _tools.Run(x)));
        var toolRequestsWithResponses = toolRequests
            .Zip(toolResponses, (toolRequest, toolResponse) => toolRequest with { Response = toolResponse })
            .ToList();

        return new Message(string.Empty, Roles.Tool, toolRequestsWithResponses);
    }
}