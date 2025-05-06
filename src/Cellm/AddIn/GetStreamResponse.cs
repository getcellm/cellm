using System.Text;
using Cellm.Models;
using Cellm.Models.Prompts;
using Cellm.Models.Providers;
using ExcelDna.Integration;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cellm.AddIn;

internal class GetStreamResponse(Prompt prompt, Provider provider) : IExcelObservable
{
    private readonly List<IExcelObserver> _observers = [];
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly StringBuilder _responseBuilder = new();
    private Task? _task = null;
    private readonly ILoggerFactory _loggerFactory = CellmAddIn.Services.GetRequiredService<ILoggerFactory>();

    public IDisposable Subscribe(IExcelObserver observer)
    {
        if (_responseBuilder.Length > 0)
        {
            observer.OnNext(_responseBuilder.ToString());
        }
        else
        {
            observer.OnNext(ExcelError.ExcelErrorGettingData);
        }

        var logger = _loggerFactory.CreateLogger<GetResponse>();

        logger.LogDebug("Adding observer");
        _observers.Add(observer);

        // Consume stream on first call
        _task ??= Task.Run(async () =>
            {
                try
                {
                    var client = CellmAddIn.Services.GetRequiredService<Client>();
                    await foreach (var update in client.GetStreamResponseAsync(prompt, provider, _cancellationTokenSource.Token).WithCancellation(_cancellationTokenSource.Token))
                    {
                        if (update is ChatResponseUpdate { Text: var text })
                        {
                            _responseBuilder.Append(text);

                            foreach (var observer in _observers)
                            {
                                observer.OnNext(_responseBuilder.ToString());
                            }
                        }
                        else if (update is ChatResponseUpdate { FinishReason: var finishReason })
                        {
                            if (finishReason == ChatFinishReason.Stop)
                            {
                                foreach (var observer in _observers)
                                {
                                    observer.OnCompleted();
                                }
                            }
                        }
                        else
                        {
                            throw new InvalidOperationException($"Unknown update type: {update.GetType()}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError("{message}", ex.Message);

                    foreach (var observer in _observers)
                    {
                        observer.OnError(ex);
                    }
                }
            }, _cancellationTokenSource.Token);


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

    class ActionDisposable(Action disposeAction) : IDisposable
    {
        readonly Action _disposeAction = disposeAction;

        public void Dispose()
        {
            _disposeAction();
        }
    }
}
