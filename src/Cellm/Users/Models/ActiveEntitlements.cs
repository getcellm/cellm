using System.Text.Json.Serialization;

namespace Cellm.Users.Models;

internal class ActiveEntitlements()
{
    [JsonRequired]
    [JsonPropertyName("email_address")]
    public string EmailAddress { get; init; } = string.Empty;

    [JsonRequired]
    [JsonPropertyName("entitlements")]
    public List<RawEntitlement> RawEntitlements { get; init; } = [];

    // Entitlements for anonymous users
    [JsonIgnore]
    private readonly List<Entitlement> AnonymousEntitlements = [
        Entitlement.EnableAnthropicProvider,
        Entitlement.EnableOllamaProvider,
        Entitlement.EnableOpenAiProvider
    ];

    [JsonIgnore]
    public IReadOnlyList<Entitlement> Entitlements
    {
        get
        {
            var entitlements = new List<Entitlement>();

            foreach (var rawEntitlement in RawEntitlements)
            {
                if (string.IsNullOrEmpty(rawEntitlement.Name)) continue;

                // Convert kebab-case to PascalCase for enum matching
                string enumCandidateName = rawEntitlement.Name.Replace("-", "");

                // Attempt to parse, case insensitive. Ignore if TryParse fails.
                if (Enum.TryParse<Entitlement>(enumCandidateName, ignoreCase: true, out var parsedEntitlement))
                {
                    entitlements.Add(parsedEntitlement);
                }
            }

            if (entitlements.Count == 0)
            {
                return AnonymousEntitlements;
            }

            return entitlements;
        }
    }
    public class RawEntitlement
    {
        public string Name { get; init; } = string.Empty;

        public Dictionary<string, string> Metadata { get; init; } = [];
    }
}