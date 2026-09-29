# Plans

Plans produced by agents, skills, or workflows: implementation plans, migration plans,
roadmaps. A plan defines how something will be done; what is being solved and why belongs in
a spec (`specs/`), not here.

**When to add one:** whenever an agent or skill produces a plan worth keeping.

**Status:** `proposed | in-progress | done | abandoned`, kept current in both `plan.md` and the
index line below. A plan exists to fulfil one request: once `done` or `abandoned` it is history,
never guidance — do not resume or follow it. Anything durable a plan produced or discovered
outlives it as an ADR, memory, or learning, not as the plan itself.

**Naming:** each plan is a directory `NNNN-slug/` (zero-padded per-folder sequence; kebab-case
verb-phrase slug, at most 4 words — e.g. `0007-migrate-auth/`) containing `plan.md`. Take the
next number from the index below (first record: `0001`). A plan names the spec it implements
by its `NNNN-slug` in its `Spec:` line, and the accepted ADRs it relies on in its `ADRs:`
line — each `none` when there are none.

**Split rule:** `plan.md` holds the how — approach, steps, risks, verification. Requirements
and acceptance criteria belong in the spec; a decision that outlives the plan belongs in a new
ADR, added to the `ADRs:` line. Steps are checkboxes, ticked as they are completed; long plans
group them into phases, each ending in a checkpoint.

**Reading:** start with the index line; open `plan.md` for the approach and steps, and the
spec named in its `Spec:` line for what is being solved; follow only the ADRs in its `ADRs:`
line that are still accepted.

**Templates:** copy [`plan.template.md`](./plan.template.md) from this folder. Template files
are not records — never list them in the index.

**Index line:** `- NNNN-slug — status — YYYY-MM-DD — one-line summary` — keep the status
current here whenever it changes, and keep the whole line at most 120 characters.

## Index

_No plans yet._
