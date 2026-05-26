# Stella.FeatureManagement.Dashboard — Memories

This folder is the **durable-context registry** for the Stella.FeatureManagement.Dashboard repo. Every file here records a piece of long-lived knowledge that future agents and contributors need to know — architecture decisions, recurring constraints, framework-version recipes, environmental gotchas.

Memories differ from [Lessons](../Lessons/README.md) in intent:

- **Lesson** = "we made this mistake; here's how to prevent recurrence."
- **Memory** = "this is the way it is; reuse this knowledge."

Both are governed by the same rules — see [Lessons README § Governance](../Lessons/README.md#governance) for the canonical version.

## Index (newest first by PatternId)

| PatternId | Title | Status |
| --- | --- | --- |

Keep this index sorted by `PatternId` **descending** (newest first). When you add a new entry, prepend its row.

## File-naming convention

```markdown
M-<NNN>-<kebab-case-title>.md
```

Same numbering / replacement rules as Lessons — see the [Lessons README](../Lessons/README.md#file-naming-convention).

## Required template

Every file MUST follow this template:

```markdown
# Memory: <short title>

## Metadata
- PatternId: M-<NNN>
- PatternVersion: 1.0
- Status: active | deprecated | blocked
- Supersedes: <PatternId or "none">
- CreatedAt: <YYYY-MM-DD>
- LastValidatedAt: <YYYY-MM-DD>
- ValidationEvidence: <one-line proof — commit SHA, deployed service, doc URL>

## Source Context
- Triggering task:
- Scope/system:
- Date/time:

## Memory
- Key fact or decision:
- Why it matters:

## Applicability
- When to reuse:
- Preconditions/limitations:

## Actionable Guidance
- Recommended future action:
- Related files/services/components:
```

## Governance, lifecycle, sub-agent contract

All identical to the Lessons folder — see the [Lessons README](../Lessons/README.md#governance). The same agents are required to consult this folder pre-task, propose memories post-task, and observe the dedupe + conflict-resolution rules.
