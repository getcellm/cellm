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
        OutputTable,
        OutputColumn,
    }

    public string OutputGroup()
    {
        return $"""
        <group id="{nameof(OutputGroup)}" label="Output">
            <box id="{nameof(OutputGroupControlIds.OutputGroupHorizontalContainer)}" boxStyle="horizontal">
                <button id="{nameof(OutputGroupControlIds.OutputCell)}" 
                        size="large"
                        label="Single"
                        imageMso="TableSelectCell"
                        onAction="{nameof(OnOutputCellClicked)}"
                        screentip="Output response in a single cell (default)" />
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
                <button id="{nameof(OutputGroupControlIds.OutputTable)}"
                        size="large"
                        label="Table"
                        imageMso="TableSelect"
                        onAction="{nameof(OnOutputTableClicked)}" 
                        screentip="Output response in rows and columns"
                        supertip="Let model decide how to output multiple values (as single cell, row, column, or table, just tell it what you want)" />
            </box>
        </group>
        """;
    }

    public void OnOutputCellClicked(IRibbonControl control)
    {
        InsertFormula("PROMPTSINGLE");
    }

    public void OnOutputRowClicked(IRibbonControl control)
    {
        InsertFormula("PROMPTROW");
    }

    public void OnOutputColumnClicked(IRibbonControl control)
    {
        InsertFormula("PROMPTCOLUMN");
    }

    public void OnOutputTableClicked(IRibbonControl control)
    {
        InsertFormula("PROMPT");
    }

    private void InsertFormula(string functionName)
    {
        ExcelAsyncUtil.QueueAsMacro(() =>
        {
            ExcelDnaUtil.Application.ActiveCell.NumberFormat = "@";
            ExcelDnaUtil.Application.ActiveCell.Value = $"={functionName}()";
            ExcelDnaUtil.Application.ActiveCell.NumberFormat = "General";
            ExcelDnaUtil.Application.Dialogs[Microsoft.Office.Interop.Excel.XlBuiltInDialog.xlDialogFunctionWizard].Show();

        });
        //var selection = XlCall.Excel(XlCall.xlfSelection) as ExcelReference;
        //if (selection == null)
        //{
        //    return;
        //}

        //var activeCell = XlCall.Excel(XlCall.xlfActiveCell) as ExcelReference;
        //if (activeCell == null)
        //{
        //    return;
        //}

        //var app = ExcelDnaUtil.Application as Microsoft.Office.Interop.Excel.Application;
        //var a = app.Selection;

        //string formula;
        //if (selection.RowFirst == selection.RowLast && selection.ColumnFirst == selection.ColumnLast)
        //{
        //    ExcelAsyncUtil.QueueAsMacro(() =>
        //    {
        //        app.ActiveCell.Formula = $"={functionName}(\"\")";
        //    });
        //}
            //else if (selection.RowFirst == selection.RowLast)  // Row selection, put formula to the right of the selection
            //{
            //    var selectionAddress = XlCall.Excel(XlCall.xlfReftext, selection, true) as string;
            //    formula = $"={functionName}({selectionAddress}, \"\")";
            //    XlCall.Excel(XlCall.xlcFormula | XlCall.xlIntl, formula, new ExcelReference(selection.RowFirst, selection.ColumnLast + 1));
            //}
            //else if (selection.ColumnFirst == selection.ColumnLast)  // Column
            //{
            //    var selectionAddress = XlCall.Excel(XlCall.xlfReftext, selection, true) as string;
            //    formula = $"={functionName}({selectionAddress}, \"\")";
            //    XlCall.Excel(XlCall.xlcFormula | XlCall.xlIntl, formula, new ExcelReference(selection.RowLast + 1, selection.ColumnFirst));
            //}
            //else // Block
            //{
            //    var selectionAddress = XlCall.Excel(XlCall.xlfReftext, selection, true) as string;

            //    for (var i = selection.ColumnFirst; i <= selection.ColumnLast; i++)
            //    {
            //        formula = $"={functionName}({selectionAddress}, \"\")";
            //        XlCall.Excel(XlCall.xlcFormula | XlCall.xlIntl, formula, new ExcelReference(selection.RowLast + 1, i));
            //    }

            //}
    }

    [ExcelCommand()]
    public static void EnterEditModeHelper1()
    {
        SendKeys.Send("{F2}");
        //ExcelDnaUtil.Application.OnTime(System.DateTime.Now, "EnterEditModeHelper2");
    }

    [ExcelCommand()]
    public static void EnterEditModeHelper2()
    {
        SendKeys.SendWait("{ESCAPE}");
        SendKeys.SendWait("{F2}");
    }
}
