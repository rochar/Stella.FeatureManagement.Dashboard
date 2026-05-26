# Stella.FeatureManagement.Dashboard Agent Catalog

This repository keeps a **curated mixed agent set**:
- **Upstream awesome-copilot agents** for broadly reusable roles
- **Stella-specific agents** for repository conventions, delivery flow, and project-specific behavior

> **New here?** Read [`WORKFLOW.md`](WORKFLOW.md) first. Every change goes through three stages: **Plan → Review → Implement**.

## Three-stage workflow at a glance

| Stage | Driver agent | Purpose |
| --- | --- | --- |
| **1. Plan** | `stella-stage-1-plan-author` | Drafts and iterates a plan file in `.github/plans/`. Fills `## Reviews` section. |
| **2. Review** | `stella-stage-2-plan-reviewer` | Auto-invokes the listed reviewers in parallel; consolidates verdicts. |
| **3. Implement** | `stella-stage-3-phase-implementer` | Per phase: dispatches the plan's declared implementer, runs gate + reviews, commits via `stella-commit-reviewer`. |

## Stage 1, 2 & 3 drivers

| Agent | Stage | Use for |
| --- | --- | --- |
| `stella-stage-1-plan-author.agent.md` | 1 | Drafting and iterating plan files |
| `stella-stage-2-plan-reviewer.agent.md` | 2 | Orchestrating reviewers from a plan's `## Reviews` section |
| `stella-stage-3-phase-implementer.agent.md` | 3 | Orchestrating one phase of implementation |
| `stella-test-strategy-reviewer.agent.md` | 2 | Reviewing test strategy in plans |
| `stella-critical-thinking.agent.md` | adhoc | Standalone challenge sessions |

## Stage 2 reviewers

| Agent | Reviews |
| --- | --- |
| `dotnet-self-learning-architect.agent.md` | Backend / .NET design |
| `expert-react-frontend-engineer.agent.md` | React / frontend design |
| `accessibility.agent.md` | Accessibility / WCAG |
| `api-architect.agent.md` | API contracts |
| `github-actions-expert.agent.md` | CI workflows |
| `software-engineering-team:se-security-reviewer` (plugin) | Security |
| `software-engineering-team:se-ux-ui-designer` (plugin) | UX / UI design |

## Stage 3 — Implementation specialists

| Agent | Use for |
| --- | --- |
| `expert-dotnet-software-engineer.agent.md` | Production .NET implementation |
| `expert-react-frontend-engineer.agent.md` | Production React implementation |
| `csharp-dotnet-janitor.agent.md` | Tech-debt cleanup, modernization |
| `debug.agent.md` | Systematic debugging |
| `stella-generate-unit-tests.agent.md` | Writing tests (for test-backfill phases) |
| `stella-commit-reviewer.agent.md` | Final staged review + Conventional Commit |
| `repo-architect.agent.md` | Copilot asset structure |
| `adr-generator.agent.md` | Authoring ADRs |

## Routing matrix (Stage 3)

| Requirement | Implementer |
| --- | --- |
| Build a backend feature | `expert-dotnet-software-engineer` |
| Build a frontend feature | `expert-react-frontend-engineer` |
| Investigate a failure | `debug` |
| Modernize / clean up .NET code | `csharp-dotnet-janitor` |
| Generate unit tests (backfill) | `stella-generate-unit-tests` |
| Update a CI/CD workflow | `github-actions-expert` |
| Write an ADR | `adr-generator` |
| Reorganize Copilot assets | `repo-architect` |

## Overlap guide

| Zone | Use A when… | Use B when… |
| --- | --- | --- |
| .NET code | `csharp-dotnet-janitor` — tech debt, cleanup | `expert-dotnet-software-engineer` — new features |
| Architecture | `api-architect` — API contracts | `dotnet-self-learning-architect` — cross-cutting design |
| Tests | `stella-test-strategy-reviewer` — reviewing strategy | `stella-generate-unit-tests` — writing tests |
| Plans | `stella-stage-1-plan-author` — drafts Stage 1 plans | `stella-stage-2-plan-reviewer` — orchestrates Stage 2 |

## Operating Rules

- Do not add a new local agent if an upstream agent already covers the same role.
- Never skip Stage 1 — even small changes get a plan file.
- Do not move to Stage 3 until Stage 2 returns `Ready for Stage 3`.
- Keep this file, `WORKFLOW.md`, and `.github/copilot-instructions.md` aligned when the catalog changes.
