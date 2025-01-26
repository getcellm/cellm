using Cellm.Models.Providers.OpenAiCompatible;
using MediatR;
using Microsoft.Extensions.Options;

namespace Cellm.Models.Providers.Llamafile;

internal class LlamafileRequestHandler(ISender sender, IOptions<LlamafileConfiguration> llamafileConfiguration) : IProviderRequestHandler<LlamafileRequest, LlamafileResponse>
{

    public async Task<LlamafileResponse> Handle(LlamafileRequest request, CancellationToken cancellationToken)
    {
        request.Prompt.Options.ModelId ??= llamafileConfiguration.Value.DefaultModel;

        var openAiResponse = await sender.Send(new OpenAiCompatibleRequest(request.Prompt, llamafileConfiguration.Value.BaseAddress, llamafileConfiguration.Value.ApiKey), cancellationToken);

        return new LlamafileResponse(openAiResponse.Prompt);
    }
}

