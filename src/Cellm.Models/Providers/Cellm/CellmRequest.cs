using Cellm.Models.Behaviors;
using Cellm.Models.Prompts;
using Cellm.User;

namespace Cellm.Models.Providers.Cellm;

internal record CellmRequest(Prompt Prompt) : IModelRequest<CellmResponse>;
