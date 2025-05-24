using System.Text;
using Cellm.Models;
using Cellm.Models.Prompts;
using ExcelDna.Integration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cellm.AddIn;

internal class ObserveResponse(Arguments arguments) : IExcelObservable
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

            _logger.LogDebug("Getting response ...");
            _observer = observer ?? throw new ArgumentNullException(nameof(observer));
        }

        _observer.OnNext(ExcelError.ExcelErrorGettingData);

        _task = Task.Run(async () =>
        {
            try
            {
                var userMessage = new StringBuilder()
                    .AppendLine(arguments.Instructions)
                    .AppendLine(arguments.Context)
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
                var response = await client.GetResponseAsync(prompt, arguments.Provider, _cancellationTokenSource.Token);
                var assistantMessage = response.Messages.LastOrDefault()?.Text ?? throw new InvalidOperationException("No text response");

                // Check for cancellation before notifying observer
                _cancellationTokenSource.Token.ThrowIfCancellationRequested();

                // Notify observer on the main thread
                ExcelAsyncUtil.QueueAsMacro(() =>
                {
                    _observer?.OnNext(assistantMessage);
                    _observer?.OnCompleted();
                });

                _logger.LogDebug("Getting response {id} ... Done", _task?.Id);
            }
            catch (OperationCanceledException ex)
            {
                // Notify observer on the main thread
                ExcelAsyncUtil.QueueAsMacro(() =>
                {
                    _observer.OnError(ex);
                });

                _logger.LogDebug(ex, "Getting response {id} ... Cancelled", _task?.Id);
            }
            catch (Exception ex)
            {
                // Notify observer on the main thread
                ExcelAsyncUtil.QueueAsMacro(() =>
                {
                    observer.OnError(ex);
                });

                _logger.LogError(ex, "Getting response {id} ... Failed: {message}", _task?.Id, ex.Message);
            }
        }, _cancellationTokenSource.Token);

        return new ActionDisposable(() =>
        {
            _logger.LogDebug("Getting response {id} ... Disposing ({status})", _task.Id, _task.IsCompleted ? "done" : "cancelled");
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
