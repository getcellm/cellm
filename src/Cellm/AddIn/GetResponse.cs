using Cellm.Models;
using Cellm.Models.Prompts;
using Cellm.Models.Providers;
using ExcelDna.Integration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cellm.AddIn;

internal class GetResponse(Prompt prompt, Provider provider) : IExcelObservable
{
    private readonly List<IExcelObserver> _observers = [];
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly ILoggerFactory _loggerFactory = CellmAddIn.Services.GetRequiredService<ILoggerFactory>();
    private Task? _task;

    public IDisposable Subscribe(IExcelObserver observer)
    {
        observer.OnNext(ExcelError.ExcelErrorGettingData);

        var logger = _loggerFactory.CreateLogger<GetResponse>();

        logger.LogDebug("Adding observer");
        _observers.Add(observer);

        if (_task is null)
        {
            logger.LogDebug("Starting task");
            _task = Task.Run(async () => await GetResponseAsync(prompt, provider, _cancellationTokenSource.Token), _cancellationTokenSource.Token);
        }
        
        return new ActionDisposable(() =>
        {
            logger.LogDebug("Removing observer");
            _observers.Remove(observer);

            // If this was the last observer, cancel the call
            if (_observers.Count == 0)
            {
                logger.LogDebug("Removed last observer, cancelling");
                _cancellationTokenSource.Cancel();
            }
        });
    }

    private async Task GetResponseAsync(Prompt prompt, Provider provider, CancellationToken cancellationToken)
    {
        try
        {
            var client = CellmAddIn.Services.GetRequiredService<Client>();
            var response = await client.GetResponseAsync(prompt, provider, cancellationToken);
            var assistantMessage = response.Messages.Last().Text ?? throw new NullReferenceException("No text response");

            foreach (var observer in _observers)
            {
                observer.OnNext(assistantMessage);
                observer.OnCompleted();
            }
        }
        catch (Exception ex)
        {
            foreach (var observer in _observers)
            {
                observer.OnError(ex);
            }
        }
    }

    class ActionDisposable(Action disposeAction) : IDisposable
    {
        readonly Action _disposeAction = disposeAction;

        public void Dispose()
        {
            _disposeAction();
        }
    }
}
