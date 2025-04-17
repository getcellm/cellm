using Cellm.Models.Providers;

namespace Cellm.AddIn;

public record Arguments(Provider Provider, string Model, string Context, string Instructions, double Temperature);
