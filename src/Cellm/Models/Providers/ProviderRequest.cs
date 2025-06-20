using Cellm.Models.Behaviors;
using Cellm.Models.Prompts;
using MediatR;

namespace Cellm.Models.Providers;

internal record ProviderRequest(Prompt Prompt, Provider Provider) : IGetPrompt, IGetProvider, IRequest<ProviderResponse>;
