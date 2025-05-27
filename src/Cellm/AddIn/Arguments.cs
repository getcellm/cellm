using Cellm.Models.Providers;

namespace Cellm.AddIn;

internal record Arguments(Provider Provider, string Model, object Cells, object Instructions, double Temperature);
