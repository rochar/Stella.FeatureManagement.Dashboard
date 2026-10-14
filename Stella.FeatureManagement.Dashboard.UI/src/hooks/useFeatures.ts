import { useCallback, useEffect, useRef, useState } from 'react'
import * as api from '../api'
import type { AvailableFilter, FeatureChanges, FeatureState } from '../api'
import type { DailyUsage } from '../UsageChart'

export const SPARK_DAYS = 7

// The server stores microseconds, may trim trailing zeros, and moves `updatedAt` forward by as little
// as 1 µs, so neither a plain string compare nor Date (milliseconds) orders two versions reliably.
function versionKey(iso: string | null | undefined): string | null {
  const match = iso ? /^(\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2})(?:\.(\d{1,7}))?Z$/.exec(iso) : null
  return match ? `${match[1]}.${(match[2] ?? '').padEnd(7, '0')}` : null
}

/**
 * Keeps the cached copy of a feature moving forward only. A response that left the server before a
 * newer copy arrived (a slow refresh, a refresh racing a save) must not put the older version back,
 * or the list shows a version that is gone and every edit started from it (an editor's base, the
 * toggle, Add filter) conflicts for no visible reason. Unknown or unparsable versions fall back to
 * accepting the incoming copy, as before.
 */
function newest(cached: FeatureState | undefined, incoming: FeatureState): FeatureState {
  if (!cached) return incoming
  const a = versionKey(incoming.updatedAt)
  const b = versionKey(cached.updatedAt)
  return a !== null && b !== null && a < b ? cached : incoming
}

/**
 * Dashboard data and mutations. Mutations throw (with the server's message) so the dialog that
 * started them can show the error inline; callers decide between inline errors and toasts.
 * A PUT that conflicts first replaces the cached feature with the stored copy, then re-throws so the
 * editor that sent it stays open and rebases on `err.serverFeature`.
 */
export function useFeatures() {
  const [features, setFeatures] = useState<FeatureState[]>([])
  const [serverApplications, setServerApplications] = useState<string[]>([])
  // Added in the sidebar but not stored yet: an application exists once a feature uses it.
  const [draftApplications, setDraftApplications] = useState<string[]>([])
  const [availableFilters, setAvailableFilters] = useState<AvailableFilter[]>([])
  // null until loaded (or when loading failed): render no usage column rather than a false "no usage"
  const [usage, setUsage] = useState<Map<string, DailyUsage[]> | null>(null)
  const [loaded, setLoaded] = useState(false)
  const [refreshing, setRefreshing] = useState(false)
  const [loadError, setLoadError] = useState<string | null>(null)
  const [lastUpdated, setLastUpdated] = useState<Date | null>(null)
  // Features with a PUT in flight. Every PUT replaces the whole feature, so two at once for the same
  // feature would let the slower one silently undo the other (e.g. a toggle reverting a filter edit).
  // The ref answers synchronously (two clicks in one tick); the state re-renders the disabled controls.
  const pendingRef = useRef(new Set<string>())
  const [pending, setPending] = useState<ReadonlySet<string>>(() => new Set())
  // Switch positions whose toggle PUT is still in flight, kept apart from `features` so that list only
  // ever holds versions the server returned. A flipped copy there would carry a still-valid token: an
  // editor opened mid-toggle would capture it as its base, and if the toggle then failed without a 409
  // that editor's Save would silently apply the flip the user saw rolled back.
  const [pendingToggles, setPendingToggles] = useState<ReadonlyMap<string, boolean>>(() => new Map())
  const usageRequest = useRef(0)
  const featuresRequest = useRef(0)

  const beginUpdate = useCallback((name: string) => {
    if (pendingRef.current.has(name)) return false
    pendingRef.current.add(name)
    setPending(new Set(pendingRef.current))
    return true
  }, [])

  const endUpdate = useCallback((name: string) => {
    pendingRef.current.delete(name)
    setPending(new Set(pendingRef.current))
  }, [])

  const loadApplications = useCallback(async () => {
    try {
      setServerApplications(await api.fetchApplications())
    } catch (err) {
      console.error('Failed to fetch applications:', err)
    }
  }, [])

  // Separate from the feature load so the list never waits on usage; failures only hide the sparklines.
  // Only the latest request may set state, so a slow earlier refresh can't overwrite newer data.
  const loadUsage = useCallback(async () => {
    const request = ++usageRequest.current
    try {
      const data = await api.fetchUsageMap(SPARK_DAYS)
      if (request === usageRequest.current) setUsage(data)
    } catch (err) {
      console.error('Failed to fetch usage:', err)
      if (request === usageRequest.current) setUsage(null)
    }
  }, [])

  const refresh = useCallback(async () => {
    const request = ++featuresRequest.current
    setRefreshing(true)
    loadUsage()
    loadApplications()
    try {
      const fetched = await api.fetchFeatures()
      // A later refresh owns the list; applying this older response would only roll it back.
      if (request !== featuresRequest.current) return
      setFeatures(prev => {
        const cached = new Map(prev.map(f => [f.name, f]))
        return fetched.map(f => newest(cached.get(f.name), f))
      })
      setLoadError(null)
      setLastUpdated(new Date())
    } catch (err) {
      if (request === featuresRequest.current) setLoadError(api.errorMessage(err, 'Failed to load features'))
    } finally {
      if (request === featuresRequest.current) {
        setLoaded(true)
        setRefreshing(false)
      }
    }
  }, [loadUsage, loadApplications])

  useEffect(() => {
    refresh()
    api.fetchAvailableFilters()
      .then(setAvailableFilters)
      .catch(err => console.error('Failed to fetch available filters:', err))
  }, [refresh])

  const replaceFeature = (updated: FeatureState) => {
    setFeatures(prev => prev.map(f => f.name === updated.name ? newest(f, updated) : f))
    setLastUpdated(new Date())
  }

  /**
   * Optimistic: flips the switch at once and rolls back when the PUT fails. The row has no draft, so
   * the copy it rendered at click time is both the body's base and the token. The flip lives only in
   * `pendingToggles`, so dropping it is the whole rollback.
   */
  const toggleFeature = useCallback(async (feature: FeatureState) => {
    if (!beginUpdate(feature.name)) return
    const newState = !feature.isEnabled
    setPendingToggles(prev => new Map(prev).set(feature.name, newState))
    try {
      // Use the server's copy so the refreshed updatedAt is shown
      replaceFeature(await api.putFeature(feature, { isEnabled: newState }, feature.updatedAt ?? null, `Failed to ${newState ? 'enable' : 'disable'} ${feature.name}`))
    } catch (err) {
      // On a conflict the pre-toggle copy is known to be stale, so roll back to what is stored instead.
      if (api.isConflict(err)) replaceFeature(err.serverFeature)
      throw err
    } finally {
      setPendingToggles(prev => {
        const next = new Map(prev)
        next.delete(feature.name)
        return next
      })
      endUpdate(feature.name)
    }
  }, [beginUpdate, endUpdate])

  const createFeature = useCallback(async (body: Parameters<typeof api.createFeature>[0]) => {
    const created = await api.createFeature(body)
    setFeatures(prev => [...prev, created])
    setLastUpdated(new Date())
    loadApplications()
    return created
  }, [loadApplications])

  /**
   * `base` is the snapshot the caller's edit began from and `expectedUpdatedAt` the token captured with
   * it; both are passed in, never read from the cache here, because the cache may have moved on since.
   */
  const updateFeature = useCallback(async (base: FeatureState, changes: FeatureChanges, expectedUpdatedAt: string | null, failureMessage: string) => {
    if (!beginUpdate(base.name)) {
      throw new Error(`Another change to ${base.name} is still being saved. Try again in a moment.`)
    }
    try {
      const updated = await api.putFeature(base, changes, expectedUpdatedAt, failureMessage)
      replaceFeature(updated)
      if (changes.application !== undefined) loadApplications()
      return updated
    } catch (err) {
      // Every view of this feature, not only the editor that got the 409, must stop showing the
      // overwritten version; the editor itself rebases on err.serverFeature.
      if (api.isConflict(err)) replaceFeature(err.serverFeature)
      throw err
    } finally {
      endUpdate(base.name)
    }
  }, [loadApplications, beginUpdate, endUpdate])

  const deleteFeature = useCallback(async (name: string) => {
    await api.deleteFeature(name)
    setFeatures(prev => prev.filter(f => f.name !== name))
    setLastUpdated(new Date())
    loadApplications()
  }, [loadApplications])

  const addDraftApplication = useCallback((name: string) => {
    setDraftApplications(prev => prev.includes(name) ? prev : [...prev, name])
  }, [])

  const applications = [...new Set([...serverApplications, ...draftApplications])].sort((a, b) => a.localeCompare(b))
  const isUpdating = (name: string) => pending.has(name)

  // What the list shows: the stored versions with any in-flight toggle drawn on top.
  const shownFeatures = pendingToggles.size === 0
    ? features
    : features.map(f => pendingToggles.has(f.name) ? { ...f, isEnabled: pendingToggles.get(f.name) === true } : f)

  /**
   * The stored version of a shown feature. An edit must take its base from here, not from the copy a
   * row rendered, because that copy may show a toggle the server has not confirmed (see
   * `pendingToggles`). Falls back to the given copy once the feature has left the list.
   */
  const storedCopy = (feature: FeatureState): FeatureState =>
    features.find(f => f.name === feature.name) ?? feature

  return {
    features: shownFeatures,
    applications,
    availableFilters,
    usage,
    loaded,
    refreshing,
    loadError,
    lastUpdated,
    isUpdating,
    storedCopy,
    refresh,
    toggleFeature,
    createFeature,
    updateFeature,
    deleteFeature,
    addDraftApplication,
  }
}

export type FeaturesStore = ReturnType<typeof useFeatures>
