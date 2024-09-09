using System.Text;
using Cellm.AddIn.Exceptions;
using ExcelDna.Integration;
using Microsoft.Office.Interop.Excel;

namespace Cellm.AddIn.Prompts;

public class Format
{
    public static string Cells(ExcelReference reference)
    {
        try
        {
            var app = (Application)ExcelDnaUtil.Application;
            var sheetName = (string)XlCall.Excel(XlCall.xlSheetNm, reference);
            sheetName = sheetName[(sheetName.LastIndexOf("]") + 1)..];
            var worksheet = app.Sheets[sheetName];

            var stringBuilder = new StringBuilder();

            var rows = reference.RowLast - reference.RowFirst + 1;
            var columns = reference.ColumnLast - reference.ColumnFirst + 1;

            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    var value = worksheet.Cells[reference.RowFirst + row + 1, reference.ColumnFirst + column + 1].Text;

                    stringBuilder.Append("| ");
                    stringBuilder.Append(GetColumnName(reference.ColumnFirst + column) + GetRowName(reference.RowFirst + row));
                    stringBuilder.Append(' ');
                    stringBuilder.Append(value);
                }

                stringBuilder.AppendLine(" | ");
            }

            return stringBuilder.ToString();
        }
        catch (Exception ex)
        {
            throw new CellmException("#CELLM_RENDER_ERROR?", ex);
        }
    }

    private static string GetColumnName(int columnNumber)
    {
        string columnName = "";
        while (columnNumber >= 0)
        {
            columnName = (char)('A' + columnNumber % 26) + columnName;
            columnNumber = columnNumber / 26 - 1;
        }
        return columnName;
    }

    private static string GetRowName(int rowNumber)
    {
        return (rowNumber + 1).ToString();
    }
}
