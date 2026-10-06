# Architectural Decision Records (ADRs)

Records of architectural and design decisions: technology choices, conventions adopted,
trade-offs accepted, and approaches rejected (with reasons). Anything important for new and
existing features belongs here.

**When to add one:** whenever an architectural or design decision is made — at decision time,
not retroactively.

**Naming:** each ADR is one file `ADR-YYYYMMDD-slug.md` — its creation date (equal to its
`date:`, never changed afterwards) and a kebab-case noun-phrase slug, at most 4 words (e.g.
`ADR-20261003-use-postgres.md`). Dates, not sequence numbers, so parallel branches never claim
the same id. The whole stem is the id: refer to an ADR elsewhere as `ADR-YYYYMMDD-slug`.

**Front matter:** `id`, `status`, `date`, `superseded-by`, `scope` (paths or areas it binds),
and `summary` (the decision in one line). It is written to be enough on its own: read the body
only when the ADR is `accepted` and its `scope` touches your task.

**Status:** `proposed | accepted | rejected | superseded`, kept current in both the front matter
and the index line below. Only **accepted** ADRs bind future work. `rejected` and `superseded`
records are history: never follow them, and never delete them. To supersede an ADR, write a new
one whose `## Context` says why, then set the old one's `status: superseded` and
`superseded-by: ADR-YYYYMMDD-slug`. Git history keeps earlier states; there is no log section.

**Body:** `## Context` holds the forces, constraints, and rejected options (one line each, with
why). `## Decision` holds the decision in a few imperative sentences and a `Binds:` line — what
future work must respect. Keep both short.

**Template:** copy [`adr.template.md`](./adr.template.md) from this folder. Template files are
not records — never list them in the index.

**Index line:** `- ADR-YYYYMMDD-slug — status — one-line summary` — ordered by id (oldest
first); keep the status current here whenever it changes, and keep the whole line at most 120
characters.

## Index

- ADR-20261006-additive-public-dto-fields — accepted — add public DTO fields as init-only props, not ctor params
- ADR-20261006-buffered-usage-counters — accepted — buffer usage counts in memory; flush via additive upsert
