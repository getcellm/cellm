using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cellm.Prompts;

public interface IHasPrompt
{
    Prompt Prompt { get; set; }
}