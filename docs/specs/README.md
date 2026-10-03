# Specs

Specifications of what is to be built and why: features, bug fixes, changes in behaviour. A
spec defines the problem and what "done" means; how it is implemented belongs in a plan
(`plans/`), not here.

**When to add one:** whenever a problem or requirement is worth writing down before it is
implemented.

**Status:** `draft | approved | implemented | abandoned`, kept current in both the front matter
and the index line below. `implemented` and `abandoned` specs are history, never guidance — the
code is the source of truth for implemented behaviour. Anything durable a spec settled
outlives it as an ADR, memory, or learning, not as the spec itself.

**Naming:** each spec is one file `SPEC-YYYYMMDD-slug.md` — its creation date (equal to its
`date:`, never changed afterwards) and a kebab-case noun-phrase slug, at most 4 words (e.g.
`SPEC-20261003-bulk-export.md`). Dates, not sequence numbers, so parallel branches never claim
the same id. The whole stem is the id: plans refer to a spec by its `SPEC-YYYYMMDD-slug`.

**Split rule:** a spec states the what and the why — objective, scope, requirements,
acceptance criteria. Design, root cause, and implementation steps belong in a plan.

**Reading:** start with the index line, then the front matter (`summary` says what is needed);
open the body for the full definition.

**Template:** copy [`spec.template.md`](./spec.template.md) from this folder. Template files
are not records — never list them in the index.

**Index line:** `- SPEC-YYYYMMDD-slug — status — one-line summary` — ordered by id (oldest
first); keep the status current here whenever it changes, and keep the whole line at most 120
characters.

## Index

_No specs yet._
