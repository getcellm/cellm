using Cellm.Prompts;

namespace Cellm.Models;

internal interface IClient
{
    public Task<Prompt> Send(Prompt prompt, string? provider, string? model, Uri? baseAddress);
}