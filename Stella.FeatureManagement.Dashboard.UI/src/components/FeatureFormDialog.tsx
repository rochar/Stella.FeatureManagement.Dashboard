import { useId, useState, type FormEvent } from 'react'
import { errorDetail, type FeatureState } from '../api'
import type { FeaturesStore } from '../hooks/useFeatures'
import { Button } from './Button'
import { Dialog } from './Dialog'
import { Icon } from './Icon'
import { Switch } from './Switch'
import { useToast } from './Toast'

type Props =
  | { mode: 'create'; defaultApplication: string; store: FeaturesStore; onClose: () => void }
  | { mode: 'edit'; feature: FeatureState; store: FeaturesStore; onClose: () => void }

/** Create and edit share one form; the name is only editable on create (it is the feature's key). */
export function FeatureFormDialog(props: Props) {
  const { store, onClose } = props
  const toast = useToast()
  const id = useId()
  const editing = props.mode === 'edit' ? props.feature : null

  const [name, setName] = useState(editing?.name ?? '')
  const [application, setApplication] = useState(editing?.application ?? (props.mode === 'create' ? props.defaultApplication : 'Default'))
  const [description, setDescription] = useState(editing?.description ?? '')
  const [isEnabled, setIsEnabled] = useState(editing?.isEnabled ?? false)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)

  // Always read the live copy: a toggle in the list may have changed it since the dialog opened.
  const current = editing ? store.features.find(f => f.name === editing.name) ?? editing : null
  const trimmedDescription = description.trim() || null
  const unchanged = !!current
    && trimmedDescription === (current.description?.trim() || null)
    && application === current.application
    && isEnabled === current.isEnabled
  const toggleInFlight = !!current && store.isUpdating(current.name)

  const applications = store.applications.includes(application) ? store.applications : [...store.applications, application]

  const submit = async (e: FormEvent) => {
    e.preventDefault()
    if (saving) return
    setError(null)

    if (!current) {
      const trimmedName = name.trim()
      if (!trimmedName) { setError('Enter a feature name.'); return }
      setSaving(true)
      try {
        await store.createFeature({ name: trimmedName, isEnabled: false, description: trimmedDescription, application })
        toast.success(`Feature “${trimmedName}” created`)
        onClose()
      } catch (err) {
        setError(errorDetail(err, 'Failed to create feature'))
        setSaving(false)
      }
      return
    }

    // Unchanged: skip the PUT, which would rewrite the filters, bump UpdatedAt and run OnFeatureChanging.
    // A toggle PUT for the same feature in flight: both replace the whole feature, so wait for it.
    if (unchanged) { onClose(); return }
    if (toggleInFlight) return
    setSaving(true)
    try {
      await store.updateFeature(current, { application, description: trimmedDescription, isEnabled }, 'Failed to update feature')
      toast.success(`Changes to “${current.name}” saved`)
      onClose()
    } catch (err) {
      setError(errorDetail(err, 'Failed to update feature'))
      setSaving(false)
    }
  }

  return (
    <Dialog
      title={current ? 'Edit feature' : 'New feature'}
      description={current ? <>Update the settings of <strong>{current.name}</strong>.</> : 'New features start disabled. Add filters after creating it.'}
      onClose={onClose}
      busy={saving}
      onSubmit={submit}
      footer={
        <>
          <Button onClick={onClose} disabled={saving}>Cancel</Button>
          <Button
            type="submit"
            variant="primary"
            loading={saving}
            disabled={current ? unchanged || toggleInFlight : !name.trim()}
          >
            {current ? 'Save changes' : 'Create feature'}
          </Button>
        </>
      }
    >
      {!current && (
        <div className="field">
          <label htmlFor={`${id}-name`} className="field-label">Name</label>
          <input
            id={`${id}-name`}
            className="input"
            placeholder="e.g. NewCheckoutFlow"
            value={name}
            onChange={e => setName(e.target.value)}
            disabled={saving}
            autoComplete="off"
            spellCheck={false}
            data-autofocus
          />
          <span className="field-hint">The key your code passes to the feature manager. It can't be changed later.</span>
        </div>
      )}
      <div className="field">
        <label htmlFor={`${id}-app`} className="field-label">Application</label>
        <select id={`${id}-app`} className="select" value={application} onChange={e => setApplication(e.target.value)} disabled={saving}>
          {applications.map(app => <option key={app} value={app}>{app}</option>)}
        </select>
      </div>
      <div className="field">
        <label htmlFor={`${id}-description`} className="field-label">
          Description <span className="field-optional">(optional)</span>
        </label>
        <input
          id={`${id}-description`}
          className="input"
          placeholder="What does this feature control?"
          value={description}
          onChange={e => setDescription(e.target.value)}
          disabled={saving}
          data-autofocus={current ? true : undefined}
        />
      </div>
      {current && (
        <div className="field">
          <span className="field-label" id={`${id}-state`}>State</span>
          <div>
            <Switch checked={isEnabled} onChange={setIsEnabled} label={`${current.name} enabled`} disabled={saving} />
          </div>
        </div>
      )}
      {error && (
        <p className="field-error form-error" role="alert"><Icon name="alert" />{error}</p>
      )}
    </Dialog>
  )
}
