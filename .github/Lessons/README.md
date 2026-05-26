# Stella.FeatureManagement.Dashboard — Lessons

This folder is the **postmortem registry** for the Stella.FeatureManagement.Dashboard repo. Every file here records a concrete mistake (CI failure, bad design call, broken assumption, missed review finding) and the preventive action that should stop it from happening again.

The three workflow agents (Stage 1 plan author, Stage 2 plan reviewer, Stage 3 phase implementer) and their dispatched sub-agents **must consult this folder before starting a task** and **must propose new entries after one** (see § Lifecycle below).

## Index (newest first by PatternId)

| PatternId | Title | Status |
| --- | --- | --- |

Keep this index sorted by `PatternId` **descending** (newest first). When you add a new entry, prepend its row.

## File-naming convention

```markdown
L-<NNN>-<kebab-case-title>.md
```

- `<NNN>` is a zero-padded sequence (`001`, `002`, …) shared across all `active`/`deprecated`/`blocked` lessons. Never reuse a number; if a lesson is replaced, mark the old one `deprecated` and link via `Supersedes`.
- `<kebab-case-title>` summarizes the lesson in 4–8 words.

## Required template

Every file MUST follow this template:

```markdown
# Lesson: <short title>

## Metadata
- PatternId: L-<NNN>
- PatternVersion: 1.0
- Status: active | deprecated | blocked
- Supersedes: <PatternId or "none">
- CreatedAt: <YYYY-MM-DD>
- LastValidatedAt: <YYYY-MM-DD>
- ValidationEvidence: <one-line proof the lesson is real — e.g. CI run URL, plan section, commit SHA>

## Task Context
- Triggering task:
- Date/time:
- Impacted area:

## Mistake
- What went wrong:
- Expected behavior:
- Actual behavior:

## Root Cause Analysis
- Primary cause:
- Contributing factors:
- Detection gap:

## Resolution
- Fix implemented:
- Why this fix works:
- Verification performed:

## Preventive Actions
- Guardrails added:
- Tests/checks added:
- Process updates:

## Reuse Guidance
- How to apply this lesson in future tasks:
```

## Governance

These rules apply uniformly to lessons (this folder) and memories (`.github/Memories/`).

### 1. Versioning

Every entry carries `PatternId`, `PatternVersion`, `Status`, `Supersedes`. Bump `PatternVersion` on meaningful guidance changes. Set `LastValidatedAt` whenever the entry's accuracy is re-confirmed.

### 2. Pre-write dedupe (required)

Before creating a new entry, grep existing files for keywords from the proposed title and root cause. If a close match exists:
- Update the existing entry (bump `PatternVersion`, add new `ValidationEvidence`).
- Only create a new file when the root cause is genuinely distinct.

### 3. Conflict resolution

If two entries contradict each other:
- The newer one (higher `PatternId`) takes precedence **only if** it explicitly `Supersedes` the older one.
- Otherwise, flag the conflict for human resolution — do not silently ignore.

### 4. Status lifecycle

- `active` — the default for new entries. Agents must honor it.
- `deprecated` — replaced by a newer entry (`Supersedes` field links forward). Agents ignore it.
- `blocked` — the approach described is temporarily forbidden (e.g., waiting on a dependency fix). Agents must refuse to use the blocked approach and surface it to the user.

### 5. Safety gate

Never delete a lesson file. Mark it `deprecated` or `blocked` instead. Git history is not a substitute for explicit status — agents grep file content, not git log.
