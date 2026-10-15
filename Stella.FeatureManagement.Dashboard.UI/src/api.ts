import type { DailyUsage, FeatureUsage } from './UsageChart'

export interface FeatureFilter {
  filterType: string
  parameters: string | null
}

export interface FeatureState {
  name: string
  isEnabled: boolean
  description: string | null
  filters: FeatureFilter[]
  application: string
  createdAt?: string | null
  updatedAt?: string | null
}

export interface AvailableFilter {
  name: string
  /** JSON that pre-fills the editor; null when the filter was registered without defaults. */
  defaultSettings: string | null
}

export type FeatureChanges = Partial<Pick<FeatureState, 'isEnabled' | 'description' | 'filters' | 'application'>>

// API base path - use VITE_API_URL if available (Aspire), otherwise fall back to relative path
const apiBase = (resource: string) => import.meta.env.VITE_API_URL
  ? `${import.meta.env.VITE_API_URL}/features/dashboardapi/${resource}`
  : `../dashboardapi/${resource}`

export const API_BASE = apiBase('features')
export const FILTERS_API_BASE = apiBase('filters')
export const APPLICATIONS_API_BASE = apiBase('applications')
const USAGE_API_BASE = apiBase('usage')

const featureUrl = (name: string) => `${API_BASE}/${encodeURIComponent(name)}`

/**
 * Error from a dashboard API call. `detail` holds the server's own message (validation /
 * OnFeatureChanging rejections), so callers can show it next to the field that caused it.
 * `serverFeature` is set only when a PUT conflicts: the feature as stored now, so the editor that
 * sent it can rebase on that copy instead of guessing what changed.
 */
export class ApiError extends Error {
  constructor(message: string, readonly status: number, readonly detail: string | null, readonly serverFeature: FeatureState | null = null) {
    super(detail ? `${message}: ${detail}` : message)
  }
}

/** Shown when an edit's base was changed by someone else and the UI now shows the stored version. */
export const CHANGED_ELSEWHERE = 'Changed elsewhere — showing the latest version; re-apply your edit to it.'

/** A PUT refused because the feature changed after the edit began; it carries the stored copy. */
export const isConflict = (err: unknown): err is ApiError & { serverFeature: FeatureState } =>
  err instanceof ApiError && err.status === 409 && err.serverFeature !== null

// Results.BadRequest(string) sends a JSON string (validation rejections); Conflict/NotFound and the
// malformed-token 400 send { message }. Both shapes reach the PUT, so both must yield the message.
async function readServerBody(res: Response): Promise<{ message: string | null; body: unknown }> {
  const text = (await res.text()).trim()
  if (!text) return { message: null, body: null }
  let body: unknown
  try {
    body = JSON.parse(text)
  } catch {
    // Not JSON: use the raw text
    return { message: text, body: null }
  }
  if (typeof body === 'string') return { message: body, body }
  if (body && typeof body === 'object') {
    const { message, detail, title } = body as Record<string, unknown>
    const found = [message, detail, title].find(v => typeof v === 'string')
    if (typeof found === 'string') return { message: found, body }
  }
  return { message: text, body }
}

async function ensureOk(res: Response, failureMessage: string): Promise<Response> {
  if (res.ok) return res
  const detail = res.status === 400 || res.status === 409 || res.status === 404
    ? (await readServerBody(res)).message
    : null
  throw new ApiError(`${failureMessage} (${res.status})`, res.status, detail)
}

// Only the fields an editor rebases on are checked: a malformed 409 body must not become the cached
// feature, or the next render would crash on it.
function isFeatureState(value: unknown): value is FeatureState {
  if (!value || typeof value !== 'object') return false
  const f = value as Record<string, unknown>
  return typeof f.name === 'string'
    && typeof f.isEnabled === 'boolean'
    && typeof f.application === 'string'
    && Array.isArray(f.filters)
    && f.filters.every(x => !!x && typeof x === 'object' && typeof (x as Record<string, unknown>).filterType === 'string')
}

async function getJson<T>(url: string, failureMessage: string, signal?: AbortSignal): Promise<T> {
  const res = await ensureOk(await fetch(url, { signal }), failureMessage)
  return res.json()
}

export const fetchFeatures = () => getJson<FeatureState[]>(API_BASE, 'Failed to load features')
export const fetchAvailableFilters = () => getJson<AvailableFilter[]>(FILTERS_API_BASE, 'Failed to load filters')
export const fetchApplications = () => getJson<string[]>(APPLICATIONS_API_BASE, 'Failed to load applications')

export async function fetchUsageMap(days: number): Promise<Map<string, DailyUsage[]>> {
  const data = await getJson<FeatureUsage[]>(`${USAGE_API_BASE}?days=${days}`, 'Failed to load usage')
  // A Map, not a plain object: a feature named e.g. "constructor" must not resolve to an Object.prototype member.
  return new Map(data.map(u => [u.name, u.days]))
}

export async function fetchFeatureUsage(name: string, days: number, signal?: AbortSignal): Promise<DailyUsage[]> {
  const data = await getJson<FeatureUsage>(`${USAGE_API_BASE}/${encodeURIComponent(name)}?days=${days}`, 'Failed to load usage', signal)
  return data.days
}

export async function createFeature(body: Pick<FeatureState, 'name' | 'isEnabled' | 'description' | 'application'>): Promise<FeatureState> {
  const res = await fetch(API_BASE, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body)
  })
  if (res.status === 409) throw new ApiError('Could not create feature', 409, `A feature named '${body.name}' already exists.`)
  return (await ensureOk(res, 'Failed to create feature')).json()
}

/**
 * The PUT endpoint replaces the whole feature, so `feature` must be the snapshot the edit began from,
 * with `changes` applied on top. The token is a separate argument and is sent as given, never read from
 * `feature` here: only the caller knows which version its edit is based on, and a token refreshed after
 * the edit began would let a stale body overwrite a newer change without a conflict.
 */
export async function putFeature(feature: FeatureState, changes: FeatureChanges, expectedUpdatedAt: string | null, failureMessage: string): Promise<FeatureState> {
  const { isEnabled, description, filters, application } = { ...feature, ...changes }
  const res = await fetch(featureUrl(feature.name), {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      isEnabled,
      description,
      application,
      filters: filters.length > 0
        ? filters.map(f => ({ filterType: f.filterType, parameters: f.parameters }))
        : undefined,
      expectedUpdatedAt
    })
  })
  if (res.status === 409) {
    const { message, body } = await readServerBody(res)
    const stored = body && typeof body === 'object' ? (body as Record<string, unknown>).feature : null
    if (isFeatureState(stored)) throw new ApiError(`${failureMessage} (409)`, 409, CHANGED_ELSEWHERE, stored)
    throw new ApiError(`${failureMessage} (409)`, 409, message)
  }
  return (await ensureOk(res, failureMessage)).json()
}

export async function deleteFeature(name: string): Promise<void> {
  await ensureOk(await fetch(featureUrl(name), { method: 'DELETE' }), 'Failed to delete feature')
}

export const errorMessage = (err: unknown, fallback: string) => err instanceof Error ? err.message : fallback

/** The server's own message when there is one (shown inline), else the full error text. */
export const errorDetail = (err: unknown, fallback: string) =>
  err instanceof ApiError && err.detail ? err.detail : errorMessage(err, fallback)
