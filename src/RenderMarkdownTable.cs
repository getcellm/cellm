using ExcelDna.Integration;
using System.Text;
using System.Xml.Linq;

namespace Cellm.RenderMarkdownTable;

public class MarkdownTable
{
    public static string Render(object[,] input)
    {
        try
        {
            int rows = input.GetLength(0);
            int cols = input.GetLength(1);
            StringBuilder sb = new StringBuilder();

            // Header row with column coordinates
            sb.Append("|   |");
            for (int col = 0; col < cols; col++)
            {
                sb.Append($" {GetColumnName(col)} |");
            }
            sb.AppendLine();

            // Separator row
            sb.Append("|");
            for (int i = 0; i <= cols; i++)
            {
                sb.Append("---|");
            }
            sb.AppendLine();

            // Data rows
            for (int row = 0; row < rows; row++)
            {
                sb.Append($"| {row + 1} |");
                for (int col = 0; col < cols; col++)
                {
                    string cellValue = input[row, col]?.ToString() ?? "";
                    sb.Append($" {cellValue} |");
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    private static string GetColumnName(int columnNumber)
    {
        string columnName = "";
        while (columnNumber >= 0)
        {
            columnName = (char)('A' + (columnNumber % 26)) + columnName;
            columnNumber = (columnNumber / 26) - 1;
        }
        return columnName;
    }
}
