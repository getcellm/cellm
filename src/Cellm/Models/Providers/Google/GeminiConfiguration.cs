﻿using Cellm.Users;

namespace Cellm.Models.Providers.Google;

internal class GeminiConfiguration : IProviderConfiguration
{
    public Provider Id { get => Provider.Gemini; }

    public string Name { get => "Gemini"; }

    public Entitlement Entitlement { get => Entitlement.EnableGeminiProvider; }

    public string Icon { get => $"AddIn/UserInterface/Resources/{nameof(Provider.Gemini)}.png"; }

    public Uri BaseAddress => new("https://generativelanguage.googleapis.com/v1beta/openai/");

    public string DefaultModel { get; init; } = string.Empty;

    public string ApiKey { get; init; } = string.Empty;

    public string SmallModel { get; init; } = string.Empty;

    public string MediumModel { get; init; } = string.Empty;

    public string LargeModel { get; init; } = string.Empty;

    public bool CanUseStructuredOutputWithTools { get; init; } = false;

    public bool IsEnabled { get; init; } = false;
}
