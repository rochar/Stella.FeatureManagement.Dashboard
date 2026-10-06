import { useId, useState, type FormEvent } from 'react'
import type { FeatureState } from '../api'
import { Button, IconButton } from './Button'

interface SidebarProps {
  applications: string[]
  features: FeatureState[]
  selected: string | null
  onSelect: (application: string | null) => void
  onAddApplication: (name: string) => void
}

export function Sidebar({ applications, features, selected, onSelect, onAddApplication }: SidebarProps) {
  const id = useId()
  const [adding, setAdding] = useState(false)
  const [name, setName] = useState('')
  const trimmed = name.trim()
  const exists = applications.some(a => a.toLowerCase() === trimmed.toLowerCase())

  const close = () => { setAdding(false); setName('') }

  const submit = (e: FormEvent) => {
    e.preventDefault()
    if (!trimmed || exists) return
    onAddApplication(trimmed)
    close()
  }

  return (
    <nav className="sidebar" aria-labelledby={`${id}-heading`}>
      <h2 id={`${id}-heading`} className="sidebar-heading">Applications</h2>
      <ul className="sidebar-list">
        <li>
          <SidebarItem label="All applications" count={features.length} active={selected === null} onClick={() => onSelect(null)} />
        </li>
        {applications.map(app => (
          <li key={app}>
            <SidebarItem label={app} count={features.filter(f => f.application === app).length} active={selected === app} onClick={() => onSelect(app)} />
          </li>
        ))}
      </ul>
      <div className="sidebar-add">
        {adding ? (
          <form className="sidebar-add-form" onSubmit={submit} onKeyDown={e => { if (e.key === 'Escape') close() }}>
            <label htmlFor={`${id}-name`} className="field-label">New application</label>
            <div className="sidebar-add-row">
              <input
                id={`${id}-name`}
                className="input"
                placeholder="e.g. Checkout"
                value={name}
                onChange={e => setName(e.target.value)}
                aria-invalid={exists || undefined}
                aria-describedby={`${id}-hint`}
                autoFocus
              />
              <IconButton type="submit" icon="check" label="Add application" disabled={!trimmed || exists} />
              <IconButton icon="x" label="Cancel" onClick={close} />
            </div>
            <span id={`${id}-hint`} className={exists ? 'field-error' : 'field-hint'}>
              {exists ? 'An application with this name already exists.' : 'Kept once a feature is created in it.'}
            </span>
          </form>
        ) : (
          <Button size="sm" variant="ghost" icon="plus" onClick={() => setAdding(true)}>Add application</Button>
        )}
      </div>
    </nav>
  )
}

function SidebarItem({ label, count, active, onClick }: { label: string; count: number; active: boolean; onClick: () => void }) {
  return (
    <button type="button" className="sidebar-item" aria-current={active ? 'true' : undefined} onClick={onClick}>
      <span className="sidebar-item-name" title={label}>{label}</span>
      <span className="sidebar-item-count">{count}</span>
    </button>
  )
}
