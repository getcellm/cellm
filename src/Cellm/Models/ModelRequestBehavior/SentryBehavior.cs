using MediatR;

namespace Cellm.Models.ModelRequestBehavior;

internal class SentryBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var transaction = SentrySdk.StartTransaction(typeof(TRequest).Name, typeof(TRequest).Name);
        SentrySdk.ConfigureScope(scope => scope.Transaction = transaction);

        try
        {
            return await next();
        }
        finally
        {
            transaction.Finish();
        }
    }
}
