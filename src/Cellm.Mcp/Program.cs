using Cellm.Mcp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});

builder.Services
    .AddSingleton<CellmClient>()
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<CellmTools>();

await builder.Build().RunAsync();
