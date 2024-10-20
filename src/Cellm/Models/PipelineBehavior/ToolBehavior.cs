using Cellm.Models.OpenAi;
using Cellm.Prompts;
using Cellm.Tools;
using MediatR;

namespace Cellm.Models.PipelineBehavior;

internal class ToolBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IModelRequest<TResponse>
    where TResponse : IModelResponse
{
    private readonly ISender _sender;
    private readonly ITools _tools;

    public ToolBehavior(ISender sender, ITools tools)
    {
        _sender = sender;
        _tools = tools;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var response = await next();

        var toolCalls = response.Prompt.Messages.LastOrDefault()?.ToolCalls;

        if (toolCalls is not null)
        {
            // Model called tools, run tools and call model again
            request.Prompt.Messages.Add(await RunTools(toolCalls));
            response = await _sender.Send(request, cancellationToken);
        }

        return response;
    }

    private async Task<Message> RunTools(List<ToolCall> toolCalls)
    {
        var toolResults = await Task.WhenAll(toolCalls.Select(x => _tools.Run(x)));
        var toolCallsWithResults = toolCalls
            .Zip(toolResults, (toolCall, toolResult) => toolCall with { Result = toolResult })
            .ToList();

        return new Message(string.Empty, Roles.Tool, toolCallsWithResults);
    }
}