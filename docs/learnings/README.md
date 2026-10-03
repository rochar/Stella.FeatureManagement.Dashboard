# Learnings

Lessons learned: approaches that failed and why, user corrections, gotchas that cost time,
post-mortems.

**When to add one:** whenever something did not work as expected and the reason is worth
knowing next time — but only if every rule below holds:

- It **cost real time** or produced a wrong result, and could plausibly recur.
- The lesson is **actionable** — what to do or avoid next time, not just "X happened".
- It is **not prevented by the fix itself** — an ordinary bug fixed in code needs no learning;
  record one only when the fix does not stop the mistake from being repeated elsewhere.
- It **duplicates no existing learning** — extend the existing record instead of adding a
  near-copy.

**Obsolescence:** a learning records something that happened, so it is never re-verified — but
its lesson can stop applying (the gotcha is fixed upstream, or a guard now prevents the mistake
structurally). When that happens, mark the record obsolete in place: set its front matter's
`obsolete: YYYY-MM-DD — <why it no longer applies>` and keep the rest as history. Its index line stays, suffixed `— obsolete`, so ids are never reused and references
stay valid. Obsolete learnings are history, not guidance — skip them when skimming the
index.

**Naming:** each learning is one file `LRN-YYYYMMDD-slug.md` — its creation date (equal to its
`date:`, never changed afterwards) and a kebab-case slug, at most 4 words (e.g.
`LRN-20261003-hook-stdout-buffering.md`). Dates, not sequence numbers, so parallel branches
never claim the same id. The whole stem is the id. A lesson that outgrows one file is split
into new records.

**Reading:** the front matter's `summary` states the lesson; skip records with `obsolete:` set;
open the body for what happened.

**Template:** copy [`learning.template.md`](./learning.template.md) from this folder — lead
with the lesson, keep the story under it. Template files are not records — never list them in
the index.

**Index line:** `- LRN-YYYYMMDD-slug — one-line summary` — ordered by id (oldest first);
keep the whole line at most 120 characters.

## Index

_No learnings yet._
