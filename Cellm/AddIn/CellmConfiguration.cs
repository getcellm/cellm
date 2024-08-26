using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cellm.AddIn;

internal class CellmConfiguration
{
    public string DefaultModelProvider { get; init; }

    public CellmConfiguration()
    {
        DefaultModelProvider = default!;
    }
}
