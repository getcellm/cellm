using System.ComponentModel;

namespace Cellm.Tools;

internal interface IFunction<TRequest, TResponse>
{
    public TResponse Handle(TRequest request);
}
