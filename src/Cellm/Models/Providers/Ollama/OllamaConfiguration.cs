﻿using Cellm.Users;
using Microsoft.Extensions.AI;

namespace Cellm.Models.Providers.Ollama;

internal class OllamaConfiguration : IProviderConfiguration
{
    public Provider Id { get => Provider.Ollama; }

    public string Name { get => "Ollama"; }

    public Entitlement Entitlement { get => Entitlement.EnableOllamaProvider; }

    public string Icon { get => $"AddIn/UserInterface/Resources/{nameof(Provider.Ollama)}.png"; }

    public Uri BaseAddress => new("http://127.0.0.1:11434/");

    public string DefaultModel { get; init; } = string.Empty;

    public string SmallModel { get; init; } = string.Empty;

    public string MediumModel { get; init; } = string.Empty;

    public string LargeModel { get; init; } = string.Empty;

    public int MaxInputTokens { get; init; } = 16364;

    public AdditionalPropertiesDictionary AdditionalProperties { get; init; } = [];

    public bool CanUseStructuredOutputWithTools { get; init; } = false;

    public bool IsEnabled { get; init; } = false;
}