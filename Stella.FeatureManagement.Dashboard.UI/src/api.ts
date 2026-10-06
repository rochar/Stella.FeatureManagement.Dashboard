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
  defaultSettings: string
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
 */
export class ApiError extends Error {
  constructor(message: string, readonly status: number, readonly detail: string | null) {
    super(detail ? `${message}: ${detail}` : message)
  }
}

// Results.BadRequest(string) sends a JSON string; Conflict/NotFound send { message }.
async function readServerMessage(res: Response): Promise<string | null> {
  const text = (await res.text()).trim()
  if (!text) return null
  try {
    const body: unknown = JSON.parse(text)
    if (typeof body === 'string') return body
    if (body && typeof body === 'object') {
      const { message, detail, title } = body as Record<string, unknown>
      const found = [message, detail, title].find(v => typeof v === 'string')
      if (typeof found === 'string') return found
    }
  } catch {
    // Not JSON: use the raw text
  }
  return text
}

async function ensureOk(res: Response, failureMessage: string): Promise<Response> {
  if (res.ok) return res
  const detail = res.status === 400 || res.status === 409 || res.status === 404
    ? await readServerMessage(res)
    : null
  throw new ApiError(`${failureMessage} (${res.status})`, res.status, detail)
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

// The PUT endpoint replaces the whole feature, so send the current state with `changes` applied on top.
export async function putFeature(feature: FeatureState, changes: FeatureChanges, failureMessage: string): Promise<FeatureState> {
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
        : undefined
    })
  })
  return (await ensureOk(res, failureMessage)).json()
}

export async function deleteFeature(name: string): Promise<void> {
  await ensureOk(await fetch(featureUrl(name), { method: 'DELETE' }), 'Failed to delete feature')
}

export const errorMessage = (err: unknown, fallback: string) => err instanceof Error ? err.message : fallback

/** The server's own message when there is one (shown inline), else the full error text. */
export const errorDetail = (err: unknown, fallback: string) =>
  err instanceof ApiError && err.detail ? err.detail : errorMessage(err, fallback)
