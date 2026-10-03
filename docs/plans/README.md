# Plans

Plans produced by agents, skills, or workflows: implementation plans, migration plans,
roadmaps. A plan defines how something will be done; what is being solved and why belongs in
a spec (`specs/`), not here.

**When to add one:** whenever an agent or skill produces a plan worth keeping.

**Status:** `proposed | in-progress | done | abandoned`, kept current in both the front matter
and the index line below. A plan exists to fulfil one request: once `done` or `abandoned` it is
history, never guidance — do not resume or follow it. Anything durable a plan produced or
discovered outlives it as an ADR, memory, or learning, not as the plan itself.

**Naming:** each plan is one file `PLAN-YYYYMMDD-slug.md` — its creation date (equal to its
`date:`, never changed afterwards) and a kebab-case verb-phrase slug, at most 4 words (e.g.
`PLAN-20261003-migrate-auth.md`). Dates, not sequence numbers, so parallel branches never claim
the same id. The whole stem is the id. A plan names the spec it implements in its `spec:` field
(`SPEC-YYYYMMDD-slug`), and the accepted ADRs it relies on in its `adrs:` field
(`ADR-YYYYMMDD-slug`, comma-separated) — each `none` when there are none.

**Split rule:** a plan holds the how — approach, steps, risks, verification. Requirements and
acceptance criteria belong in the spec; a decision that outlives the plan belongs in a new ADR,
added to `adrs:`. Steps are checkboxes, ticked as they are completed; long plans group them
into phases, each ending in a checkpoint.

**Reading:** start with the index line, then the front matter; open the body for the approach
and steps, the spec named in `spec:` for what is being solved, and follow only the ADRs in
`adrs:` that are still accepted.

**Template:** copy [`plan.template.md`](./plan.template.md) from this folder. Template files
are not records — never list them in the index.

**Index line:** `- PLAN-YYYYMMDD-slug — status — one-line summary` — ordered by id (oldest
first); keep the status current here whenever it changes, and keep the whole line at most 120
characters.

## Index

_No plans yet._
