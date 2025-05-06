using Cellm.Models.Prompts;
using MediatR;
using Microsoft.Extensions.AI;

namespace Cellm.Models.Providers;

internal record ProviderStreamRequest(Prompt Prompt, Provider Provider) : IPrompt, IStreamRequest<ChatResponseUpdate>;