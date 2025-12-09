using System.Text.Json.Serialization;

namespace Cellm.Users.Models;

internal class Entitlements()
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
        Entitlement.EnableDeepSeekProvider,
        Entitlement.EnableGeminiProvider,
        Entitlement.EnableMistralProvider,
        Entitlement.EnableModelContextProtocol,
        Entitlement.EnableOllamaProvider,
        Entitlement.EnableOpenAiProvider,
        Entitlement.EnableOpenAiCompatibleProvider,
        Entitlement.EnableOpenAiCompatibleProviderLocalModels
    ];

    public IEnumerable<Entitlement> AsEnumerable()
    {
        var entitlements = new List<Entitlement>();

        foreach (var rawEntitlement in RawEntitlements)
        {
            if (string.IsNullOrEmpty(rawEntitlement.Name)) continue;

            // Convert kebab-case to PascalCase for enum matching
            var enumCandidateName = rawEntitlement.Name.Replace("-", "");

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