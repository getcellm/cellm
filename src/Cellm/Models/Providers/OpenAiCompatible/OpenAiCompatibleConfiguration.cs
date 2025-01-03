﻿namespace Cellm.Models.Providers.OpenAiCompatible;

internal class OpenAiCompatibleConfiguration
{
    public Uri BaseAddress { get; set; } = default!;

    public string DefaultModel { get; init; } = string.Empty;

    public string ApiKey { get; init; } = string.Empty;
}
