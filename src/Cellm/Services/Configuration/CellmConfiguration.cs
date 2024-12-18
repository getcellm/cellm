﻿namespace Cellm.Services.Configuration;

public class CellmConfiguration
{
    public bool Debug { get; init; }

    public string DefaultProvider { get; init; } = string.Empty;

    public string DefaultModel { get; init; } = string.Empty;

    public double DefaultTemperature { get; init; }

    public int HttpTimeoutInSeconds { get; init; }

    public int CacheTimeoutInSeconds { get; init; }

    public bool EnableCache { get; init; }

    public bool EnableTools { get; init; }
}

