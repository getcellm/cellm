using Cellm.Models.Providers;

namespace Cellm.AddIn;

internal record Cells(int RowFirst, int RowLast, int ColumnFirst, int ColumnLast, string[,] Values);

internal record Arguments(Provider Provider, string Model, Cells Cells, string Instructions, double Temperature);
