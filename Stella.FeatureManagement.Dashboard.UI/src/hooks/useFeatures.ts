import { useCallback, useEffect, useRef, useState } from 'react'
import * as api from '../api'
import type { AvailableFilter, FeatureChanges, FeatureState } from '../api'
import type { DailyUsage } from '../UsageChart'

export const SPARK_DAYS = 7

/**
 * Dashboard data and mutations. Mutations throw (with the server's message) so the dialog that
 * started them can show the error inline; callers decide between inline errors and toasts.
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
  const usageRequest = useRef(0)

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
    setRefreshing(true)
    loadUsage()
    loadApplications()
    try {
      setFeatures(await api.fetchFeatures())
      setLoadError(null)
      setLastUpdated(new Date())
    } catch (err) {
      setLoadError(api.errorMessage(err, 'Failed to load features'))
    } finally {
      setLoaded(true)
      setRefreshing(false)
    }
  }, [loadUsage, loadApplications])

  useEffect(() => {
    refresh()
    api.fetchAvailableFilters()
      .then(setAvailableFilters)
      .catch(err => console.error('Failed to fetch available filters:', err))
  }, [refresh])

  const replaceFeature = (updated: FeatureState) => {
    setFeatures(prev => prev.map(f => f.name === updated.name ? updated : f))
    setLastUpdated(new Date())
  }

  /** Optimistic: flips the switch at once and rolls back when the PUT fails. */
  const toggleFeature = useCallback(async (feature: FeatureState) => {
    if (!beginUpdate(feature.name)) return
    const newState = !feature.isEnabled
    setFeatures(prev => prev.map(f => f.name === feature.name ? { ...f, isEnabled: newState } : f))
    try {
      // Use the server's copy so the refreshed updatedAt is shown
      replaceFeature(await api.putFeature(feature, { isEnabled: newState }, `Failed to ${newState ? 'enable' : 'disable'} ${feature.name}`))
    } catch (err) {
      setFeatures(prev => prev.map(f => f.name === feature.name ? { ...f, isEnabled: feature.isEnabled } : f))
      throw err
    } finally {
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

  const updateFeature = useCallback(async (feature: FeatureState, changes: FeatureChanges, failureMessage: string) => {
    if (!beginUpdate(feature.name)) {
      throw new Error(`Another change to ${feature.name} is still being saved. Try again in a moment.`)
    }
    try {
      const updated = await api.putFeature(feature, changes, failureMessage)
      replaceFeature(updated)
      if (changes.application !== undefined) loadApplications()
      return updated
    } finally {
      endUpdate(feature.name)
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

  return {
    features,
    applications,
    availableFilters,
    usage,
    loaded,
    refreshing,
    loadError,
    lastUpdated,
    isUpdating,
    refresh,
    toggleFeature,
    createFeature,
    updateFeature,
    deleteFeature,
    addDraftApplication,
  }
}

export type FeaturesStore = ReturnType<typeof useFeatures>
