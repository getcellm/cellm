
namespace Cellm.Models.OpenAiCompatible;

internal class OpenAiCompatibleRequestHandler : IModelRequestHandler<OpenAiCompatibleRequest, OpenAiCompatibleResponse>
{
    public Task<OpenAiCompatibleResponse> Handle(OpenAiCompatibleRequest request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
