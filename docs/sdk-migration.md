# SDK Migration: Anthropic, Mistral, and Gemini

## Summary

Three provider SDKs need replacing due to incompatibilities and maintainability concerns. Both community SDKs (`Anthropic.SDK`, `Mistral.SDK`) are maintained by the same author (tghamm) and have become inactive. The Gemini provider uses an OpenAI-compatible endpoint that doesn't fully support tool use schemas.

## ~~Anthropic.SDK (5.10.0)~~ ✅ DONE

Migrated to official `Anthropic` SDK (v12.11.0). The community `Anthropic.SDK` was incompatible with MEAI 10.4.x (`MissingMethodException` on `HostedMcpServerTool.AuthorizationToken`). The official SDK has native IChatClient support and accepts custom HttpClient, so the resilient HttpClient pipeline is preserved. Also fixed a bug where the entitlement check referenced `EnableAzureProvider` instead of `EnableAnthropicProvider`. Removed the `RateLimitsExceeded` exception from `RateLimiterHelpers` (was Anthropic.SDK-specific; 429 status is already handled by `retryableStatusCodes`). All 4 integration tests pass.

## ~~Mistral.SDK (2.3.1)~~ ✅ DONE

Migrated both `AddMistralChatClient()` and `AddCellmChatClient()` to use `OpenAIClient` with custom endpoint, same pattern as DeepSeek and OpenRouter. Removed `Mistral.SDK` dependency entirely. All 4 Mistral integration tests pass (basic prompt, file reader, file search, Playwright MCP).

**Known issue: Magistral thinking models.** The OpenAI .NET SDK cannot deserialize Magistral's `thinking` content part type (`ArgumentOutOfRangeException: Unknown ChatMessageContentPartKind value: thinking`). The failure occurs at the deserialization level before `MistralThinkingBehavior` can process the response. This is a limitation of using the OpenAI SDK with Mistral's extended thinking format. Magistral models (`magistral-small-2509`, `magistral-medium-2509`) are currently broken.

## ~~Gemini (OpenAI-compatible endpoint)~~ ✅ DONE

Migrated to official `Google.GenAI` SDK (v1.6.1). The OpenAI-compatible endpoint rejected `strict: true` / `additionalProperties: false` in tool schemas. The native SDK handles tool schemas correctly. All 4 integration tests pass (basic prompt, file reader, file search, Playwright MCP).

**Tradeoff:** Google.GenAI does not support custom HttpClient injection, so HTTP-level retry/timeout from the resilience pipeline is not available for Gemini. Rate limiting (application layer) is unaffected. `GeminiTemperatureBehavior` (0-1 → 0-2 scaling) is still needed — the native SDK passes temperature as-is.

## Additional considerations

When switching SDKs, provider-specific behaviors and other code may need updating. Examples include but are not limited to:

- `GeminiTemperatureBehavior` — temperature scaling may differ with native SDK
- `AdditionalPropertiesBehavior` — provider-specific additional properties format may change
- `ProviderRequestHandler.UseJsonSchemaResponseFormat()` — structured output support flags
- Provider configuration classes (`SupportsJsonSchemaResponses`, `SupportsStructuredOutputWithTools`) — verify accuracy with new SDKs
- Resilient HTTP client integration — new SDKs may handle HTTP clients differently

A thorough review of all provider-specific code paths is needed during migration.
