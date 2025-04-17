using Cellm.Models.Prompts;
using Microsoft.Extensions.AI;

namespace Cellm.Models.Providers.Cellm;

internal record CellmResponse(Prompt Prompt, ChatResponse ChatResponse) : IModelResponse;
