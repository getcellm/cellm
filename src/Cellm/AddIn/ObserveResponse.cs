using System.Diagnostics;
using System.Text;
using Cellm.AddIn.Exceptions;
using Cellm.Models;
using Cellm.Models.Prompts;
using ExcelDna.Integration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cellm.AddIn;

internal class ObserveResponse(Arguments arguments, Stopwatch stopwatch) : IExcelObservable
{
    private Task? _task;
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    private readonly ILogger _logger = CellmAddIn.Services
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger<ObserveResponse>();

    public IDisposable Subscribe(IExcelObserver observer)
    {
        var elapsedSubscribeStart = stopwatch.ElapsedMilliseconds;

        _task = Task.Run(() => GetResponse(observer));

        _logger.LogInformation("Sending request {id} ... Queued (elapsed time: {} ms, queue time: {})", _task?.Id, stopwatch.ElapsedMilliseconds, stopwatch.ElapsedMilliseconds - elapsedSubscribeStart);

        return new ActionDisposable(() =>
        {
            _cancellationTokenSource.Cancel();
        });
    }

    public async void GetResponse(IExcelObserver observer)
    {
        try
        {
            var elapsedTaskStart = stopwatch.ElapsedMilliseconds;

            // Check for cancellation before doing any work
            _cancellationTokenSource.Token.ThrowIfCancellationRequested();

            observer.OnNext(ExcelError.ExcelErrorGettingData);

            var cells = arguments.Cells switch
            {
                Cells argumentCells => ArgumentParser.ParseCells(argumentCells),
                null => "Not available",
                _ => throw new ArgumentException(nameof(arguments.Cells))
            };

            var instructions = arguments.Instructions switch
            {
                string argumentInstruction => argumentInstruction,
                Cells argumentInstruction => ArgumentParser.ParseCells(argumentInstruction),
                _ => throw new ArgumentException(nameof(arguments.Instructions))
            };

            // Check for cancellation before constructing prompt
            _cancellationTokenSource.Token.ThrowIfCancellationRequested();

            var userMessage = new StringBuilder()
                .AppendLine(ArgumentParser.AddInstructionTags(instructions))
                .AppendLine(ArgumentParser.AddCellTags(cells))
                .ToString();

            var cellmAddInConfiguration = CellmAddIn.Services.GetRequiredService<IOptionsMonitor<CellmAddInConfiguration>>();

            var prompt = new PromptBuilder()
                .SetModel(arguments.Model)
                .SetTemperature(arguments.Temperature)
                .SetMaxOutputTokens(cellmAddInConfiguration.CurrentValue.MaxOutputTokens)
                .AddSystemMessage(SystemMessages.SystemMessage)
                .AddUserMessage(userMessage)
                .Build();

            // Check for cancellation before sending request
            _cancellationTokenSource.Token.ThrowIfCancellationRequested();

            var client = CellmAddIn.Services.GetRequiredService<Client>();
            var response = await client.GetResponseAsync(prompt, arguments.Provider, _cancellationTokenSource.Token).ConfigureAwait(false);
            var assistantMessage = response.Messages.LastOrDefault()?.Text ?? throw new InvalidOperationException("No text response");

            // Check for cancellation before notifying observer
            _cancellationTokenSource.Token.ThrowIfCancellationRequested();

            observer.OnNext(assistantMessage);
            observer.OnCompleted();

            _logger.LogInformation("Sending request {id} ... Done (elapsed time: {} ms, task time: {})", _task?.Id, stopwatch.ElapsedMilliseconds, stopwatch.ElapsedMilliseconds - elapsedTaskStart);
        }
        catch (ExcelErrorException ex)
        {
             observer.OnNext(ex.GetExcelError());
             observer.OnCompleted();

        }
        catch (OperationCanceledException)
        {
            // Do not notify observer, it is being disposed or something went wrong
            _logger.LogInformation("Sending request {id} ... Cancelled (elapsed time: {} ms)", _task?.Id, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            observer.OnError(ex);
            _logger.LogError(ex, "Sending request {id} ... Failed: {message} (elapsed time: {} ms)", _task?.Id, ex.Message, stopwatch.ElapsedMilliseconds);
        }
    }

    class ActionDisposable(Action disposeAction) : IDisposable
    {
        public void Dispose()
        {
            disposeAction();
        }
    }
}
