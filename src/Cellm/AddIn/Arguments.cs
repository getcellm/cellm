using Cellm.Models.Providers;

namespace Cellm.AddIn;

internal record Cells(int RowFirst, int ColumnFirst, object[,] Values);

internal record Arguments(Provider Provider, string Model, Cells? Cells, object Instructions, double Temperature);
