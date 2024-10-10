using Cellm.Prompts;

namespace Cellm.Models;

internal interface IClient
{
    public Task<Prompt> Send(Prompt prompt, string? provider, Uri? baseAddress);
}