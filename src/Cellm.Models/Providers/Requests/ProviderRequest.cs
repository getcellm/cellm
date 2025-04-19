using Cellm.Models.Prompts;
using Cellm.Models.Providers;
using MediatR;

namespace Cellm.Models.Providers;

internal record ProviderRequest(Prompt Prompt, Provider Provider) : IRequest<ProviderResponse>;
