# Stella.FeatureManagement.Dashboard Agentic Workflow

Every change — feature, bug, refactor, infra — flows through three stages. **The user moves work between stages manually**; there is no orchestrator that spans stages.

> **Session reload note.** Agent files (`.github/agents/*.agent.md`) are loaded into the dispatch enum at the start of every Copilot CLI session. After **adding, renaming, or removing** any agent file, end your current session and start a new one before invoking the agent.

```markdown
Requirement / Bug
       │
       ▼
┌──────────────────────────────────────────────┐
│ Stage 1 — Plan                               │
│ Agent: stella-stage-1-plan-author            │
│ Loop: draft → present → refine → APPROVED    │
│ Output: .github/plans/<type>-<slug>-<n>.md   │
└──────────────────────────────────────────────┘
       │ user hands off
       ▼
┌──────────────────────────────────────────────┐
│ Stage 2 — Review                             │
│ Agent: stella-stage-2-plan-reviewer          │
│ Auto-invokes reviewers from plan's           │
│ ## Reviews section in parallel               │
│ Output: consolidated verdict                  │
└──────────────────────────────────────────────┘
       │ user hands off (only if all ✅)
       ▼
┌──────────────────────────────────────────────┐
│ Stage 3 — Implement                          │
│ Driver: stella-stage-3-phase-implementer     │
│ Inputs: approved plan + phase id             │
│ Dispatches the phase's declared implementer, │
│ runs reviews, commits via stella-commit-reviewer │
└──────────────────────────────────────────────┘
```

## Stage 1 — Plan

**Driver:** [`stella-stage-1-plan-author`](stella-stage-1-plan-author.agent.md)

The planner agent:
1. Reads the requirement and project context (copilot-instructions).
2. Applies a built-in critical-thinking pass.
3. Writes the plan to `.github/plans/<type>-<slug>-<n>.md`.
4. Presents it to the user and iterates until approved.
5. Fills the plan's `## Reviews` section with required reviewers.

### Reviewer-selection rubric

| Plan touches… | Reviewer to add |
| --- | --- |
| C# / .NET / backend code | `dotnet-self-learning-architect` |
| React / TypeScript / Dashboard UI | `expert-react-frontend-engineer` |
| New screens or flows / significant UX changes | `software-engineering-team:se-ux-ui-designer` |
| UI elements visible to users | `accessibility` |
| New API endpoint or contract change | `api-architect` |
| `.github/workflows/` or CI configuration | `github-actions-expert` |
| Auth, secrets, PII, encryption | `se-security-reviewer` |
| **Always** (every plan) | `stella-test-strategy-reviewer` |

### Reviewer registry (Stage 2 dispatch table)

| Reviewer key (use in plan) | Source | Type |
| --- | --- | --- |
| `dotnet-self-learning-architect` | `dotnet-self-learning-architect.agent.md` | Project agent |
| `stella-test-strategy-reviewer` | `stella-test-strategy-reviewer.agent.md` | Project agent |
| `expert-react-frontend-engineer` | `expert-react-frontend-engineer.agent.md` | Project agent |
| `accessibility` | `accessibility.agent.md` | Project agent |
| `api-architect` | `api-architect.agent.md` | Project agent |
| `github-actions-expert` | `github-actions-expert.agent.md` | Project agent |
| `software-engineering-team:se-security-reviewer` | `software-engineering-team` plugin | Plugin agent |
| `software-engineering-team:se-ux-ui-designer` | `software-engineering-team` plugin | Plugin agent |

## Stage 2 — Review

**Driver:** [`stella-stage-2-plan-reviewer`](stella-stage-2-plan-reviewer.agent.md)

The user invokes this agent and points it at the approved plan. It:
1. Validates the plan exists, is `status: Planned`, and contains a non-empty `## Reviews` section.
2. Dispatches every reviewer with `Status: ⏳ Pending` — in parallel.
3. Each reviewer updates its own row and appends a review section.
4. Returns a consolidated report: All ✅ = Ready for Stage 3; Any ❌ = loop back to Stage 1.

## Stage 3 — Implement

**Driver:** [`stella-stage-3-phase-implementer`](stella-stage-3-phase-implementer.agent.md)

Invoked with approved plan path + phase identifier. Per invocation, implements **one phase**:
1. Validates plan is `Planned`/`In progress`, Stage 2 is fully ✅, snapshots baseline SHA.
2. Dispatches the phase's declared implementer, branching on `Tests:` value.
3. Runs pre-commit gate (`dotnet format` → build → test for backend; `npm run build:dev` for frontend).
4. Re-dispatches reviewers in parallel — implementation review.
5. Runs built-in `code-review`.
6. Runs `software-engineering-team:se-security-reviewer` (deduplicated).
7. Applies bounded trivial fixes only. Non-trivial findings pause for user.
8. Invokes `stella-commit-reviewer` for final staged review + Conventional Commit.
9. Updates plan: marks tasks ✅, appends `### Phase N — Implemented` summary.

## Plan file `## Reviews` section

Status values: `⏳ Pending`, `✅ Approved`, `⚠️ Approved with comments`, `❌ Changes requested`, `⚠️ Failed`

## Role boundaries

| Agent | Use only for | Don't use for |
| --- | --- | --- |
| `stella-stage-1-plan-author` | Drafting + iterating plans (Stage 1) | Reviewing, implementing |
| `stella-stage-2-plan-reviewer` | Stage 2 orchestration | Drafting, implementing |
| `stella-stage-3-phase-implementer` | Stage 3 orchestration — one phase | Drafting, reviewing, picking different implementer |
| `stella-critical-thinking` | Standalone challenge sessions | Drafting plans |
| `stella-test-strategy-reviewer` | Reviewing test strategy in plans | Writing tests |

## Rules

- Do not skip Stage 1. Even small changes get a plan file.
- Do not let Stage 2 begin until you approve the plan.
- Do not begin Stage 3 until Stage 2 returns `Ready for Stage 3`.
- The plan file is the source of truth.

## Self-Learning System

Every stage agent participates in a repo-wide self-learning loop:
- [`.github/Lessons/`](../Lessons/README.md) — postmortem registry
- [`.github/Memories/`](../Memories/README.md) — durable-context registry

See the Lessons README for governance rules.
