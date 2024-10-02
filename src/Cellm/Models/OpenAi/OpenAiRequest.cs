using Cellm.Prompts;
using MediatR;

namespace Cellm.Models.OpenAi;

internal record OpenAiRequest(Prompt Prompt, string? Provider, string? Model, Uri? BaseAddress) : IRequest<OpenAiResponse>;
