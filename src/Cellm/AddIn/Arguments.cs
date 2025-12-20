using Cellm.Models.Prompts;
using Cellm.Models.Providers;

namespace Cellm.AddIn;

internal record Arguments(Provider Provider, string Model, IReadOnlyList<Range> Ranges, object Instructions, double Temperature, StructuredOutputShape OutputShape);
