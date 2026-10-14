import { useId, useState, type FormEvent } from 'react'
import { errorDetail, isConflict, type FeatureState } from '../api'
import type { FeaturesStore } from '../hooks/useFeatures'
import { compactJson, formatJson, safeParse } from '../json'
import { Button } from './Button'
import { Dialog } from './Dialog'
import { JsonEditor } from './JsonEditor'
import { useToast } from './Toast'

export function AddFilterDialog({ feature, store, onClose }: { feature: FeatureState; store: FeaturesStore; onClose: () => void }) {
  const toast = useToast()
  const id = useId()
  const choices = store.availableFilters.filter(a => !feature.filters.some(f => f.filterType === a.name))
  const [filterName, setFilterName] = useState(choices.length === 1 ? choices[0].name : '')
  const [params, setParams] = useState(() => choices.length === 1 ? formatJson(choices[0].defaultSettings) : '')
  const [saving, setSaving] = useState(false)
  const [serverError, setServerError] = useState<string | null>(null)

  const selected = choices.find(c => c.name === filterName)
  const paramsValid = params.trim() !== '' && safeParse(params).ok

  const selectFilter = (name: string) => {
    setFilterName(name)
    setServerError(null)
    const filter = choices.find(c => c.name === name)
    setParams(filter ? formatJson(filter.defaultSettings) : '')
  }

  const submit = async (e: FormEvent) => {
    e.preventDefault()
    const parameters = compactJson(params)
    if (!selected || parameters === null || saving) return
    // Read the latest stored copy so filters added meanwhile aren't dropped by the whole-feature PUT. The new
    // filter does not depend on the rest of the feature, so this dialog has no older base to keep; the
    // body and the token just have to come from this one copy, so anything newer on the server conflicts.
    const current = store.storedCopy(feature)

    setSaving(true)
    setServerError(null)
    try {
      await store.updateFeature(current, { filters: [...current.filters, { filterType: selected.name, parameters }] }, current.updatedAt ?? null, 'Failed to add filter')
      toast.success(`${selected.name} filter added to “${feature.name}”`)
      onClose()
    } catch (err) {
      // On a conflict the store now holds the stored copy, so the dialog stays open with the user's
      // filter intact and the next submit is built from that copy.
      setServerError(isConflict(err)
        ? 'Changed elsewhere — the latest version is now loaded; add the filter again to apply it to that version.'
        : errorDetail(err, 'Failed to add filter'))
      setSaving(false)
    }
  }

  return (
    <Dialog
      title="Add filter"
      description={<>Filters decide when <strong>{feature.name}</strong> is on for a given request.</>}
      onClose={onClose}
      busy={saving}
      onSubmit={submit}
      footer={
        <>
          <Button onClick={onClose} disabled={saving}>Cancel</Button>
          <Button type="submit" variant="primary" loading={saving} disabled={!selected || !paramsValid || store.isUpdating(feature.name)}>Add filter</Button>
        </>
      }
    >
      <div className="field">
        <label htmlFor={`${id}-type`} className="field-label">Filter type</label>
        <select id={`${id}-type`} className="select" value={filterName} onChange={e => selectFilter(e.target.value)} disabled={saving} data-autofocus>
          <option value="">Select a filter…</option>
          {choices.map(c => <option key={c.name} value={c.name}>{c.name}</option>)}
        </select>
      </div>
      {selected && (
        <div className="field">
          <JsonEditor
            label="Parameters"
            value={params}
            onChange={v => { setParams(v); setServerError(null) }}
            defaults={selected.defaultSettings}
            serverError={serverError}
            disabled={saving}
            rows={Math.min(14, Math.max(6, params.split('\n').length + 1))}
          />
        </div>
      )}
      {!selected && serverError && <p className="field-error form-error" role="alert">{serverError}</p>}
    </Dialog>
  )
}
