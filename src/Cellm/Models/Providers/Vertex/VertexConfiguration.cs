using Cellm.Users;
using Microsoft.Extensions.AI;

namespace Cellm.Models.Providers.Vertex;

internal class VertexConfiguration : IProviderConfiguration
{
    public Provider Id { get => Provider.Vertex; }

    public string Name { get => "Vertex AI"; }

    public Entitlement Entitlement { get => Entitlement.EnableVertexProvider; }

    public string Icon { get => $"AddIn/UserInterface/Resources/{nameof(Provider.Vertex)}.png"; }

    public Uri BaseAddress { get; init; } = new Uri("https://us-central1-aiplatform.googleapis.com/v1beta1/projects/YOUR_PROJECT_ID/locations/us-central1/endpoints/openapi");

    public string DefaultModel { get; init; } = string.Empty;

    public string ApiKey { get; init; } = string.Empty;

    public string SmallModel { get; init; } = string.Empty;

    public string MediumModel { get; init; } = string.Empty;

    public string LargeModel { get; init; } = string.Empty;

    public AdditionalPropertiesDictionary? AdditionalProperties { get; init; } = [];

    public bool SupportsJsonSchemaResponses { get; init; } = true;

    public bool SupportsStructuredOutputWithTools { get; init; } = false;

    public bool IsEnabled { get; init; } = false;
}
