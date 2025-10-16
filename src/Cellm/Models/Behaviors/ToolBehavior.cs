using System.Collections.Concurrent;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Cellm.AddIn;
using Cellm.Tools.ModelContextProtocol;
using Cellm.Tools.ModelContextProtocol.Exceptions;
using Cellm.Users;
using MediatR;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;

namespace Cellm.Models.Behaviors;

internal class ToolBehavior<TRequest, TResponse>(
  Account account,
  IOptionsMonitor<CellmAddInConfiguration> cellmAddInConfiguration,
  IMcpConfigurationService mcpConfigurationService,
  IEnumerable<AIFunction> functions,
  ILogger<ToolBehavior<TRequest, TResponse>> logger,
  ILoggerFactory loggerFactory)
  : IPipelineBehavior<TRequest, TResponse>
  where TRequest : IGetPrompt
{
    // TODO: Cannot use HybridCache because McpClientTool instances can be serialized
    private readonly ConcurrentDictionary<string, IList<McpClientTool>> _cache = new();

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (cellmAddInConfiguration.CurrentValue.EnableTools.Any(t => t.Value))
        {
            request.Prompt.Options.Tools = [.. functions.Where(f => cellmAddInConfiguration.CurrentValue.EnableTools[f.Name])];
        }

        var enableModelContextProtocol = await account.HasEntitlementAsync(Entitlement.EnableModelContextProtocol, cancellationToken).ConfigureAwait(false);

        if (cellmAddInConfiguration.CurrentValue.EnableModelContextProtocolServers.Any(t => t.Value) && enableModelContextProtocol)
        {
            await foreach (var tool in GetMcpToolsAsync(cancellationToken))
            {
                request.Prompt.Options.Tools ??= [];
                request.Prompt.Options.Tools.Add(tool);
            }
        }

        if (request.Prompt.Options.Tools?.Any() ?? false)
        {
            logger.LogDebug("Tools enabled: {tools}", request.Prompt.Options.Tools);
        }
        else
        {
            logger.LogDebug("Tools disabled");
        }

        return await next().ConfigureAwait(false);
    }

    private async IAsyncEnumerable<McpClientTool> GetMcpToolsAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var stdioToolTasks = mcpConfigurationService.GetAllStdioServers()
            .Where(stdioClientTransportOptions => cellmAddInConfiguration.CurrentValue.EnableModelContextProtocolServers
                .TryGetValue(stdioClientTransportOptions.Name ?? throw new NullReferenceException(nameof(stdioClientTransportOptions.Name)), out var isEnabled) && isEnabled)
            .Select(stdioClientTransportOptions => GetOrFetchServerToolsAsync(stdioClientTransportOptions, cancellationToken))
            .ToList();

        var sseToolTasks = mcpConfigurationService.GetAllSseServers()
            .Where(sseClientTransportOptions => cellmAddInConfiguration.CurrentValue.EnableModelContextProtocolServers
                .TryGetValue(sseClientTransportOptions.Name ?? throw new NullReferenceException(nameof(sseClientTransportOptions.Name)), out var isEnabled) && isEnabled)
            .Select(sseClientTransportOptions => GetOrFetchServerToolsAsync(sseClientTransportOptions, cancellationToken))
            .ToList();

        List<Task<IList<McpClientTool>>> pendingTasks = [.. stdioToolTasks, .. sseToolTasks];

        while (pendingTasks.Count > 0)
        {
            var completedTask = await Task.WhenAny(pendingTasks).ConfigureAwait(false);
            pendingTasks.Remove(completedTask);

            foreach (var tool in await completedTask)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return tool;
            }
        }
    }

    private async Task<IList<McpClientTool>> GetOrFetchServerToolsAsync(StdioClientTransportOptions stdioClientTransportOptions, CancellationToken cancellationToken)
    {
        if (_cache.ContainsKey(stdioClientTransportOptions.Name ?? throw new NullReferenceException(nameof(stdioClientTransportOptions))) && _cache[stdioClientTransportOptions.Name] is IList<McpClientTool> cachedMcpClientTools)
        {
            logger.LogDebug("Using cached tools for {ServerName}", stdioClientTransportOptions.Name);
            return cachedMcpClientTools;
        }

        try
        {
            var clientTransport = new StdioClientTransport(stdioClientTransportOptions);
            var mcpClient = await McpClientFactory.CreateAsync(clientTransport, loggerFactory: loggerFactory, cancellationToken: cancellationToken).ConfigureAwait(false);
            var mcpClientTools = await mcpClient.ListToolsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

            _cache[stdioClientTransportOptions.Name ?? throw new NullReferenceException(nameof(stdioClientTransportOptions))] = mcpClientTools;

            return mcpClientTools;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "MCP server '{ServerName}' failed to start: {Message} ({Command})", stdioClientTransportOptions.Name, ex.Message, stdioClientTransportOptions.Command);
            throw new McpServerException($"The MCP server '{stdioClientTransportOptions.Name}' failed to start. Please check https://docs.getcellm.com/get-started/install for troubleshooting or disable the '{stdioClientTransportOptions.Name}' tool in the Cellm > Tools > MCP menu. Error message: {ex.Message}", ex);
        }
    }

    private async Task<IList<McpClientTool>> GetOrFetchServerToolsAsync(SseClientTransportOptions sseClientTransportOptions, CancellationToken cancellationToken)
    {
        if (_cache.ContainsKey(sseClientTransportOptions.Name ?? throw new NullReferenceException(nameof(sseClientTransportOptions))) && _cache[sseClientTransportOptions.Name] is IList<McpClientTool> cachedMcpClientTools)
        {
            logger.LogDebug("Using cached tools for {ServerName}", sseClientTransportOptions.Name);
            return cachedMcpClientTools;
        }

        try
        {
            var clientTransport = new SseClientTransport(sseClientTransportOptions);
            var mcpClient = await McpClientFactory.CreateAsync(clientTransport, loggerFactory: loggerFactory, cancellationToken: cancellationToken).ConfigureAwait(false);
            var mcpClientTools = await mcpClient.ListToolsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

            _cache[sseClientTransportOptions.Name ?? throw new NullReferenceException(nameof(sseClientTransportOptions))] = mcpClientTools;

            return mcpClientTools;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "MCP server '{ServerName}' failed to connect to {Endpoint}: {Message}", sseClientTransportOptions.Name, sseClientTransportOptions.Endpoint, ex.Message);
            throw new McpServerException($"The MCP server '{sseClientTransportOptions.Name}' failed to connect to {sseClientTransportOptions.Endpoint}. Please check https://docs.getcellm.com/get-started/install for troubleshooting or disable the '{sseClientTransportOptions.Name}' tool in the Cellm > Tools > MCP menu. Error message: {ex.Message}", ex);
        }
    }
}

