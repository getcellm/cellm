using Cellm.Prompts;

namespace Cellm.Models;

public interface IModelResponse
{
    Prompt Prompt { get; }
}