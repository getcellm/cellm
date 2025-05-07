using Cellm.Models;
using Cellm.Models.Prompts;
using Cellm.Models.Providers;
using ExcelDna.Integration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cellm.AddIn;

internal class ObserveResponse(Prompt prompt, Provider provider) : IExcelObservable
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
                var client = CellmAddIn.Services.GetRequiredService<Client>();
                var response = await client.GetResponseAsync(prompt, provider, _cancellationTokenSource.Token);
                var assistantMessage = response.Messages.LastOrDefault()?.Text ?? throw new InvalidOperationException("No text response");

                // Check for cancellation before notifying observer
                _cancellationTokenSource.Token.ThrowIfCancellationRequested();

                _observer.OnNext(assistantMessage);
                _observer.OnCompleted();

                _logger.LogDebug("Getting response ... Done");
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogDebug(ex, "Getting response ... Cancelled");
                _observer.OnError(ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Getting response ... Failed: {message}", ex.Message);
                observer.OnError(ex);
            }
        }, _cancellationTokenSource.Token);

        return new ActionDisposable(() =>
        {          
            _logger.LogDebug("Getting response ... Disposing");
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
