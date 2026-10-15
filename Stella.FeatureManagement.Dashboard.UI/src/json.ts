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

/**
 * Editor text for stored parameters or a filter's defaults: blank when there are none (null or
 * whitespace), pretty-printed JSON, or the input unchanged when it does not parse.
 */
export function editorText(text: string | null | undefined): string {
  if (!text?.trim()) return ''
  const parsed = safeParse(text)
  return parsed.ok ? JSON.stringify(parsed.value, null, 2) : text
}

export type ParametersResult =
  | { ok: true; value: string | null }
  | { ok: false }

/**
 * The parameters to store for editor (or stored) text, also used to compare two of them: null for a blank
 * or whitespace-only text, compact JSON for valid JSON, and not ok when the text does not parse.
 */
export function toParameters(text: string | null | undefined): ParametersResult {
  if (!text?.trim()) return { ok: true, value: null }
  const parsed = safeParse(text)
  return parsed.ok ? { ok: true, value: JSON.stringify(parsed.value) } : { ok: false }
}
