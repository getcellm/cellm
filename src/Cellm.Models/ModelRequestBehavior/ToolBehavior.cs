using Cellm.AddIn;
using Cellm.Tools;
using MediatR;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace Cellm.Models.ModelRequestBehavior;

internal class ToolBehavior<TRequest, TResponse>(IOptions<CellmConfiguration> cellmConfiguration, Functions functions)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : IModelRequest<TResponse>
{
    private readonly CellmConfiguration _cellmConfiguration = cellmConfiguration.Value;
    private readonly List<AITool> _tools = [
        AIFunctionFactory.Create(functions.GlobRequest),
        AIFunctionFactory.Create(functions.FileReaderRequest)
    ];

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (_cellmConfiguration.EnableTools)
        {
            request.Prompt.Options.Tools = _tools;
        }

        return await next();
    }
}
