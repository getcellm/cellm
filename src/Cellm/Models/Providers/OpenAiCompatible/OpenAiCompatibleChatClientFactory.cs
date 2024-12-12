﻿using System.ClientModel;
using System.ClientModel.Primitives;
using Microsoft.Extensions.AI;
using OpenAI;

namespace Cellm.Models.Providers.OpenAiCompatible;

internal class OpenAiCompatibleChatClientFactory(HttpClient httpClient)
{
    public IChatClient Create(Uri BaseAddress, string modelId, string apiKey)
    {
        var openAiClient = new OpenAIClient(
            new ApiKeyCredential(apiKey),
            new OpenAIClientOptions
            {
                Transport = new HttpClientPipelineTransport(httpClient),
                Endpoint = BaseAddress
            });

        return new ChatClientBuilder(openAiClient.AsChatClient(modelId))
            .UseFunctionInvocation()
            .Build();
    }
}
