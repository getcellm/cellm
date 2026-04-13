---
name: sentry-triage
description: Use when triaging production bugs from Sentry, debugging unresolved issues, or when asked to check Sentry for errors
---

# Sentry Triage

## Overview

Automatically triage the highest-impact unresolved Sentry issues using systematic root cause analysis. Filters out user-specific flukes, writes a plan for real bugs, reproduces them with tests, and fixes them.

**Core principle:** Find root cause before attempting fixes. Never ship a fix without a failing test first.

**Violating the letter of this process is violating the spirit of debugging.**

## Sentry Configuration

- Org: `cellm`
- Region URL: `https://de.sentry.io`
- Project: `client`

## The Process

You MUST complete each phase before proceeding to the next.

### Phase 1: Load and Rank Issues

1. **Fetch top unresolved issues sorted by frequency:**

```
mcp__sentry__list_issues(
  organizationSlug='cellm',
  regionUrl='https://de.sentry.io',
  projectSlugOrId='client',
  query='is:unresolved',
  sort='freq',
  limit=5
)
```

2. **Present the issues to the user** as a numbered list with:
   - Issue ID
   - Title / error message
   - Event count
   - User count (if available)
   - First seen / last seen

3. **Pick the highest-impact issue** (most frequent + most users affected). Present your choice and reasoning. Ask the user to confirm or pick a different one.

### Phase 2: Root Cause Investigation

**BEFORE attempting ANY fix:**

1. **Get full issue details:**

```
mcp__sentry__get_sentry_resource(
  resourceType='issue',
  organizationSlug='cellm',
  resourceId='<issue-short-id>'
)
```

2. **Get recent events for context:**

```
mcp__sentry__list_issue_events(
  issueId='<issue-short-id>',
  organizationSlug='cellm',
  regionUrl='https://de.sentry.io',
  limit=5
)
```

3. **Optionally run Seer analysis** if the stack trace alone is not enough to understand root cause:

```
mcp__sentry__analyze_issue_with_seer(
  organizationSlug='cellm',
  regionUrl='https://de.sentry.io',
  issueId='<issue-short-id>'
)
```

4. **Read the error messages and stack traces carefully.** Don't skip past them.

5. **Trace backward through the call chain** to find the original trigger. Don't stop at where the error appears - trace up until you find the source. Read the actual source files in the codebase at each level of the stack.

6. **Check recent changes** that could have caused the issue (git log, git diff).

### Phase 3: Classify the Issue

After investigation, classify the issue into one of two categories:

#### Category A: User-Specific / Environmental

Issues caused by the user's specific environment that we cannot and should not fix in code. Examples:
- File or folder permission errors on the user's machine
- Incorrect provider API URLs configured by the user
- Missing API keys or expired tokens
- Antivirus or firewall blocking requests
- Corrupted local Excel installation
- Network-specific issues (corporate proxy, VPN)
- OS-specific quirks unique to one machine

**If Category A:**

STOP. Present your findings to the user:

```
I believe this issue is user-specific and should be marked as resolved without a code change.

**Issue:** <title>
**Evidence:** <what you found in the events/stack trace>
**Why it's environmental:** <specific reasoning - e.g., "All 3 events come from the same user,
  the error is a file permission denied on a path specific to their machine">
**Impact of fixing in code:** This would affect all users unnecessarily.

Should I mark this as resolved in Sentry?
```

Wait for the user to confirm before marking as resolved.

#### Category B: Real Bug

The issue is caused by a defect in our code that affects or could affect any user.

**If Category B:** Proceed to Phase 4.

### Phase 4: Hypothesis

1. **State your hypothesis clearly:**
   - "I think X is the root cause because Y"
   - Be specific, not vague
   - Reference exact file paths and line numbers

2. **Identify what evidence would confirm or reject the hypothesis:**
   - What behavior would a test need to demonstrate?
   - What input triggers the bug?
   - What is the expected vs actual output?

3. **Present hypothesis to the user** for validation before proceeding.

### Phase 5: Write a Plan

**REQUIRED SUB-SKILL:** Invoke `superpowers:writing-plans` to create a plan.

The plan should cover:

1. **Write a failing test** that reproduces the bug based on the hypothesis
   - Prefer a **unit test** if the bug can be isolated to a single component
   - If the bug requires Excel interaction, write an **integration test**
   - If neither is possible, STOP and ask the user what to do

2. **Run the test** to confirm it fails (RED)

3. **Write the minimal fix** that makes the test pass

4. **Run the test** to confirm it passes (GREEN)

5. **Run the full test suite** to confirm no regressions

The plan validates the hypothesis: if the test fails as predicted, the hypothesis is confirmed. If the test passes unexpectedly, the hypothesis is rejected - return to Phase 4 with new information.

### Phase 6: Execute the Plan

**REQUIRED SUB-SKILL:** Invoke `superpowers:executing-plans` to execute the plan task by task.

### Phase 7: Wrap Up

After the fix is verified:

1. **Present a suggested commit message** to the user (do NOT commit yourself)
2. **Ask the user** if the Sentry issue should be marked as resolved

## Red Flags - STOP and Return to Phase 2

If you catch yourself:
- Proposing a fix without reading the stack trace
- Fixing where the error appears instead of where it originates
- Skipping the hypothesis step
- Writing a fix without a failing test
- "It's probably X, let me just fix that"
- Changing code that affects all users for a user-specific issue

**ALL of these mean: STOP. Return to Phase 2.**

## Test Guidelines

When writing tests for Sentry issues:

- **Unit tests:** Use real `ConfigureServices` DI setup, only mock Excel-dependent services
- **Missing API keys:** Tests must fail loudly with `Assert.Fail`, never skip silently
- **Run only relevant tests:** API calls cost money, only run tests for the provider being worked on
- **Always run `dotnet format`** before suggesting a commit

## Quick Reference

| Phase | Key Activity | Gate |
|-------|-------------|------|
| **1. Load Issues** | Fetch top 5, pick highest impact | User confirms issue choice |
| **2. Investigate** | Stack traces, events, source code | Understand WHAT and WHY |
| **3. Classify** | Environmental vs real bug | User confirms classification |
| **4. Hypothesis** | State root cause theory | User validates hypothesis |
| **5. Plan** | Use writing-plans skill | Plan reviewed |
| **6. Execute** | Use executing-plans skill | Tests pass, no regressions |
| **7. Wrap Up** | Suggest commit, resolve issue | User commits |
