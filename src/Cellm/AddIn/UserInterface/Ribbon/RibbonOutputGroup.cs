using Cellm.Models.Prompts;
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
                <toggleButton id="{nameof(OutputGroupControlIds.OutputCell)}" 
                        size="large"
                        label="Cell"
                        imageMso="TableSelectCell"
                        getPressed="{nameof(GetOutputCellPressed)}"
                        onAction="{nameof(OnOutputCellClicked)}"
                        screentip="Output response in a single cell (default)" />
                <toggleButton id="{nameof(OutputGroupControlIds.OutputRow)}"
                        size="large"
                        label="Row"
                        imageMso="TableRowSelect"
                        getPressed="{nameof(GetOutputRowPressed)}"
                        onAction="{nameof(OnOutputRowClicked)}" 
                        screentip="Output response in a row"
                        supertip="Spill multiple response values (if any) across cells to the right." />
                <toggleButton id="{nameof(OutputGroupControlIds.OutputColumn)}"
                        size="large"
                        label="Column"
                        imageMso="TableColumnSelect" 
                        getPressed="{nameof(GetOutputColumnPressed)}"
                        onAction="{nameof(OnOutputColumnClicked)}" 
                        screentip="Output response in a column"
                        supertip="Spill multiple response values (if any) across cells below" />
                <toggleButton id="{nameof(OutputGroupControlIds.OutputTable)}"
                        size="large"
                        label="Table"
                        imageMso="TableSelect"
                        getPressed="{nameof(GetOutputTablePressed)}"
                        onAction="{nameof(OnOutputTableClicked)}" 
                        screentip="Output response in rows and columns"
                        supertip="Let model decide how to output multiple values (as single cell, row, column, or table, just tell it what you want)" />
            </box>
        </group>
        """;
    }

    public void OnOutputCellClicked(IRibbonControl control, bool isPressed)
    {
        // Default, cannot toggle off via this button
        SetValue($"{nameof(CellmAddInConfiguration)}:{nameof(StructuredOutputShape)}", StructuredOutputShape.None.ToString());
        InvalidateOutputToggleButtons();
    }

    public void OnOutputRowClicked(IRibbonControl control, bool isPressed)
    {
        if (isPressed)
        {
            SetValue($"{nameof(CellmAddInConfiguration)}:{nameof(StructuredOutputShape)}", StructuredOutputShape.Row.ToString());
            InvalidateOutputToggleButtons();
        }
        else
        {
            SetValue($"{nameof(CellmAddInConfiguration)}:{nameof(StructuredOutputShape)}", StructuredOutputShape.None.ToString());
            InvalidateOutputToggleButtons();
        }
    }

    public void OnOutputColumnClicked(IRibbonControl control, bool isPressed)
    {
        if (isPressed)
        {
            SetValue($"{nameof(CellmAddInConfiguration)}:{nameof(StructuredOutputShape)}", StructuredOutputShape.Column.ToString());
            InvalidateOutputToggleButtons();
        }
        else
        {
            SetValue($"{nameof(CellmAddInConfiguration)}:{nameof(StructuredOutputShape)}", StructuredOutputShape.None.ToString());
            InvalidateOutputToggleButtons();
        }
    }

    public void OnOutputTableClicked(IRibbonControl control, bool isPressed)
    {
        if (isPressed)
        {
            SetValue($"{nameof(CellmAddInConfiguration)}:{nameof(StructuredOutputShape)}", StructuredOutputShape.Table.ToString());
            InvalidateOutputToggleButtons();
        }
        else
        {
            SetValue($"{nameof(CellmAddInConfiguration)}:{nameof(StructuredOutputShape)}", StructuredOutputShape.None.ToString());
            InvalidateOutputToggleButtons();
        }
    }

    public bool GetOutputCellPressed(IRibbonControl control)
    {
        return GetValue($"{nameof(CellmAddInConfiguration)}:{nameof(CellmAddInConfiguration.StructuredOutputShape)}") == StructuredOutputShape.None.ToString();
    }

    public bool GetOutputRowPressed(IRibbonControl control)
    {
        return GetValue($"{nameof(CellmAddInConfiguration)}:{nameof(CellmAddInConfiguration.StructuredOutputShape)}") == StructuredOutputShape.Row.ToString();
    }

    public bool GetOutputColumnPressed(IRibbonControl control)
    {
        return GetValue($"{nameof(CellmAddInConfiguration)}:{nameof(CellmAddInConfiguration.StructuredOutputShape)}") == StructuredOutputShape.Column.ToString();
    }

    public bool GetOutputTablePressed(IRibbonControl control)
    {
        return GetValue($"{nameof(CellmAddInConfiguration)}:{nameof(CellmAddInConfiguration.StructuredOutputShape)}") == StructuredOutputShape.Table.ToString();
    }

    private void InvalidateOutputToggleButtons()
    {
        _ribbonUi?.InvalidateControl(nameof(OutputGroupControlIds.OutputCell));
        _ribbonUi?.InvalidateControl(nameof(OutputGroupControlIds.OutputRow));
        _ribbonUi?.InvalidateControl(nameof(OutputGroupControlIds.OutputTable));
        _ribbonUi?.InvalidateControl(nameof(OutputGroupControlIds.OutputColumn));
    }
}
