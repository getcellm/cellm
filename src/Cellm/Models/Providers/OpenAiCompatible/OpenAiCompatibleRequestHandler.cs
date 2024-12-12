﻿using Cellm.Models.Prompts;
using Microsoft.Extensions.Options;

namespace Cellm.Models.Providers.OpenAiCompatible;

internal class OpenAiCompatibleRequestHandler(
    OpenAiCompatibleChatClientFactory openAiCompatibleChatClientFactory,
    IOptions<OpenAiCompatibleConfiguration> openAiCompatibleConfiguration)
    : IModelRequestHandler<OpenAiCompatibleRequest, OpenAiCompatibleResponse>
{
    private readonly OpenAiCompatibleConfiguration _openAiCompatibleConfiguration = openAiCompatibleConfiguration.Value;

    public async Task<OpenAiCompatibleResponse> Handle(OpenAiCompatibleRequest request, CancellationToken cancellationToken)
    {
        var chatClient = openAiCompatibleChatClientFactory.Create(
            request.BaseAddress ?? _openAiCompatibleConfiguration.BaseAddress,
            request.Prompt.Options.ModelId ?? _openAiCompatibleConfiguration.DefaultModel,
            request.ApiKey ?? _openAiCompatibleConfiguration.ApiKey);

        var chatCompletion = await chatClient.CompleteAsync(request.Prompt.Messages, request.Prompt.Options, cancellationToken);

        var prompt = new PromptBuilder(request.Prompt)
            .AddMessage(chatCompletion.Message)
            .Build();

        return new OpenAiCompatibleResponse(prompt);
    }
}
