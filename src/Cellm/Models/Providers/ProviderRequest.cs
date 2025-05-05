using Cellm.Models.Prompts;
using MediatR;

namespace Cellm.Models.Providers;

internal record ProviderRequest(Prompt Prompt, Provider Provider) : IPrompt, IRequest<ProviderResponse>;
