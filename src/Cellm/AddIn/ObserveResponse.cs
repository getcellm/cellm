using System.Diagnostics;
using System.Text;
using Cellm.Models;
using Cellm.Models.Prompts;
using ExcelDna.Integration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cellm.AddIn;

internal class ObserveResponse(Arguments arguments, Stopwatch stopwatch) : IExcelObservable
{
    private IExcelObserver? _observer;
    private Task? _task;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly Lock _lock = new();

    private readonly ILogger _logger = CellmAddIn.Services
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger<ObserveResponse>();

    public IDisposable Subscribe(IExcelObserver observer)
    {
        lock (_lock)
        {
            if (_observer is not null)
            {
                throw new InvalidOperationException("Internal error: GetResponse instance has already been subscribed to. Each call requires a new instance.");
            }

            _observer = observer ?? throw new ArgumentNullException(nameof(observer));
        }

        _observer.OnNext(ExcelError.ExcelErrorGettingData);

        _task = Task.Factory.StartNew(async () =>
        {
            try
            {
                var cells = arguments.Cells switch
                {
                    Cells argumentCells => ArgumentParser.ParseCells(argumentCells),
                    null => "Not available",
                    _ => throw new ArgumentException(nameof(arguments.Cells))
                };

                var instructions = arguments.Instructions switch
                {
                    string instruction => instruction,
                    Cells values => ArgumentParser.ParseCells(values),
                    _ => throw new ArgumentException(nameof(arguments.Instructions))
                };

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

                // Notify observer on the main thread
                ExcelAsyncUtil.QueueAsMacro(() =>
                {
                    _observer?.OnNext(assistantMessage);
                    _observer?.OnCompleted();
                });

                _logger.LogInformation("Sending request {id} ... Done (elapsed time: {} ms)", _task?.Id, stopwatch.ElapsedMilliseconds);
            }
            catch (OperationCanceledException ex)
            {
                // Notify observer on the main thread
                ExcelAsyncUtil.QueueAsMacro(() =>
                {
                    _observer.OnError(ex);
                });

                _logger.LogInformation(ex, "Sending request {id} ... Cancelled (elapsed time: {} ms)", _task?.Id, stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                // Notify observer on the main thread
                ExcelAsyncUtil.QueueAsMacro(() =>
                {
                    observer.OnError(ex);
                });

                _logger.LogError(ex, "Sending request {id} ... Failed: {message} (elapsed time: {} ms)", _task?.Id, ex.Message, stopwatch.ElapsedMilliseconds);
            }
        // Provide a hint to the scheduler that a new thread might be required, to avoid thread pool starvation
        }, _cancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);

        _logger.LogInformation("Sending request {id} ... Queued (elapsed time: {} ms)", _task?.Id, stopwatch.ElapsedMilliseconds);

        return new ActionDisposable(() =>
        {
            _logger.LogInformation("Sending request {id} ... Disposing ({status}, elapsed time: {} ms))", _task.Id, _task.IsCompleted ? "done" : "cancelled", stopwatch.ElapsedMilliseconds);
            _cancellationTokenSource.Cancel();
        });
    }

    class ActionDisposable(Action disposeAction) : IDisposable
    {
        public void Dispose()
        {
            disposeAction();
        }
    }
}
