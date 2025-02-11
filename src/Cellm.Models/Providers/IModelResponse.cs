using Cellm.Models.Prompts;
using Microsoft.Extensions.AI;

namespace Cellm.Models;

public interface IModelResponse
{
    Prompt Prompt { get; }

    ChatCompletion ChatCompletion { get; }
}