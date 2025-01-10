using System.Diagnostics;
using Cellm.Models.Prompts;
using Cellm.Models.Providers;
using Cellm.Models;
using Cellm.Services;
using ExcelDna.Integration;
using Microsoft.Extensions.AI;

namespace Cellm.AddIn;

public class CompleteStreaming : IExcelObservable
{
    private string _response = string.Empty;
    private readonly IAsyncEnumerable<StreamingChatCompletionUpdate> _stream;
    private readonly List<IExcelObserver> _observers = [];
    
    private Task? _task = null;
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    public CompleteStreaming(Prompt prompt, Provider provider, Uri? baseAddress)
    {
        var client = ServiceLocator.Get<Client>();
        _stream = client.CreateStream(prompt, provider, null, _cancellationTokenSource.Token);
    }

    public IDisposable Subscribe(IExcelObserver observer)
    {
        if (_response.Length > 0)
        {
            observer.OnNext(_response);
        }
        else
        {
            observer.OnNext(ExcelError.ExcelErrorGettingData);
        }

        _observers.Add(observer);

        // Consume stream on first call
        _task ??= Task.Run(CompleteStreamingAsync);

        return new ActionDisposable(() =>
        {
            _observers.Remove(observer);

            // If this was the last observer, cancel the stream
            if (_observers.Count == 0)
            {
                _cancellationTokenSource.Cancel();
            }
        });
    }

    private async Task CompleteStreamingAsync()
    {
        try
        {
            await foreach (var update in _stream)
            {
                _response += update.Text;

                // Ensure we update observers on the Excel thread
                ExcelAsyncUtil.QueueAsMacro(() =>
                {
                    foreach (var observer in _observers)
                    {
                        observer.OnNext(_response);
                    }
                });
            }

            ExcelAsyncUtil.QueueAsMacro(() =>
            {
                foreach (var observer in _observers)
                {
                    observer.OnCompleted();
                }
            });
        }
        catch (OperationCanceledException)
        {
            // Handle cancellation gracefully
        }
        catch (Exception ex)
        {
            // Log the error and notify observers
            Debug.WriteLine($"Error processing stream: {ex}");
            foreach (var observer in _observers)
            {
                observer.OnError(ex);
            }
        }
    }

    class ActionDisposable : IDisposable
    {
        readonly Action _disposeAction;

        public ActionDisposable(Action disposeAction)
        {
            _disposeAction = disposeAction;
        }
        public void Dispose()
        {
            _disposeAction();
            Debug.WriteLine("Disposed");
        }
    }
}
