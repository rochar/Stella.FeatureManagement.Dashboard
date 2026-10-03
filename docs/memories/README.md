# Memories

Durable facts agents learned about this repository or its domain that are not derivable from
the code or git history: environment quirks, external-system behavior, domain rules, ownership
knowledge.

**When to add one:** whenever a session learns something a future session would otherwise have
to rediscover — but only if every rule below holds:

- The fact is **not derivable** from the code, git history, CLAUDE.md, or an existing doc —
  if it is, it does not belong here.
- The fact will **still matter** in a future session — nothing session-specific, speculative,
  or about work in progress (that is a plan).
- It is **one fact** per record, stated so it stands alone.
- It **duplicates no existing memory** — update the existing record instead of adding a
  near-copy.
- It contains **no secrets** or credentials.

**Type:** every memory declares exactly one type — `environment` (local/CI/tooling quirks),
`external-system` (behavior of systems this repository talks to), `domain-rule` (business or
domain constraints), `ownership` (who owns or decides what).

**Staleness:** every memory carries a `verified:` date — the last time the fact was confirmed
true. When a session relies on a memory and confirms it still holds, update `verified:` in the
file. A memory found to be wrong is corrected in place; a memory no longer relevant is deleted,
but its index line stays, suffixed `— deleted`, so ids are never reused and references stay
valid. If the correction itself is a lesson, record that in `learnings/`. The older
the `verified:` date, the less a memory should be trusted without re-checking.

**Naming:** each memory is one file `MEM-YYYYMMDD-slug.md` — its creation date (equal to its
`date:`, never changed afterwards) and a kebab-case slug, at most 4 words (e.g.
`MEM-20261003-staging-db-quirk.md`). Dates, not sequence numbers, so parallel branches never
claim the same id. The whole stem is the id. A fact that outgrows one file is split into new
records.

**Reading:** the front matter's `summary` states the fact; open the body for detail and how to
verify it.

**Template:** copy [`memory.template.md`](./memory.template.md) from this folder. Template
files are not records — never list them in the index.

**Index line:** `- MEM-YYYYMMDD-slug — type — one-line summary` — ordered by id (oldest
first); keep the whole line at most 120 characters.

## Index

_No memories yet._
