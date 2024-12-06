using Cellm.Models.Behaviors;
using MediatR;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace Cellm.Models.Tools;

internal class ToolBehavior<TRequest, TResponse>(IOptions<ToolConfiguration> toolConfiguration, IEnumerable<AIFunction> functions)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : IModelRequest<TResponse>
{
    private readonly ToolConfiguration _toolConfiguration = toolConfiguration.Value;
    private readonly List<AITool> _tools = new(functions);

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (_toolConfiguration.EnableTools)
        {
            request.Prompt.Options.Tools = _tools;
        }

        return await next();
    }
}
