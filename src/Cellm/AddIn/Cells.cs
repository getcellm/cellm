using Cellm.AddIn.Exceptions;
using ExcelDna.Integration;

namespace Cellm.AddIn;

internal record Cells(int RowFirst, int ColumnFirst, object Values)
{
    public int RowFirst { get; } = RowFirst;

    public int ColumnFirst { get; } = ColumnFirst;

    // Short-circuit if inputs are #GETTING_DATA. Excel will re-trigger the function when inputs are updated with realized values.
    public object Values { get; } = Values switch
    {
        ExcelError.ExcelErrorGettingData => throw new GettingDataException(),
        ExcelError.ExcelErrorNA => throw new GettingDataException(),
        _ => Values
    };
}
