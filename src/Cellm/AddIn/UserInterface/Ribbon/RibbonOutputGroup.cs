using ExcelDna.Integration;
using ExcelDna.Integration.CustomUI;

namespace Cellm.AddIn.UserInterface.Ribbon;

public partial class RibbonMain
{
    private enum OutputGroupControlIds
    {
        OutputGroupHorizontalContainer,

        OutputCell,
        OutputRow,
        OutputDynamic,
        OutputColumn,
    }

    public string OutputGroup()
    {
        return $"""
        <group id="{nameof(OutputGroup)}" label="Output Shape">
            <box id="{nameof(OutputGroupControlIds.OutputGroupHorizontalContainer)}" boxStyle="horizontal">
                <button id="{nameof(OutputGroupControlIds.OutputCell)}" 
                        size="large"
                        label="Cell"
                        imageMso="TableSelectCell"
                        onAction="{nameof(OnOutputCellClicked)}"
                        screentip="Output response in a single cell" />
                <button id="{nameof(OutputGroupControlIds.OutputRow)}"
                        size="large"
                        label="Row"
                        imageMso="TableRowSelect"
                        onAction="{nameof(OnOutputRowClicked)}" 
                        screentip="Output response in a row"
                        supertip="Spill multiple response values (if any) across cells to the right." />
                <button id="{nameof(OutputGroupControlIds.OutputColumn)}"
                        size="large"
                        label="Column"
                        imageMso="TableColumnSelect" 
                        onAction="{nameof(OnOutputColumnClicked)}" 
                        screentip="Output response in a column"
                        supertip="Spill multiple response values (if any) across cells below" />
                <button id="{nameof(OutputGroupControlIds.OutputDynamic)}"
                        size="large"
                        label="Dynamic"
                        imageMso="TableSelect"
                        onAction="{nameof(OnOutputDynamicClicked)}" 
                        screentip="Output response in cell, row, column, or table (default)"
                        supertip="Let model decide how to spill multiple response values (if any) or just tell it what you want" />
            </box>
        </group>
        """;
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
        Dynamic,
    }

    private CellmFormula? GetCellmFunction()
    {
        var formula = (string)ExcelDnaUtil.Application.ActiveCell.Formula;

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

        var formula = (string)ExcelDnaUtil.Application.ActiveCell.Formula;

        var startIndex = formula.IndexOf('.');
        var endIndex = formula.IndexOf('(');

        if (startIndex < 0 || endIndex < 0 || startIndex >= endIndex)
        {
            // This is fine, it means the formula uses the default output shape
            return CellmOutputShape.Dynamic;
        }

        var cellmOutputShapeAsString = formula.Substring(startIndex + 1, endIndex - startIndex + 1);

        if (Enum.TryParse<CellmOutputShape>(cellmOutputShapeAsString, ignoreCase: true, out var cellmOutputShape))
        {
            // The cell already contains a Cellm formula
            return cellmOutputShape;
        }

        // We could not parse the output shape from the formula
        return null;
    }

    public void OnOutputCellClicked(IRibbonControl control)
    {
        UpdateCell(CellmOutputShape.ToCell);
    }

    public void OnOutputRowClicked(IRibbonControl control)
    {
        UpdateCell(CellmOutputShape.ToRow);
    }

    public void OnOutputColumnClicked(IRibbonControl control)
    {
        UpdateCell(CellmOutputShape.ToColumn);
    }

    public void OnOutputDynamicClicked(IRibbonControl control)
    {
        UpdateCell(CellmOutputShape.Dynamic);
    }

    private void UpdateCell(CellmOutputShape targetOutputShape)
    {
        if (ExcelDnaUtil.Application.ActiveSheet == null)
        {
            // No sheet open
            return;
        }

        var currentFunction = GetCellmFunction();
        var currentOutputShape = GetCellmOutputShape();
        var targetOutputShapeAsString = targetOutputShape == CellmOutputShape.Dynamic ? string.Empty : $".{targetOutputShape.ToString().ToUpper()}";

        var selectedCells = ExcelDnaUtil.Application.Selection;

        if (selectedCells.Cells.Count > 1)
        {
            var rowStart = selectedCells.Row;
            var rowEnd = selectedCells.Rows.Count - 1;
            var columnStart = selectedCells.Column;
            var columnEnd = selectedCells.Columns.Count - 1;
            var rangeAsString = $"{GetColumnName(columnStart)}{GetRowName(rowStart)}:{GetColumnName(columnStart + columnEnd)}{GetRowName(rowStart + rowEnd)}";
            var formula = $"={nameof(CellmFunctions.Prompt).ToUpper()}({rangeAsString})";

            var targetCell = ExcelDnaUtil.Application.ActiveSheet.Range[GetColumnName(columnStart + columnEnd) + GetRowName(rowStart + rowEnd + 1)];
            targetCell.NumberFormat = "@";  // Do not recalculate the formula immediately

            try
            {
                // Use array-aware Formula2 if it exists. This prevents array-aware Excel versions from prepending "@" to formula
                targetCell.Formula2 = formula;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException)
            {
                // Fallback to normal Formula property for older versions of Excel
                targetCell.Formula = formula;
            }

            targetCell.NumberFormat = "General";  // Calculate the formula when function wizard is closed

            // Select target cell before opening function wizard
            targetCell.Select();

            ExcelDnaUtil.Application.Dialogs[Microsoft.Office.Interop.Excel.XlBuiltInDialog.xlDialogFunctionWizard].Show();

            return;
        }

        if (ExcelDnaUtil.Application.ActiveCell is null)
        {
            // No cell selected
            return;
        }

        if (currentFunction is null)
        {
            // The cell does not contain a Cellm formula, insert a new one
            ExcelAsyncUtil.QueueAsMacro(() =>
            {
                ExcelDnaUtil.Application.ActiveCell.NumberFormat = "@";  // Do not recalculate the formula immediately
                SetFormula($"={nameof(CellmFunctions.Prompt).ToUpper()}{targetOutputShapeAsString}()");
                ExcelDnaUtil.Application.ActiveCell.NumberFormat = "General";  // Calculate the formula when function wizard is closed
                ExcelDnaUtil.Application.Dialogs[Microsoft.Office.Interop.Excel.XlBuiltInDialog.xlDialogFunctionWizard].Show();

            });

            return;
        }

        if (currentOutputShape == targetOutputShape)
        {
            // Do nothing;
            return;
        }

        // Change the output shape and recalculate
        var currentFormula = (string)ExcelDnaUtil.Application.ActiveCell.Formula;
        var currentFunctionAsString = currentFunction.ToString() ?? throw new NullReferenceException(nameof(currentFunction));
        var arguments = currentFormula[currentFormula.IndexOf('(')..];

        ExcelAsyncUtil.QueueAsMacro(() =>
        {
            SetFormula($"={currentFunctionAsString.ToUpper()}{targetOutputShapeAsString}{arguments}");
            var dynamicApp = (dynamic)ExcelDnaUtil.Application;
        });
    }

    void SetFormula(string formula)
    {
        var dynamicApp = (dynamic)ExcelDnaUtil.Application;

        try
        {
            // Use array-aware Formula2 if it exists. This prevents array-aware Excel versions from prepending "@" to formula
            dynamicApp.ActiveCell.Formula2 = formula;
        }
        catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException)
        {
            // Fallback to normal Formula property for older versions of Excel
            ExcelDnaUtil.Application.ActiveCell.Formula = formula;
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
