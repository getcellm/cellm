using System.Text.Json;
using ExcelDna.Integration;
using ExcelDna.Integration.CustomUI;
using Excel = Microsoft.Office.Interop.Excel;

namespace Cellm.AddIn.UserInterface.Ribbon;

public partial class RibbonMain
{
    private static Excel.Application Application => (Excel.Application)ExcelDnaUtil.Application;

    private enum PromptGroupControlIds
    {
        PromptGroupHorizontalContainer,

        PromptToCell,
        PromptToRow,
        PromptToRange,
        PromptToColumn,
    }

    public string PromptGroup()
    {
        return $"""
        <group id="{nameof(PromptGroup)}" label="Prompt">
            <box id="{nameof(PromptGroupControlIds.PromptGroupHorizontalContainer)}" boxStyle="horizontal">
                <button id="{nameof(PromptGroupControlIds.PromptToCell)}"
                        size="large"
                        label="Cell"
                        imageMso="TableSelectCell"
                        onAction="{nameof(OnPromptToCellClicked)}"
                        screentip="Output response in a single cell" />
                <button id="{nameof(PromptGroupControlIds.PromptToRow)}"
                        size="large"
                        label="Row"
                        imageMso="TableRowSelect"
                        onAction="{nameof(OnPromptToRowClicked)}"
                        getEnabled="{nameof(GetStructuredOutputEnabled)}"
                        screentip="Output response in a row"
                        supertip="Spill multiple response values (if any) across cells to the right." />
                <button id="{nameof(PromptGroupControlIds.PromptToColumn)}"
                        size="large"
                        label="Column"
                        imageMso="TableColumnSelect"
                        onAction="{nameof(OnPromptToColumnClicked)}"
                        getEnabled="{nameof(GetStructuredOutputEnabled)}"
                        screentip="Output response in a column"
                        supertip="Spill multiple response values (if any) across cells below" />
                <button id="{nameof(PromptGroupControlIds.PromptToRange)}"
                        size="large"
                        label="Range"
                        imageMso="TableSelect"
                        onAction="{nameof(OnPromptToRangeClicked)}"
                        getEnabled="{nameof(GetStructuredOutputEnabled)}"
                        screentip="The model chooses output shape"
                        supertip="The model chooses whether response should be a single cell or spill into rows and/or columns based on your data. You can also just tell it what you want." />
            </box>
        </group>
        """;
    }

    public bool GetStructuredOutputEnabled(IRibbonControl control)
    {
        var currentProviderConfiguration = CellmAddIn.GetProviderConfiguration(GetCurrentProvider());

        if (currentProviderConfiguration.SupportsStructuredOutputWithTools)
        {
            return true;
        }

        // Have to access configuration the long way because updates happen too fast for IOptionsMonitor<CellmAddInConfiguration> to pick them up
        var enableTools = JsonSerializer.Deserialize<Dictionary<string, bool>>(GetValue($"{nameof(CellmAddInConfiguration)}:{nameof(CellmAddInConfiguration.EnableTools)}"));
        var enableModelContextProtocolServers = JsonSerializer.Deserialize<Dictionary<string, bool>>(GetValue($"{nameof(CellmAddInConfiguration)}:{nameof(CellmAddInConfiguration.EnableModelContextProtocolServers)}"));

        if (enableTools is null || enableModelContextProtocolServers is null)
        {
            // Config is messed up, don't annoy the user any further
            return false;
        }

        // Enable structured output buttons iff all tools are disabled
        if (enableTools.Values.All(isToolEnabled => !isToolEnabled) &&
            enableModelContextProtocolServers.Values.All(isServerEnabled => !isServerEnabled))
        {
            return true;
        }

        return false;
    }

    private enum CellmFormula
    {
        Prompt,
        PromptModel,
    }

    // Intentionally decoupled from internal Cellm.Models.Prompts.StructuredOutputShape
    private enum CellmOutputShape
    {
        ToCell,
        ToRow,
        ToColumn,
        ToRange,
    }

    private record ParsedCellmFormula(
        CellmFormula Function,
        CellmOutputShape Shape,
        string Arguments);

    private static ParsedCellmFormula? TryParseFormula(string formula)
    {
        // Parse the function name (e.g., "PROMPT" or "PROMPTMODEL")
        var equalsIndex = formula.IndexOf('=');
        var dotIndex = formula.IndexOf('.');
        var parenIndex = formula.IndexOf('(');

        if (equalsIndex < 0 || parenIndex < 0)
        {
            return null;
        }

        // Function name ends at dot (if present before paren) or at paren
        int functionEndIndex;
        if (dotIndex >= 0 && dotIndex < parenIndex)
        {
            functionEndIndex = dotIndex;
        }
        else
        {
            functionEndIndex = parenIndex;
        }

        if (equalsIndex >= functionEndIndex)
        {
            return null;
        }

        var functionName = formula.Substring(equalsIndex + 1, functionEndIndex - equalsIndex - 1);

        if (!Enum.TryParse<CellmFormula>(functionName, ignoreCase: true, out var function))
        {
            return null;
        }

        // Parse the output shape (e.g., "TOROW", "TOCOLUMN", "TORANGE")
        var shape = CellmOutputShape.ToCell;

        if (dotIndex >= 0 && dotIndex < parenIndex)
        {
            var shapeName = formula.Substring(dotIndex + 1, parenIndex - dotIndex - 1);

            if (Enum.TryParse<CellmOutputShape>(shapeName, ignoreCase: true, out var parsedShape))
            {
                shape = parsedShape;
            }
        }

        // Extract arguments including parentheses
        var arguments = formula[parenIndex..];

        return new ParsedCellmFormula(function, shape, arguments);
    }

    private static string BuildFormula(CellmFormula function, CellmOutputShape shape, string arguments)
    {
        var shapeSuffix = shape switch
        {
            CellmOutputShape.ToCell => string.Empty,
            _ => $".{shape.ToString().ToUpper()}"
        };

        return $"={function.ToString().ToUpper()}{shapeSuffix}{arguments}";
    }

    public void OnPromptToCellClicked(IRibbonControl control)
    {
        UpdateCell(CellmOutputShape.ToCell);
    }

    public void OnPromptToRowClicked(IRibbonControl control)
    {
        UpdateCell(CellmOutputShape.ToRow);
    }

    public void OnPromptToColumnClicked(IRibbonControl control)
    {
        UpdateCell(CellmOutputShape.ToColumn);
    }

    public void OnPromptToRangeClicked(IRibbonControl control)
    {
        UpdateCell(CellmOutputShape.ToRange);
    }

    private void UpdateCell(CellmOutputShape targetShape)
    {
        if (Application.ActiveSheet is null)
        {
            return;
        }

        if (HandleMultiCellSelection(targetShape))
        {
            return;
        }

        if (Application.ActiveCell is not Excel.Range activeCell)
        {
            return;
        }

        var formula = (string)activeCell.Formula;
        var parsed = TryParseFormula(formula);

        switch (parsed)
        {
            case null:
                InsertNewFormula(activeCell, targetShape);
                break;

            case { Shape: var s } when s == targetShape:
                TriggerRecalculation(activeCell, formula);
                break;

            case var p:
                ChangeFormulaShape(activeCell, p, targetShape);
                break;
        }
    }

    private bool HandleMultiCellSelection(CellmOutputShape targetShape)
    {
        var selectedCells = Application.Selection;

        if (selectedCells.Cells.Count <= 1)
        {
            return false;
        }

        var rowStart = selectedCells.Row;
        var rowCount = selectedCells.Rows.Count;
        var columnStart = selectedCells.Column;
        var columnCount = selectedCells.Columns.Count;

        var rangeStart = $"{GetColumnName(columnStart)}{rowStart}";
        var rangeEnd = $"{GetColumnName(columnStart + columnCount - 1)}{rowStart + rowCount - 1}";
        var formula = BuildFormula(CellmFormula.Prompt, targetShape, $"({rangeStart}:{rangeEnd})");

        var targetCell = Application.ActiveSheet.Range[
            $"{GetColumnName(columnStart + columnCount - 1)}{rowStart + rowCount}"
        ];

        // Prevent immediate recalculation while function wizard is open
        targetCell.NumberFormat = "@";
        SetFormula(targetCell, formula);
        targetCell.NumberFormat = "General";

        // Select target cell before opening function wizard
        targetCell.Select();
        Application.Dialogs[Excel.XlBuiltInDialog.xlDialogFunctionWizard].Show();

        return true;
    }

    private void InsertNewFormula(Excel.Range cell, CellmOutputShape targetShape)
    {
        var formula = BuildFormula(CellmFormula.Prompt, targetShape, "()");

        ExcelAsyncUtil.QueueAsMacro(() =>
        {
            // Prevent immediate recalculation while function wizard is open
            cell.NumberFormat = "@";
            SetFormula(cell, formula);
            cell.NumberFormat = "General";
            Application.Dialogs[Excel.XlBuiltInDialog.xlDialogFunctionWizard].Show();
        });
    }

    private void TriggerRecalculation(Excel.Range cell, string formula)
    {
        // Re-setting the formula triggers Excel-DNA async function re-evaluation
        // (Range.Calculate() does not work for async functions)
        ExcelAsyncUtil.QueueAsMacro(() =>
        {
            SetFormula(cell, formula);
        });
    }

    private void ChangeFormulaShape(Excel.Range cell, ParsedCellmFormula current, CellmOutputShape targetShape)
    {
        var newFormula = BuildFormula(current.Function, targetShape, current.Arguments);

        ExcelAsyncUtil.QueueAsMacro(() =>
        {
            SetFormula(cell, newFormula);
        });
    }
    private static void SetFormula(Excel.Range cell, string formula)
    {
        try
        {
            // Formula2 only exists in Excel 2019+, use dynamic to avoid compile error
            ((dynamic)cell).Formula2 = formula;
        }
        catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException)
        {
            // Fallback to normal Formula property for older versions of Excel
            cell.Formula = formula;
        }
    }

    internal static string GetColumnName(int columnNumber)
    {
        var columnName = string.Empty;

        while (columnNumber > 0)
        {
            var remainder = (columnNumber - 1) % 26;
            columnName = (char)('A' + remainder) + columnName;
            columnNumber = (columnNumber - 1) / 26;
        }

        return columnName;
    }

    internal static string GetRowName(int rowNumber)
    {
        return rowNumber.ToString();
    }
}
