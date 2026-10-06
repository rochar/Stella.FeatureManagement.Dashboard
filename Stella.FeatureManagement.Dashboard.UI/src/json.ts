export type JsonParseResult =
  | { ok: true; value: unknown }
  | { ok: false; error: string }

/** Parses without throwing; the error names the line/column when the engine reports a position. */
export function safeParse(text: string): JsonParseResult {
  try {
    return { ok: true, value: JSON.parse(text) }
  } catch (err) {
    return { ok: false, error: describeJsonError(text, err) }
  }
}

function describeJsonError(text: string, err: unknown): string {
  const raw = err instanceof Error ? err.message : 'Invalid JSON'
  // V8: "... at position 42" (sometimes followed by " (line 3 column 5)"); Firefox reports line/column itself.
  if (/line \d+ column \d+/i.test(raw)) return raw
  const match = /position (\d+)/.exec(raw)
  if (!match) return raw
  const position = Number(match[1])
  const before = text.slice(0, position).split('\n')
  return `${raw.replace(/\s*in JSON at position \d+.*$/, '')} (line ${before.length}, column ${before[before.length - 1].length + 1})`
}

/** Pretty-printed JSON, or the input unchanged when it does not parse. */
export function formatJson(text: string | null | undefined): string {
  if (!text) return '{}'
  const parsed = safeParse(text)
  return parsed.ok ? JSON.stringify(parsed.value, null, 2) : text
}

/** Compact JSON for comparing and storing; null when it does not parse. */
export function compactJson(text: string | null | undefined): string | null {
  if (!text) return null
  const parsed = safeParse(text)
  return parsed.ok ? JSON.stringify(parsed.value) : null
}
