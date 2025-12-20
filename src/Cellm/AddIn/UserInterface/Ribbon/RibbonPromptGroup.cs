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

    private CellmFormula? GetCellmFunction()
    {
        var formula = (string)Application.ActiveCell.Formula;

        var startIndex = formula.IndexOf('=');
        var endIndex = formula.IndexOf('.');

        if (endIndex < 0)
        {
            endIndex = formula.IndexOf('(');
        }

        if (startIndex < 0 || endIndex < 0 || startIndex >= endIndex)
        {
            // This is fine, it means the user asked us to insert formula in a cell that does not already contain a formula
            return null;
        }

        var cellmFormulaAsString = formula.Substring(startIndex + 1, endIndex - startIndex - 1);

        if (Enum.TryParse<CellmFormula>(cellmFormulaAsString, ignoreCase: true, out var cellmFormula))
        {
            // The cell already contains a Cellm formula
            return cellmFormula;
        }

        // The cell does not contain a Cellm formula
        return null;
    }

    private CellmOutputShape? GetCellmOutputShape()
    {
        if (GetCellmFunction() is null)
        {
            // The cell does not contain a Cellm formula
            return null;
        }

        var formula = (string)Application.ActiveCell.Formula;

        var startIndex = formula.IndexOf('.');
        var endIndex = formula.IndexOf('(');

        if (startIndex < 0 || endIndex < 0 || startIndex >= endIndex)
        {
            // This is fine, it means the formula uses the default output shape
            return CellmOutputShape.ToCell;
        }

        var cellmOutputShapeAsString = formula.Substring(startIndex + 1, endIndex - startIndex - 1);

        if (Enum.TryParse<CellmOutputShape>(cellmOutputShapeAsString, ignoreCase: true, out var cellmOutputShape))
        {
            // The cell already contains a Cellm formula
            return cellmOutputShape;
        }

        // We could not parse the output shape from the formula
        return null;
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

    private void UpdateCell(CellmOutputShape targetOutputShape)
    {
        if (Application.ActiveSheet == null)
        {
            // No sheet open
            return;
        }

        var currentFunction = GetCellmFunction();
        var currentOutputShape = GetCellmOutputShape();
        var targetOutputShapeAsString = targetOutputShape == CellmOutputShape.ToCell ? string.Empty : $".{targetOutputShape.ToString().ToUpper()}";

        var selectedCells = Application.Selection;

        if (selectedCells.Cells.Count > 1)
        {
            var rowStart = selectedCells.Row;
            var rowEnd = selectedCells.Rows.Count - 1;
            var columnStart = selectedCells.Column;
            var columnEnd = selectedCells.Columns.Count - 1;
            var rangeAsString = $"{GetColumnName(columnStart)}{GetRowName(rowStart)}:{GetColumnName(columnStart + columnEnd)}{GetRowName(rowStart + rowEnd)}";
            var formula = $"={nameof(CellmFunctions.Prompt).ToUpper()}({rangeAsString})";

            var targetCell = Application.ActiveSheet.Range[GetColumnName(columnStart + columnEnd) + GetRowName(rowStart + rowEnd + 1)];
            targetCell.NumberFormat = "@";  // Do not recalculate the formula immediately

            try
            {
                // Formula2 only exists in Excel 2019+, use dynamic to avoid compile error
                ((dynamic)targetCell).Formula2 = formula;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException)
            {
                // Fallback to normal Formula property for older versions of Excel
                targetCell.Formula = formula;
            }

            targetCell.NumberFormat = "General";  // Calculate the formula when function wizard is closed

            // Select target cell before opening function wizard
            targetCell.Select();

            Application.Dialogs[Microsoft.Office.Interop.Excel.XlBuiltInDialog.xlDialogFunctionWizard].Show();

            return;
        }

        if (Application.ActiveCell is null)
        {
            // No cell selected
            return;
        }

        if (currentFunction is null)
        {
            // The cell does not contain a Cellm formula, insert a new one
            ExcelAsyncUtil.QueueAsMacro(() =>
            {
                Application.ActiveCell.NumberFormat = "@";  // Do not recalculate the formula immediately
                SetFormula($"={nameof(CellmFunctions.Prompt).ToUpper()}{targetOutputShapeAsString}()");
                Application.ActiveCell.NumberFormat = "General";  // Calculate the formula when function wizard is closed
                Application.Dialogs[Microsoft.Office.Interop.Excel.XlBuiltInDialog.xlDialogFunctionWizard].Show();

            });

            return;
        }

        if (currentOutputShape == targetOutputShape)
        {
            // Re-set the formula to trigger recalculation
            var formula = (string)Application.ActiveCell.Formula;
            ExcelAsyncUtil.QueueAsMacro(() =>
            {
                SetFormula(formula);
            });
            return;
        }

        // Change the output shape and recalculate
        var currentFormula = (string)Application.ActiveCell.Formula;
        var currentFunctionAsString = currentFunction.ToString() ?? throw new NullReferenceException(nameof(currentFunction));
        var arguments = currentFormula[currentFormula.IndexOf('(')..];

        ExcelAsyncUtil.QueueAsMacro(() =>
        {
            SetFormula($"={currentFunctionAsString.ToUpper()}{targetOutputShapeAsString}{arguments}");
        });
    }

    void SetFormula(string formula)
    {
        var cell = Application.ActiveCell;
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
