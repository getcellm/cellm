namespace Cellm.Users.Models;

public class RawEntitlement
{
    public string Name { get; init; } = string.Empty;

    public Dictionary<string, string> Metadata { get; init; } = [];
}