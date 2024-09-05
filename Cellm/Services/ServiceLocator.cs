﻿using Cellm.AddIn;
using Cellm.Exceptions;
using Cellm.ModelProviders;
using Cellm.Services.Configuration;
using ExcelDna.Integration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cellm.Services;

internal static class ServiceLocator
{
    private static readonly Lazy<IServiceProvider> _serviceProvider = new(() => ConfigureServices(new ServiceCollection()).BuildServiceProvider());
    public static IServiceProvider ServiceProvider => _serviceProvider.Value;

    public static T Get<T>() where T : notnull
    {
        return ServiceProvider.GetRequiredService<T>();
    }

    private static IServiceCollection ConfigureServices(IServiceCollection services)
    {
        // Configurations
        var basePath = ExcelDnaUtil.XllPathInfo?.Directory?.FullName ??
            throw new CellmException($"Unable to configure app, invalid value for ExcelDnaUtil.XllPathInfo='{ExcelDnaUtil.XllPathInfo}'");

        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.Local.json", true)
            .Build();

        services
            .Configure<CellmAddInConfiguration>(configuration.GetRequiredSection(nameof(CellmAddInConfiguration)))
            .Configure<AnthropicConfiguration>(configuration.GetRequiredSection(nameof(AnthropicConfiguration)))
            .Configure<GoogleConfiguration>(configuration.GetRequiredSection(nameof(GoogleConfiguration)))
            .Configure<OpenAiConfiguration>(configuration.GetRequiredSection(nameof(OpenAiConfiguration)))
            .Configure<RateLimiterConfiguration>(configuration.GetRequiredSection(nameof(RateLimiterConfiguration)))
            .Configure<CircuitBreakerConfiguration>(configuration.GetRequiredSection(nameof(CircuitBreakerConfiguration)))
            .Configure<RetryConfiguration>(configuration.GetRequiredSection(nameof(RetryConfiguration)))
            .AddSingleton<ResiliencePipelineConfigurator>();

        // Internals
        services
            .AddTransient<ArgumentParser>()
            .AddSingleton<IClientFactory, ClientFactory>()
            .AddSingleton<IClient, Client>()
            .AddSingleton<ICache, Cache>()
            .AddMemoryCache();

        // Model Providers
        var resiliencePipelineConfigurator = new ResiliencePipelineConfigurator(configuration);

        var anthropicConfiguration = configuration.GetRequiredSection(nameof(AnthropicConfiguration)).Get<AnthropicConfiguration>()
            ?? throw new NullReferenceException(nameof(AnthropicConfiguration));

        services.AddHttpClient<AnthropicClient>(anthropicHttpClient =>
        {
            anthropicHttpClient.BaseAddress = anthropicConfiguration.BaseAddress;
            anthropicHttpClient.DefaultRequestHeaders.Add("x-api-key", anthropicConfiguration.ApiKey);
            anthropicHttpClient.DefaultRequestHeaders.Add("anthropic-version", anthropicConfiguration.Version);
        }).AddResilienceHandler("AnthropicResiliencePipeline", resiliencePipelineConfigurator.ConfigureResiliencePipeline);

        var googleConfiguration = configuration.GetRequiredSection(nameof(GoogleConfiguration)).Get<GoogleConfiguration>()
            ?? throw new NullReferenceException(nameof(GoogleConfiguration));

        services.AddHttpClient<GoogleClient>(googleHttpClient =>
        {
            googleHttpClient.BaseAddress = googleConfiguration.BaseAddress;
        }).AddResilienceHandler("GoogleResiliencePipeline", resiliencePipelineConfigurator.ConfigureResiliencePipeline);

        var openAiConfiguration = configuration.GetRequiredSection(nameof(OpenAiConfiguration)).Get<OpenAiConfiguration>()
            ?? throw new NullReferenceException(nameof(OpenAiConfiguration));

        services.AddHttpClient<OpenAiClient>(openAiHttpClient =>
        {
            openAiHttpClient.BaseAddress = openAiConfiguration.BaseAddress;
            openAiHttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {openAiConfiguration.ApiKey}");
        }).AddResilienceHandler("OpenAiResiliencePipeline", resiliencePipelineConfigurator.ConfigureResiliencePipeline);

        return services;
    }
}