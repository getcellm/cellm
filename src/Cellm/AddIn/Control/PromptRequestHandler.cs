using System.Runtime.InteropServices;
using ExcelDna.Integration;
using Excel = Microsoft.Office.Interop.Excel;
using Microsoft.Extensions.Logging;

namespace Cellm.AddIn.Control;

internal sealed class PromptRequestHandler(ILogger<PromptRequestHandler> logger) : IDisposable
{
    private const int _excelErrorGettingData = -2146826245;
    private readonly Dictionary<string, PendingPrompt> _pending = new(StringComparer.OrdinalIgnoreCase);
    private Excel.Application? _application;
    private bool _started;

    public void Start()
    {
        if (_started)
        {
            return;
        }

        _application = (Excel.Application)ExcelDnaUtil.Application;
        ExcelAsyncUtil.CalculationEnded += OnCalculationEnded;
        ExcelAsyncUtil.CalculationCanceled += OnCalculationCanceled;
        _application.SheetChange += OnSheetChange;
        _application.WorkbookBeforeClose += OnWorkbookBeforeClose;
        _started = true;
    }

    public async Task<PromptResponse> HandleAsync(PromptRequest request, CancellationToken cancellationToken)
    {
        PendingPrompt? pending = null;

        try
        {
            await ExcelAsyncUtil.QueueAsMacroTask(() =>
            {
                var application = _application ?? throw new InvalidOperationException("Excel is not available.");
                var workbook = FindWorkbook(application, request.Workbook)
                    ?? throw new ArgumentException($"Workbook '{request.Workbook}' is not open.");
                var worksheet = FindWorksheet(workbook, request.Worksheet)
                    ?? throw new ArgumentException($"Worksheet '{request.Worksheet}' does not exist in '{workbook.Name}'.");
                Excel.Range cell = worksheet.Range[request.Cell];

                if (cell.CountLarge != 1)
                {
                    throw new ArgumentException("The target must be a single cell.");
                }

                if (!request.Overwrite && (cell.HasFormula || cell.Value2 is not null))
                {
                    throw new InvalidOperationException($"{workbook.Name}/{worksheet.Name}!{cell.Address} is not empty. Set overwrite to true to replace it.");
                }

                var ranges = request.Ranges.Select(address =>
                {
                    Excel.Range range = worksheet.Range[address];
                    return range.Address[RowAbsolute: false, ColumnAbsolute: false, ReferenceStyle: Excel.XlReferenceStyle.xlA1];
                }).ToArray();

                var formula = BuildFormula(request.Prompt, ranges, request.ProviderAndModel);
                var address = cell.Address[RowAbsolute: false, ColumnAbsolute: false, ReferenceStyle: Excel.XlReferenceStyle.xlA1];

                pending = new PendingPrompt(workbook.Name, worksheet.Name, address, formula);
                Add(pending);

                try
                {
                    SetFormula(cell, formula);
                    TryComplete(pending);
                }
                catch
                {
                    Remove(pending);
                    throw;
                }
            }).ConfigureAwait(false);

            return await pending!.Completion.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            if (pending is not null)
            {
                Remove(pending);
            }

            throw;
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or COMException)
        {
            if (pending is not null)
            {
                Remove(pending);
            }

            return Error(request, ex.Message);
        }
    }

    internal static string BuildFormula(string prompt, IReadOnlyList<string> ranges, string? providerAndModel)
    {
        var arguments = new List<string>();
        var function = "PROMPT";

        if (!string.IsNullOrWhiteSpace(providerAndModel))
        {
            function = "PROMPTMODEL";
            arguments.Add(Quote(providerAndModel));
        }

        arguments.Add(Quote(prompt));
        arguments.AddRange(ranges);

        return $"={function}({string.Join(", ", arguments)})";
    }

    private void OnCalculationEnded()
    {
        foreach (var pending in GetPending())
        {
            TryComplete(pending);
        }
    }

    private void TryComplete(PendingPrompt pending)
    {
        try
        {
            var application = _application;
            var workbook = application is null ? null : FindWorkbook(application, pending.Workbook);
            var worksheet = workbook is null ? null : FindWorksheet(workbook, pending.Worksheet);

            if (worksheet is null)
            {
                Complete(pending, Response(pending, "workbook_closed", error: "The workbook or worksheet was closed."));
                return;
            }

            Excel.Range cell = worksheet.Range[pending.Cell];
            var formula = GetFormula(cell);

            if (!string.Equals(formula, pending.Formula, StringComparison.OrdinalIgnoreCase))
            {
                Complete(pending, Response(pending, string.IsNullOrEmpty(formula) ? "cancelled" : "replaced"));
                return;
            }

            var value = cell.Value2;
            var displayedValue = Convert.ToString(cell.Text);

            if (value is null || IsPending(value))
            {
                return;
            }

            if (string.Equals(Convert.ToString(value), "Cancelled", StringComparison.OrdinalIgnoreCase))
            {
                Complete(pending, Response(pending, "cancelled", formula));
                return;
            }

            if (value is int)
            {
                Complete(pending, Response(pending, "error", formula, error: displayedValue ?? Convert.ToString(value)));
                return;
            }

            Complete(pending, Response(pending, "completed", formula, Normalize(value)));
        }
        catch (COMException ex)
        {
            logger.LogDebug(ex, "Unable to read {Workbook}/{Worksheet}!{Cell} after calculation", pending.Workbook, pending.Worksheet, pending.Cell);
        }
    }

    private void OnCalculationCanceled()
    {
        foreach (var pending in GetPending())
        {
            Complete(pending, Response(pending, "cancelled"));
        }
    }

    private void OnSheetChange(object sheet, Excel.Range target)
    {
        if (sheet is not Excel.Worksheet worksheet)
        {
            return;
        }

        foreach (var pending in GetPending()
            .Where(item => string.Equals(item.Workbook, ((Excel.Workbook)worksheet.Parent).Name, StringComparison.OrdinalIgnoreCase)
                && string.Equals(item.Worksheet, worksheet.Name, StringComparison.OrdinalIgnoreCase)))
        {
            Excel.Range cell = worksheet.Range[pending.Cell];
            var intersection = _application?.Intersect(cell, target);

            if (intersection is null)
            {
                continue;
            }

            var formula = GetFormula(cell);
            if (!string.Equals(formula, pending.Formula, StringComparison.OrdinalIgnoreCase))
            {
                Complete(pending, Response(pending, string.IsNullOrEmpty(formula) ? "cancelled" : "replaced"));
            }
        }
    }

    private void OnWorkbookBeforeClose(Excel.Workbook workbook, ref bool cancel)
    {
        foreach (var pending in GetPending()
            .Where(item => string.Equals(item.Workbook, workbook.Name, StringComparison.OrdinalIgnoreCase)))
        {
            Complete(pending, Response(pending, "workbook_closed", error: "The workbook was closed before the prompt completed."));
        }
    }

    public void Dispose()
    {
        if (!_started)
        {
            return;
        }

        ExcelAsyncUtil.CalculationEnded -= OnCalculationEnded;
        ExcelAsyncUtil.CalculationCanceled -= OnCalculationCanceled;

        if (_application is not null)
        {
            _application.SheetChange -= OnSheetChange;
            _application.WorkbookBeforeClose -= OnWorkbookBeforeClose;
        }

        foreach (var pending in GetPending())
        {
            Complete(pending, Response(pending, "add_in_closed", error: "Cellm was closed before the prompt completed."));
        }

        _started = false;
    }

    private void Add(PendingPrompt pending)
    {
        lock (_pending)
        {
            if (!_pending.TryAdd(pending.Key, pending))
            {
                throw new InvalidOperationException($"A prompt is already running in {pending.Workbook}/{pending.Worksheet}!{pending.Cell}.");
            }
        }
    }

    private void Remove(PendingPrompt pending)
    {
        lock (_pending)
        {
            _pending.Remove(pending.Key);
        }
    }

    private PendingPrompt[] GetPending()
    {
        lock (_pending)
        {
            return _pending.Values.ToArray();
        }
    }

    private void Complete(PendingPrompt pending, PromptResponse response)
    {
        Remove(pending);
        pending.Completion.TrySetResult(response);
    }

    private static PromptResponse Response(PendingPrompt pending, string status, string? formula = null, object? value = null, string? error = null)
    {
        return new PromptResponse(1, status, pending.Workbook, pending.Worksheet, pending.Cell, formula, value, error);
    }

    private static PromptResponse Error(PromptRequest request, string error)
    {
        return new PromptResponse(1, "error", request.Workbook, request.Worksheet, request.Cell, Error: error);
    }

    private static string Quote(string value) => $"\"{value.Replace("\"", "\"\"")}\"";

    private static bool IsPending(object value) => value is int error && error == _excelErrorGettingData;

    private static object? Normalize(object? value) => value switch
    {
        null or string or double or bool => value,
        float or decimal or int or long => Convert.ToDouble(value),
        _ => Convert.ToString(value)
    };

    private static Excel.Workbook? FindWorkbook(Excel.Application application, string name)
    {
        return application.Workbooks
            .Cast<Excel.Workbook>()
            .FirstOrDefault(workbook => string.Equals(workbook.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    private static Excel.Worksheet? FindWorksheet(Excel.Workbook workbook, string name)
    {
        return workbook.Worksheets
            .Cast<Excel.Worksheet>()
            .FirstOrDefault(worksheet => string.Equals(worksheet.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    private static void SetFormula(Excel.Range cell, string formula)
    {
        try
        {
            ((dynamic)cell).Formula2 = formula;
        }
        catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException)
        {
            cell.Formula = formula;
        }
    }

    private static string GetFormula(Excel.Range cell)
    {
        try
        {
            return Convert.ToString(((dynamic)cell).Formula2) ?? string.Empty;
        }
        catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException)
        {
            return Convert.ToString(cell.Formula) ?? string.Empty;
        }
    }

    private sealed class PendingPrompt(string workbook, string worksheet, string cell, string formula)
    {
        public string Workbook { get; } = workbook;
        public string Worksheet { get; } = worksheet;
        public string Cell { get; } = cell;
        public string Formula { get; } = formula;
        public string Key { get; } = $"{workbook}\n{worksheet}\n{cell}";
        public TaskCompletionSource<PromptResponse> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}
