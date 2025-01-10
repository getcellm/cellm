using System.Runtime.CompilerServices;
using Cellm.Models.Prompts;
using MediatR;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace Cellm.Models.Providers.OpenAi;

internal class OpenAiHandler([FromKeyedServices(Provider.OpenAi)] IChatClient chatClient) : 
    IModelRequestHandler<OpenAiRequest, OpenAiResponse>, IModelStreamRequestHandler<OpenAiStreamRequest, OpenAiStreamResponse>
{

    public async Task<OpenAiResponse> Handle(OpenAiRequest request, CancellationToken cancellationToken)
    {
        var chatCompletion = await chatClient.CompleteAsync(request.Prompt.Messages, request.Prompt.Options, cancellationToken);

        var prompt = new PromptBuilder(request.Prompt)
            .AddMessage(chatCompletion.Message)
            .Build();

        return new OpenAiResponse(prompt);
    }

    public async IAsyncEnumerable<OpenAiStreamResponse> Handle(OpenAiStreamRequest request, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var update in chatClient.CompleteStreamingAsync(
            request.Prompt.Messages,
            request.Prompt.Options,
            cancellationToken))
        {
            yield return new OpenAiStreamResponse(update);
        }
    }
}
