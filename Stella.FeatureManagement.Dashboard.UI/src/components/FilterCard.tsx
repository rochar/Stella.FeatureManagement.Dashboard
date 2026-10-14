import { useState } from 'react'
import { CHANGED_ELSEWHERE, errorDetail, isConflict, type FeatureFilter, type FeatureState } from '../api'
import type { FeaturesStore } from '../hooks/useFeatures'
import { compactJson, formatJson, safeParse } from '../json'
import { Button, IconButton } from './Button'
import { Icon } from './Icon'
import { JsonEditor } from './JsonEditor'
import { useToast } from './Toast'

interface FilterCardProps {
  feature: FeatureState
  filter: FeatureFilter
  index: number
  defaults: string | null
  store: FeaturesStore
  onDelete: () => void
}

/** One filter of a feature: readable parameters, with an inline JSON editor. */
export function FilterCard({ feature, filter, index, defaults, store, onDelete }: FilterCardProps) {
  const toast = useToast()
  const [draft, setDraft] = useState<string | null>(null)
  // The version this edit is based on, taken with the draft: its filters build the PUT body and its
  // updatedAt is the token. A Refresh, a toggle or another card's save replaces `feature` but not
  // this, so Save then conflicts instead of overwriting the newer change with a stale draft.
  const [base, setBase] = useState<FeatureState | null>(null)
  const [saving, setSaving] = useState(false)
  const [serverError, setServerError] = useState<string | null>(null)
  const editing = draft !== null

  const startEditing = () => {
    setBase(store.storedCopy(feature))
    setDraft(formatJson(filter.parameters))
    setServerError(null)
  }

  const cancel = () => {
    setDraft(null)
    setBase(null)
    setServerError(null)
  }

  // Drops the stale draft and adopts the stored copy as the new base. Keeping the draft with the
  // fresh token would let the next Save overwrite, with no 409, the change that caused this one.
  const rebase = (stored: FeatureState) => {
    const storedFilter = stored.filters[index]
    if (storedFilter?.filterType !== filter.filterType) {
      // This card's filter is no longer at its index, so there is nothing left to edit here.
      cancel()
      toast.error(`${filter.filterType} filter: ${CHANGED_ELSEWHERE}`)
      return
    }
    setBase(stored)
    setDraft(formatJson(storedFilter.parameters))
    setServerError(CHANGED_ELSEWHERE)
  }

  const save = async () => {
    if (draft === null || base === null) return
    const next = compactJson(draft)
    if (next === null) return
    // Unchanged: skip the PUT, which would bump UpdatedAt and run OnFeatureChanging.
    if (next === compactJson(base.filters[index]?.parameters)) { cancel(); return }

    setSaving(true)
    setServerError(null)
    try {
      const filters = base.filters.map((f, i) => i === index ? { filterType: f.filterType, parameters: next } : f)
      await store.updateFeature(base, { filters }, base.updatedAt ?? null, 'Failed to update filter')
      toast.success(`${filter.filterType} filter updated`)
      setDraft(null)
      setBase(null)
    } catch (err) {
      if (isConflict(err)) rebase(err.serverFeature)
      else setServerError(errorDetail(err, 'Failed to update filter'))
    } finally {
      setSaving(false)
    }
  }

  const draftInvalid = draft !== null && !safeParse(draft).ok

  return (
    <div className="filter-card">
      <div className="filter-card-header">
        <h4 className="filter-card-title"><Icon name="filter" />{filter.filterType}</h4>
        {!editing && (
          <div className="filter-card-actions">
            <IconButton icon="edit" label={`Edit ${filter.filterType} parameters`} onClick={startEditing} />
            <IconButton icon="trash" tone="danger" label={`Remove ${filter.filterType} filter`} onClick={onDelete} />
          </div>
        )}
      </div>
      {editing ? (
        <div className="json-editor-wrap" onKeyDown={e => { if (e.key === 'Escape' && !saving) { e.stopPropagation(); cancel() } }}>
          <JsonEditor
            label="Parameters"
            value={draft}
            onChange={v => { setDraft(v); setServerError(null) }}
            defaults={defaults}
            serverError={serverError}
            disabled={saving}
            rows={Math.min(14, Math.max(4, draft.split('\n').length + 1))}
            autoFocus
          />
          <div className="filter-edit-actions">
            <Button size="sm" onClick={cancel} disabled={saving}>Cancel</Button>
            <Button size="sm" variant="primary" onClick={save} loading={saving} disabled={draftInvalid || store.isUpdating(feature.name)}>Save</Button>
          </div>
        </div>
      ) : (
        <FilterParameters parameters={filter.parameters} />
      )}
    </div>
  )
}

function FilterParameters({ parameters }: { parameters: string | null }) {
  if (!parameters) return <p className="filter-params-empty">No parameters.</p>

  const parsed = safeParse(parameters)
  if (!parsed.ok) {
    return (
      <>
        <p className="filter-params-empty"><span className="badge badge-warning"><Icon name="warning" />Stored parameters are not valid JSON</span></p>
        <pre className="code-block">{parameters}</pre>
      </>
    )
  }

  const value = parsed.value
  const isFlatObject = value !== null && typeof value === 'object' && !Array.isArray(value)
    && Object.values(value).every(v => v === null || typeof v !== 'object')
  if (!isFlatObject) return <pre className="code-block">{JSON.stringify(value, null, 2)}</pre>

  const entries = Object.entries(value)
  if (entries.length === 0) return <p className="filter-params-empty">No parameters.</p>
  return (
    <dl className="filter-params">
      {entries.map(([key, v]) => (
        <div key={key} className="filter-param">
          <dt>{key}</dt>
          <dd>{typeof v === 'string' ? v : JSON.stringify(v)}</dd>
        </div>
      ))}
    </dl>
  )
}
