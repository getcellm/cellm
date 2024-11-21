using Cellm.AddIn;
using Cellm.Tools;
using MediatR;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace Cellm.Models.ModelRequestBehavior;

internal class ToolBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IModelRequest<TResponse>
{
    private readonly CellmConfiguration _cellmConfiguration;
    private readonly Functions _functions;
    private readonly List<AITool> _tools;

    public ToolBehavior(IOptions<CellmConfiguration> cellmConfiguration, Functions functions)
    {
        _cellmConfiguration = cellmConfiguration.Value;
        _functions = functions;
        _tools = [
            AIFunctionFactory.Create(_functions.GlobRequest),
            AIFunctionFactory.Create(_functions.FileReaderRequest)
        ];
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (_cellmConfiguration.EnableTools)
        {
            request.Prompt.Options.Tools = _tools;
        }
        
        var response = await next();

        return response;
    }
}
