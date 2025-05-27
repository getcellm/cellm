using Cellm.Models.Providers;

namespace Cellm.AddIn;

internal record Arguments(Provider Provider, string Model, Cells? Cells, object Instructions, double Temperature);
