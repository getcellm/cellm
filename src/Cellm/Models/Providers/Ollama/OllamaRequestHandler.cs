using System.Text;
using System.Text.Json;
using Cellm.Models.Prompts;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cellm.Models.Providers.Ollama;

internal class OllamaRequestHandler(
    IOptionsMonitor<OllamaConfiguration> ollamaConfiguration,
    [FromKeyedServices(Provider.Ollama)] IChatClient chatClient,
    HttpClient httpClient) : IModelRequestHandler<OllamaRequest, OllamaResponse>
{
    public async Task<OllamaResponse> Handle(OllamaRequest request, CancellationToken cancellationToken)
    {
        // Pull model if it doesn't exist
        var json = await httpClient.GetStringAsync(new Uri(ollamaConfiguration.CurrentValue.BaseAddress, "api/tags"), cancellationToken);

        if (!JsonDocument.Parse(json).RootElement
            .GetProperty("models")
            .EnumerateArray()
            .Select(model => model.GetProperty("name").GetString())
            .Contains(request.Prompt.Options.ModelId))
        {
            var body = new StringContent($"{{\"model\": \"{request.Prompt.Options.ModelId}\", \"stream\": false}}", Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(new Uri(ollamaConfiguration.CurrentValue.BaseAddress, "api/pull"), body, cancellationToken);
            response.EnsureSuccessStatusCode();
        }

        var chatCompletion = await chatClient.CompleteAsync(
            request.Prompt.Messages,
            request.Prompt.Options,
            cancellationToken);

        var prompt = new PromptBuilder(request.Prompt)
            .AddMessage(chatCompletion.Message)
            .Build();

        return new OllamaResponse(prompt);
    }
}
