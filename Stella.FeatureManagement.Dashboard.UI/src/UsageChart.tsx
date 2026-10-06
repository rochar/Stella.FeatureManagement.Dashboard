import { useEffect, useState } from 'react'
import { errorDetail, fetchFeatureUsage } from './api'
import { Dialog } from './components/Dialog'
import { Icon } from './components/Icon'

export interface DailyUsage {
  date: string
  enabledCount: number
  disabledCount: number
}

export interface FeatureUsage {
  name: string
  days: DailyUsage[]
}

// Usage days are UTC dates ("2026-10-06"): format them in UTC so they don't shift a day in negative offsets.
const parseDay = (date: string) => new Date(`${date}T00:00:00Z`)
const dayFormatter = new Intl.DateTimeFormat(undefined, { weekday: 'short', month: 'short', day: 'numeric', timeZone: 'UTC' })
const tickFormatter = new Intl.DateTimeFormat(undefined, { month: 'short', day: 'numeric', timeZone: 'UTC' })
const numberFormatter = new Intl.NumberFormat()

const dayTotal = (d: DailyUsage) => d.enabledCount + d.disabledCount
export const totalUsage = (days: DailyUsage[]) => days.reduce((sum, d) => sum + dayTotal(d), 0)

// Flat line with a slash: shown instead of a chart when a window has no evaluations.
export function NoUsageIcon({ className }: { className?: string }) {
  return (
    <svg className={className} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
      <path d="M2 12h5l2-4 3 8 2-4h8" />
      <path d="M3 3l18 18" />
    </svg>
  )
}

interface HoveredDay {
  index: number
  x: number
  y: number
}

/**
 * Stacked daily columns: enabled evaluations (highlighted) on top of disabled ones (gray), scaled to the
 * busiest day of the window. `spark` is the compact row variant; `full` adds date ticks for the modal.
 */
export function UsageChart({ days, variant }: { days: DailyUsage[]; variant: 'spark' | 'full' }) {
  const [hovered, setHovered] = useState<HoveredDay | null>(null)
  const max = Math.max(1, ...days.map(dayTotal))
  const tickEvery = Math.max(1, Math.ceil(days.length / 6))
  const lastIndex = days.length - 1
  const hoveredDay = hovered ? days[hovered.index] : null
  const isHovering = hovered !== null

  // The fixed tooltip's coordinates go stale when anything scrolls under the pointer, so drop it on scroll.
  useEffect(() => {
    if (!isHovering) return
    const hide = () => setHovered(null)
    window.addEventListener('scroll', hide, { capture: true, passive: true })
    return () => window.removeEventListener('scroll', hide, { capture: true })
  }, [isHovering])

  return (
    <div className={`usage-chart usage-chart-${variant}`} onMouseLeave={() => setHovered(null)}>
      <div className="usage-bars">
        {days.map((d, index) => {
          const total = dayTotal(d)
          return (
            <div
              key={d.date}
              className={`usage-col ${hovered?.index === index ? 'hovered' : ''}`}
              onMouseEnter={(e) => {
                // The tooltip is position: fixed so the feature list's overflow: hidden can't clip it.
                const rect = e.currentTarget.getBoundingClientRect()
                setHovered({ index, x: rect.left + rect.width / 2, y: rect.top })
              }}
            >
              {total === 0 ? (
                <div className="usage-stub" />
              ) : (
                <div className="usage-stack" style={{ height: `${(total / max) * 100}%` }}>
                  <div className="usage-seg usage-seg-enabled" style={{ flexGrow: d.enabledCount }} />
                  <div className="usage-seg usage-seg-disabled" style={{ flexGrow: d.disabledCount }} />
                </div>
              )}
            </div>
          )
        })}
      </div>
      {variant === 'full' && (
        <div className="usage-axis">
          {/* Ticks counted back from today so the latest day is always labelled */}
          {days.map((d, index) => (
            <span key={d.date}>{(lastIndex - index) % tickEvery === 0 ? tickFormatter.format(parseDay(d.date)) : ''}</span>
          ))}
        </div>
      )}
      {hovered && hoveredDay && (
        <div className="usage-tooltip" style={{ left: hovered.x, top: hovered.y }} role="tooltip">
          <div className="usage-tooltip-date">
            {dayFormatter.format(parseDay(hoveredDay.date))}
            {hovered.index === lastIndex && <span className="usage-tooltip-today"> · today</span>}
          </div>
          <div className="usage-tooltip-row">
            <span className="usage-swatch usage-swatch-enabled" />Enabled
            <strong>{numberFormatter.format(hoveredDay.enabledCount)}</strong>
          </div>
          <div className="usage-tooltip-row">
            <span className="usage-swatch usage-swatch-disabled" />Disabled
            <strong>{numberFormatter.format(hoveredDay.disabledCount)}</strong>
          </div>
          <div className="usage-tooltip-row usage-tooltip-total">
            Total<strong>{numberFormatter.format(dayTotal(hoveredDay))}</strong>
          </div>
        </div>
      )}
    </div>
  )
}

const MODAL_DAYS = 30

export function UsageModal({ featureName, onClose }: {
  featureName: string
  onClose: () => void
}) {
  const [days, setDays] = useState<DailyUsage[] | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    const controller = new AbortController()
    fetchFeatureUsage(featureName, MODAL_DAYS, controller.signal)
      .then(setDays)
      .catch(err => {
        if (!controller.signal.aborted) setError(errorDetail(err, 'Failed to load usage'))
      })
    return () => controller.abort()
  }, [featureName])

  const enabled = days?.reduce((sum, d) => sum + d.enabledCount, 0) ?? 0
  const total = days ? totalUsage(days) : 0

  return (
    <Dialog
      title="Usage"
      description={<><strong>{featureName}</strong> · evaluations over the last {MODAL_DAYS} days (UTC)</>}
      size="lg"
      onClose={onClose}
    >
      {error && (
        <div className="alert" role="alert">
          <Icon name="alert" />
          <span className="alert-text">{error}</span>
        </div>
      )}
      {!days && !error && (
        <div className="usage-loading" role="status">
          <span className="spinner" aria-hidden="true" />
          <span className="visually-hidden">Loading usage…</span>
        </div>
      )}
      {days && total === 0 && (
        <div className="empty-state">
          <span className="empty-state-icon"><NoUsageIcon className="usage-empty-icon" /></span>
          <p className="empty-state-title">No usage yet</p>
          <p className="empty-state-text">This feature wasn't evaluated in the last {MODAL_DAYS} days.</p>
        </div>
      )}
      {days && total > 0 && (
        <>
          <div className="usage-summary">
            <div>
              <span className="usage-summary-value">{numberFormatter.format(total)}</span>
              <span className="usage-summary-label">Evaluations</span>
            </div>
            <div>
              <span className="usage-summary-value">
                <span className="usage-swatch usage-swatch-enabled" />
                {numberFormatter.format(enabled)}
              </span>
              <span className="usage-summary-label">Enabled · {Math.round((enabled / total) * 100)}%</span>
            </div>
            <div>
              <span className="usage-summary-value">
                <span className="usage-swatch usage-swatch-disabled" />
                {numberFormatter.format(total - enabled)}
              </span>
              <span className="usage-summary-label">Disabled</span>
            </div>
          </div>
          <UsageChart days={days} variant="full" />
        </>
      )}
    </Dialog>
  )
}
