﻿using System.Reflection;
using Cellm.AddIn;
using Cellm.AddIn.Exceptions;
using Cellm.Models;
using Cellm.Models.Behaviors;
using Cellm.Models.Providers;
using Cellm.Models.Providers.Anthropic;
using Cellm.Models.Providers.DeepSeek;
using Cellm.Models.Providers.Llamafile;
using Cellm.Models.Providers.Mistral;
using Cellm.Models.Providers.Ollama;
using Cellm.Models.Providers.OpenAi;
using Cellm.Models.Providers.OpenAiCompatible;
using Cellm.Models.Resilience;
using Cellm.Models.Tools;
using Cellm.Services.Configuration;
using Cellm.Tools;
using Cellm.Tools.FileReader;
using Cellm.Tools.ModelContextProtocol;
using ExcelDna.Integration;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cellm.Services;

internal static class ServiceLocator
{
    private static readonly Lazy<IServiceProvider> _serviceProvider = new(() => ConfigureServices(new ServiceCollection()).BuildServiceProvider());

    internal static string ConfigurationPath { get; set; } = ExcelDnaUtil.XllPathInfo?.Directory?.FullName ?? throw new NullReferenceException("Could not get Cellm path");

    public static IServiceProvider ServiceProvider => _serviceProvider.Value;

    private static IServiceCollection ConfigureServices(IServiceCollection services)
    {
        if (string.IsNullOrEmpty(ConfigurationPath))
        {
            throw new CellmException($"Unable to configure app, invalid value for ExcelDnaUtil.XllPathInfo='{ConfigurationPath}'");
        }

        // Configurations
        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(ConfigurationPath)
            .AddJsonFile("appsettings.json", reloadOnChange: true, optional: false)
            .AddJsonFile("appsettings.Local.json", reloadOnChange: true, optional: true)
            .Build();

        services
            .Configure<ProviderConfiguration>(configuration.GetRequiredSection(nameof(ProviderConfiguration)))
            .Configure<ModelContextProtocolConfiguration>(configuration.GetRequiredSection(nameof(ModelContextProtocolConfiguration)))
            .Configure<CellmConfiguration>(configuration.GetRequiredSection(nameof(CellmConfiguration)))
            .Configure<AnthropicConfiguration>(configuration.GetRequiredSection(nameof(AnthropicConfiguration)))
            .Configure<DeepSeekConfiguration>(configuration.GetRequiredSection(nameof(DeepSeekConfiguration)))
            .Configure<LlamafileConfiguration>(configuration.GetRequiredSection(nameof(LlamafileConfiguration)))
            .Configure<OllamaConfiguration>(configuration.GetRequiredSection(nameof(OllamaConfiguration)))
            .Configure<OpenAiConfiguration>(configuration.GetRequiredSection(nameof(OpenAiConfiguration)))
            .Configure<OpenAiCompatibleConfiguration>(configuration.GetRequiredSection(nameof(OpenAiCompatibleConfiguration)))
            .Configure<MistralConfiguration>(configuration.GetRequiredSection(nameof(MistralConfiguration)))
            .Configure<ResilienceConfiguration>(configuration.GetRequiredSection(nameof(ResilienceConfiguration)))
            .Configure<SentryConfiguration>(configuration.GetRequiredSection(nameof(SentryConfiguration)));

        // Logging
        var cellmConfiguration = configuration.GetRequiredSection(nameof(CellmConfiguration)).Get<CellmConfiguration>()
            ?? throw new NullReferenceException(nameof(CellmConfiguration));

        var sentryConfiguration = configuration.GetRequiredSection(nameof(SentryConfiguration)).Get<SentryConfiguration>()
            ?? throw new NullReferenceException(nameof(SentryConfiguration));

        services
          .AddLogging(loggingBuilder =>
          {
              var assembly = Assembly.GetExecutingAssembly();
              var gitVersionInformationType = assembly.GetType("GitVersionInformation");
              var assemblyInformationalVersion = gitVersionInformationType?.GetField("AssemblyInformationalVersion");

              loggingBuilder
                  .AddConsole()
                  .AddDebug()
                  .AddSentry(sentryLoggingOptions =>
                  {
                      sentryLoggingOptions.InitializeSdk = sentryConfiguration.IsEnabled;
                      sentryLoggingOptions.Release = GetReleaseVersion();
                      sentryLoggingOptions.Dsn = sentryConfiguration.Dsn;
                      sentryLoggingOptions.Debug = cellmConfiguration.Debug;
                      sentryLoggingOptions.DiagnosticLevel = SentryLevel.Debug;
                      sentryLoggingOptions.TracesSampleRate = sentryConfiguration.TracesSampleRate;
                      sentryLoggingOptions.ProfilesSampleRate = sentryConfiguration.ProfilesSampleRate;
                      sentryLoggingOptions.Environment = sentryConfiguration.Environment;
                      sentryLoggingOptions.AutoSessionTracking = true;
                      sentryLoggingOptions.IsGlobalModeEnabled = true;
                      sentryLoggingOptions.AddIntegration(new ProfilingIntegration());
                  });
          });

        // Internals
        services
            .AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
                cfg.AddOpenBehavior(typeof(SentryBehavior<,>));
                cfg.AddOpenBehavior(typeof(CacheBehavior<,>));
                cfg.AddOpenBehavior(typeof(ToolBehavior<,>));
            })
            .AddSingleton(configuration)
            .AddTransient<ArgumentParser>()
            .AddSingleton<Client>()
            .AddSingleton<Serde>()
            .AddRateLimiter(configuration)
            .AddRetryHttpClient(configuration);

#pragma warning disable EXTEXP0018 // Type is for evaluation purposes only and is subject to change or removal in future updates.
        services
            .AddHybridCache();
#pragma warning restore EXTEXP0018 // Type is for evaluation purposes only and is subject to change or removal in future updates.

        // Add providers
        services
            .AddAnthropicChatClient(configuration)
            .AddDeepSeekChatClient()
            .AddLlamafileChatClient()
            .AddMistralChatClient()
            .AddOllamaChatClient()
            .AddOpenAiChatClient()
            .AddOpenAiCompatibleChatClient();

        // Add tools
        services
            .AddSingleton<FileReaderFactory>()
            .AddSingleton<IFileReader, PdfReader>()
            .AddSingleton<IFileReader, TextReader>()
            .AddSingleton<NativeTools>()
            .AddTools(
                serviceProvider => AIFunctionFactory.Create(serviceProvider.GetRequiredService<NativeTools>().FileSearchRequest),
                serviceProvider => AIFunctionFactory.Create(serviceProvider.GetRequiredService<NativeTools>().FileReaderRequest));

        // Workarounds

        // https://github.com/openai/openai-dotnet/issues/297
        var metricsFilterDescriptor = services.FirstOrDefault(descriptor =>
            descriptor.ImplementationType?.ToString() == $"Microsoft.Extensions.Http.MetricsFactoryHttpMessageHandlerFilter");

        if (metricsFilterDescriptor is not null)
        {
            services.Remove(metricsFilterDescriptor);
        }

        return services;
    }

    public static string GetReleaseVersion()
    {
        var releaseVersion = "unknown";

        var value = Assembly
            .GetExecutingAssembly()
            .GetType("GitVersionInformation")?
            .GetField("AssemblyInformationalVersion")?
            .GetValue(null);

        if (value is string valueAsString)
        {
            releaseVersion = valueAsString;
        }

        return releaseVersion;
    }

    public static void Dispose()
    {
        if (_serviceProvider.IsValueCreated)
        {
            (_serviceProvider.Value as IDisposable)?.Dispose();
        }
    }
}
