﻿namespace Cellm.Services.Configuration;

internal class OpenAiConfiguration
{
    public Uri BaseAddress { get; init; }

    public string DefaultModel { get; init; }

    public string ApiKey { get; init; }

    public OpenAiConfiguration()
    {
        BaseAddress = default!;
        DefaultModel = default!;
        ApiKey = default!;
    }
}