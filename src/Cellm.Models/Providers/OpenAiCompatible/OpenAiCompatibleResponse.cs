﻿using Cellm.Models.Prompts;
using Microsoft.Extensions.AI;

namespace Cellm.Models.Providers.OpenAiCompatible;

internal record OpenAiCompatibleResponse(Prompt Prompt, ChatResponse ChatResponse) : IModelResponse;
