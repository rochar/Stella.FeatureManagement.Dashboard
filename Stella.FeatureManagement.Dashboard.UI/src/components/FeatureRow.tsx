import { errorMessage, type FeatureState } from '../api'
import { SPARK_DAYS, type FeaturesStore } from '../hooks/useFeatures'
import { NoUsageIcon, totalUsage, UsageChart, type DailyUsage } from '../UsageChart'
import { Button, IconButton } from './Button'
import { FilterCard } from './FilterCard'
import { Icon } from './Icon'
import { Switch } from './Switch'
import { useToast } from './Toast'

// One formatter for all rows: toLocaleString with options builds a new Intl.DateTimeFormat per call.
const dateFormatter = new Intl.DateTimeFormat(undefined, { dateStyle: 'medium', timeStyle: 'short' })
const formatDate = (iso: string) => dateFormatter.format(new Date(iso))

const relativeFormatter = new Intl.RelativeTimeFormat(undefined, { numeric: 'auto' })
const RELATIVE_UNITS: [Intl.RelativeTimeFormatUnit, number][] = [
  ['year', 31_536_000], ['month', 2_592_000], ['week', 604_800], ['day', 86_400], ['hour', 3_600], ['minute', 60],
]
function formatRelative(iso: string) {
  const seconds = (new Date(iso).getTime() - Date.now()) / 1000
  for (const [unit, size] of RELATIVE_UNITS) {
    if (Math.abs(seconds) >= size) return relativeFormatter.format(Math.round(seconds / size), unit)
  }
  return 'just now'
}

// Edits within a minute of creation count as "never updated" (the create round-trip can stamp both).
const wasUpdated = (createdAt: string, updatedAt?: string | null): updatedAt is string =>
  !!updatedAt && new Date(updatedAt).getTime() - new Date(createdAt).getTime() > 60_000

interface FeatureRowProps {
  feature: FeatureState
  expanded: boolean
  onToggleExpanded: () => void
  usage: Map<string, DailyUsage[]> | null
  store: FeaturesStore
  editingName: string | null
  onEdit: () => void
  onDelete: () => void
  onOpenUsage: (name: string) => void
  onAddFilter: () => void
  onDeleteFilter: (index: number) => void
}

export function FeatureRow({ feature: f, expanded, onToggleExpanded, usage, store, editingName, onEdit, onDelete, onOpenUsage, onAddFilter, onDeleteFilter }: FeatureRowProps) {
  const toast = useToast()
  const detailsId = `feature-details-${encodeURIComponent(f.name)}`

  const toggle = async () => {
    try {
      await store.toggleFeature(f)
    } catch (err) {
      toast.error(errorMessage(err, `Failed to update ${f.name}`))
    }
  }

  const canAddFilter = store.availableFilters.some(a => !f.filters.some(existing => existing.filterType === a.name))

  return (
    <li className={`feature-item ${expanded ? 'expanded' : ''}`}>
      <div className="feature-row">
        <button type="button" className="feature-expand" aria-expanded={expanded} aria-controls={detailsId} onClick={onToggleExpanded}>
          <Icon name="chevronRight" className="feature-expand-icon" />
          <span className="feature-info">
            <span className="feature-title">
              <span className="feature-name">{f.name}</span>
              {f.filters.length > 0 && (
                <span className="badge badge-accent" title={`${f.filters.length} filter${f.filters.length === 1 ? '' : 's'}`}>
                  <Icon name="filter" />
                  {f.filters.length}
                  <span className="visually-hidden"> filter{f.filters.length === 1 ? '' : 's'}</span>
                </span>
              )}
            </span>
            {f.description && <span className="feature-description" title={f.description}>{f.description}</span>}
          </span>
        </button>
        {f.createdAt ? <FeatureActivity createdAt={f.createdAt} updatedAt={f.updatedAt} /> : <span className="feature-activity" />}
        {usage && <FeatureUsageSpark name={f.name} days={usage.get(f.name)} onOpen={onOpenUsage} />}
        <div className="feature-state">
          <Switch
            checked={f.isEnabled}
            onChange={toggle}
            label={`${f.name} enabled`}
            disabled={store.isUpdating(f.name) || editingName === f.name}
          />
        </div>
        <div className="feature-actions">
          <IconButton icon="edit" label={`Edit ${f.name}`} onClick={onEdit} />
          <IconButton icon="trash" tone="danger" label={`Delete ${f.name}`} onClick={onDelete} />
        </div>
      </div>
      {expanded && (
        <div className="feature-details" id={detailsId}>
          <h3 className="details-section-title">Filters</h3>
          {f.filters.length === 0 ? (
            <div className="details-empty">
              <span>No filters — when enabled, this feature is on for everyone.</span>
              {canAddFilter && <Button size="sm" variant="primary" icon="plus" onClick={onAddFilter}>Add filter</Button>}
            </div>
          ) : (
            <div className="filter-list">
              {f.filters.map((filter, index) => (
                <FilterCard
                  key={`${filter.filterType}-${index}`}
                  feature={f}
                  filter={filter}
                  index={index}
                  defaults={store.availableFilters.find(a => a.name === filter.filterType)?.defaultSettings ?? null}
                  store={store}
                  onDelete={() => onDeleteFilter(index)}
                />
              ))}
            </div>
          )}
          <div className="details-footer">
            {f.createdAt ? <FeatureDates createdAt={f.createdAt} updatedAt={f.updatedAt} /> : <span />}
            {f.filters.length > 0 && canAddFilter && (
              <Button size="sm" variant="dashed" icon="plus" onClick={onAddFilter}>Add filter</Button>
            )}
          </div>
        </div>
      )}
    </li>
  )
}

// Compact "last activity" column in a feature row: one relative time, absolute dates on hover.
function FeatureActivity({ createdAt, updatedAt }: { createdAt: string; updatedAt?: string | null }) {
  const updated = wasUpdated(createdAt, updatedAt)
  const when = updated ? updatedAt : createdAt
  const title = `Created ${formatDate(createdAt)}${updated ? `\nLast updated ${formatDate(updatedAt)}` : ''}`
  return (
    <span className="feature-activity" title={title}>
      <span className="feature-activity-label">{updated ? 'Updated' : 'Created'}</span>
      <time className="feature-activity-value" dateTime={when}>{formatRelative(when)}</time>
    </span>
  )
}

// Full, absolute dates at the bottom of the expanded panel (tooltips don't work on touch).
function FeatureDates({ createdAt, updatedAt }: { createdAt: string; updatedAt?: string | null }) {
  return (
    <dl className="feature-dates">
      <div><dt>Created</dt><dd><time dateTime={createdAt}>{formatDate(createdAt)}</time></dd></div>
      {wasUpdated(createdAt, updatedAt) && (
        <div><dt>Last updated</dt><dd><time dateTime={updatedAt}>{formatDate(updatedAt)}</time></dd></div>
      )}
    </dl>
  )
}

// Last-7-days column chart in a feature row; a muted "no usage" marker when nothing was evaluated.
// Either way it opens the 30-day dialog, which may still have older data.
function FeatureUsageSpark({ name, days, onOpen }: { name: string; days?: DailyUsage[]; onOpen: (name: string) => void }) {
  const total = days ? totalUsage(days) : 0
  return (
    <button
      type="button"
      className="usage-spark-btn"
      onClick={() => onOpen(name)}
      aria-label={total > 0
        ? `Usage of ${name}: ${total} evaluations in the last ${SPARK_DAYS} days. Open 30-day usage`
        : `No usage of ${name} in the last ${SPARK_DAYS} days. Open 30-day usage`}
    >
      {days && total > 0 ? (
        <UsageChart days={days} variant="spark" />
      ) : (
        <span className="usage-none" title={`No usage in the last ${SPARK_DAYS} days`}>
          <NoUsageIcon className="usage-none-icon" />
          No usage · {SPARK_DAYS}d
        </span>
      )}
    </button>
  )
}
