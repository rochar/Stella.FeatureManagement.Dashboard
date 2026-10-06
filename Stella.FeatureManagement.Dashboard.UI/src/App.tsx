import { useCallback, useId, useState } from 'react'
import { errorMessage, type FeatureState } from './api'
import { AddFilterDialog } from './components/AddFilterDialog'
import { Button, IconButton } from './components/Button'
import { ConfirmDialog } from './components/Dialog'
import { FeatureFormDialog } from './components/FeatureFormDialog'
import { FeatureRow } from './components/FeatureRow'
import { Icon } from './components/Icon'
import { Sidebar } from './components/Sidebar'
import { ToastProvider, useToast } from './components/Toast'
import { useFeatures, SPARK_DAYS } from './hooks/useFeatures'
import { useTheme, type ThemePreference } from './hooks/useTheme'
import { UsageModal } from './UsageChart'

type FeatureDialog =
  | { kind: 'create' }
  | { kind: 'edit'; name: string }
  | { kind: 'delete'; name: string }
  | { kind: 'addFilter'; name: string }
  | { kind: 'deleteFilter'; name: string; index: number; filterType: string }
  | { kind: 'usage'; name: string }

export default function App() {
  return (
    <ToastProvider>
      <Dashboard />
    </ToastProvider>
  )
}

function Dashboard() {
  const store = useFeatures()
  const toast = useToast()
  const { theme, cycleTheme } = useTheme()
  const searchId = useId()
  const [search, setSearch] = useState('')
  const [selectedApplication, setSelectedApplication] = useState<string | null>(null)
  const [expanded, setExpanded] = useState<string | null>(null)
  const [dialog, setDialog] = useState<FeatureDialog | null>(null)
  const [deleting, setDeleting] = useState(false)
  const [deleteError, setDeleteError] = useState<string | null>(null)

  const closeDialog = useCallback(() => { setDialog(null); setDeleteError(null) }, [])
  const findFeature = (name: string) => store.features.find(f => f.name === name)

  const scoped = store.features.filter(f => selectedApplication === null || f.application === selectedApplication)
  const term = search.trim().toLowerCase()
  const visible = scoped
    .filter(f => !term || f.name.toLowerCase().includes(term) || f.description?.toLowerCase().includes(term))
    .sort((a, b) => a.name.localeCompare(b.name))
  const enabledCount = scoped.filter(f => f.isEnabled).length

  const runDelete = async (action: () => Promise<unknown>, success: string, failure: string) => {
    setDeleting(true)
    setDeleteError(null)
    try {
      await action()
      toast.success(success)
      closeDialog()
    } catch (err) {
      setDeleteError(errorMessage(err, failure))
    } finally {
      setDeleting(false)
    }
  }

  const confirmDeleteFeature = (name: string) => runDelete(
    async () => {
      await store.deleteFeature(name)
      if (expanded === name) setExpanded(null)
    },
    `Feature “${name}” deleted`,
    'Failed to delete feature'
  )

  const confirmDeleteFilter = (name: string, index: number, filterType: string) => runDelete(
    async () => {
      const feature = findFeature(name)
      // Don't report success for a removal that never happened.
      if (feature?.filters[index]?.filterType !== filterType) throw new Error('This filter no longer exists. Refresh and try again.')
      await store.updateFeature(feature, { filters: feature.filters.filter((_, i) => i !== index) }, 'Failed to remove filter')
    },
    `${filterType} filter removed from “${name}”`,
    'Failed to remove filter'
  )

  const dialogFeature = dialog && dialog.kind !== 'create' ? findFeature(dialog.name) : undefined

  return (
    <>
      <header className="topbar">
        <div className="topbar-inner">
          <div className="brand">
            <span className="brand-mark"><Icon name="flag" /></span>
            <div className="brand-text">
              <p className="brand-title">Feature Management</p>
              <p className="brand-subtitle">Manage and monitor feature flags</p>
            </div>
          </div>
          <div className="topbar-actions">
            <ThemeButton theme={theme} onClick={cycleTheme} />
            <IconButton
              icon="refresh"
              label="Refresh"
              bordered
              onClick={store.refresh}
              disabled={store.refreshing}
              iconClassName={store.refreshing ? 'spin' : undefined}
            />
            <Button variant="primary" icon="plus" onClick={() => setDialog({ kind: 'create' })}>
              <span>New<span className="btn-label-wide"> feature</span></span>
            </Button>
          </div>
        </div>
      </header>

      <div className="page">
        <Sidebar
          applications={store.applications}
          features={store.features}
          selected={selectedApplication}
          onSelect={setSelectedApplication}
          onAddApplication={name => { store.addDraftApplication(name); setSelectedApplication(name) }}
        />

        <main className="content">
          <div className="page-heading">
            <div>
              <h1 className="page-title">{selectedApplication ?? 'All features'}</h1>
              <p className="page-subtitle">
                {selectedApplication ? `Feature flags of the ${selectedApplication} application` : 'Feature flags across all applications'}
              </p>
            </div>
          </div>

          <section className="stats" aria-label="Summary">
            <Stat label="Total" value={scoped.length} />
            <Stat label="Enabled" value={enabledCount} dot="success" total={scoped.length} />
            <Stat label="Disabled" value={scoped.length - enabledCount} dot="muted" total={scoped.length} />
          </section>

          {store.loadError && (
            <div className="alert" role="alert">
              <Icon name="alert" />
              <span className="alert-text">
                <strong>Couldn't load features</strong>
                {store.loadError}{store.lastUpdated && ' — showing the last loaded data.'}
              </span>
              <Button size="sm" onClick={store.refresh} loading={store.refreshing}>Retry</Button>
            </div>
          )}

          <div className="toolbar">
            <div className="search" role="search">
              <label htmlFor={searchId} className="visually-hidden">Search features</label>
              <Icon name="search" className="search-icon" />
              <input
                id={searchId}
                type="search"
                className="input"
                placeholder="Search by name or description…"
                value={search}
                onChange={e => setSearch(e.target.value)}
                onKeyDown={e => { if (e.key === 'Escape') setSearch('') }}
                autoComplete="off"
              />
              {search && <IconButton icon="x" label="Clear search" onClick={() => setSearch('')} />}
            </div>
            {store.loaded && scoped.length > 0 && (
              <span className="toolbar-count" aria-live="polite">
                {visible.length === scoped.length ? `${scoped.length} feature${scoped.length === 1 ? '' : 's'}` : `${visible.length} of ${scoped.length}`}
              </span>
            )}
          </div>

          <div className="card feature-list">
            {!store.loaded ? (
              <SkeletonRows />
            ) : visible.length === 0 ? (
              store.loadError && store.features.length === 0 ? (
                <EmptyState icon="alert" title="Nothing to show" text="Features will appear here once they load." />
              ) : term ? (
                <EmptyState
                  icon="search"
                  title="No matching features"
                  text={`Nothing matches “${search.trim()}”${selectedApplication ? ` in ${selectedApplication}` : ''}.`}
                  action={<Button size="sm" onClick={() => setSearch('')}>Clear search</Button>}
                />
              ) : (
                <EmptyState
                  icon="flag"
                  title={selectedApplication ? `No features in ${selectedApplication}` : 'No features yet'}
                  text="Create a feature flag to start controlling functionality at runtime."
                  action={<Button variant="primary" size="sm" icon="plus" onClick={() => setDialog({ kind: 'create' })}>Create feature</Button>}
                />
              )
            ) : (
              <>
                <div className="feature-list-header" aria-hidden="true">
                  <span className="col-name">Feature</span>
                  <span className="col-activity">Last activity</span>
                  {store.usage && <span className="col-usage">Usage · {SPARK_DAYS}d</span>}
                  <span className="col-state">State</span>
                  <span className="col-actions" />
                </div>
                <ul className="feature-rows" aria-label="Features">
                  {visible.map(f => (
                    <FeatureRow
                      key={f.name}
                      feature={f}
                      expanded={expanded === f.name}
                      onToggleExpanded={() => setExpanded(expanded === f.name ? null : f.name)}
                      usage={store.usage}
                      store={store}
                      editingName={dialog?.kind === 'edit' ? dialog.name : null}
                      onEdit={() => setDialog({ kind: 'edit', name: f.name })}
                      onDelete={() => setDialog({ kind: 'delete', name: f.name })}
                      onOpenUsage={name => setDialog({ kind: 'usage', name })}
                      onAddFilter={() => setDialog({ kind: 'addFilter', name: f.name })}
                      onDeleteFilter={index => setDialog({ kind: 'deleteFilter', name: f.name, index, filterType: f.filters[index].filterType })}
                    />
                  ))}
                </ul>
              </>
            )}
          </div>
        </main>
      </div>

      <footer className="footer">
        <span>{store.lastUpdated ? `Last updated ${store.lastUpdated.toLocaleTimeString()}` : ' '}</span>
        <span className="footer-brand">
          <a href="https://github.com/rochar/Stella.FeatureManagement.Dashboard" target="_blank" rel="noopener noreferrer">Stella.Apps</a>
          <span>v{__APP_VERSION__}</span>
        </span>
      </footer>

      {dialog?.kind === 'create' && (
        <FeatureFormDialog mode="create" defaultApplication={selectedApplication ?? 'Default'} store={store} onClose={closeDialog} />
      )}
      {dialog?.kind === 'edit' && dialogFeature && (
        <FeatureFormDialog mode="edit" feature={dialogFeature} store={store} onClose={closeDialog} />
      )}
      {dialog?.kind === 'addFilter' && dialogFeature && (
        <AddFilterDialog feature={dialogFeature} store={store} onClose={closeDialog} />
      )}
      {dialog?.kind === 'delete' && (
        <ConfirmDialog
          title="Delete feature?"
          confirmLabel="Delete feature"
          busy={deleting}
          error={deleteError}
          onConfirm={() => confirmDeleteFeature(dialog.name)}
          onClose={closeDialog}
        >
          <strong>{dialog.name}</strong> and its filters will be permanently deleted. Code that checks this flag will treat it as disabled.
        </ConfirmDialog>
      )}
      {dialog?.kind === 'deleteFilter' && (
        <ConfirmDialog
          title="Remove filter?"
          confirmLabel="Remove filter"
          busy={deleting}
          error={deleteError}
          onConfirm={() => confirmDeleteFilter(dialog.name, dialog.index, dialog.filterType)}
          onClose={closeDialog}
        >
          The <strong>{dialog.filterType}</strong> filter will be removed from <strong>{dialog.name}</strong>.
        </ConfirmDialog>
      )}
      {dialog?.kind === 'usage' && (
        <UsageModal featureName={dialog.name} onClose={closeDialog} />
      )}
    </>
  )
}

function Stat({ label, value, dot, total }: { label: string; value: number; dot?: 'success' | 'muted'; total?: number }) {
  return (
    <div className="card stat">
      <div className="stat-label">
        {dot && <span className={`stat-dot stat-dot-${dot}`} aria-hidden="true" />}
        {label}
      </div>
      <div className="stat-value">{value}</div>
      {total !== undefined && total > 0 && <div className="stat-meta">{Math.round((value / total) * 100)}% of total</div>}
    </div>
  )
}

function EmptyState({ icon, title, text, action }: { icon: 'alert' | 'search' | 'flag'; title: string; text: string; action?: React.ReactNode }) {
  return (
    <div className="empty-state">
      <span className="empty-state-icon"><Icon name={icon} /></span>
      <p className="empty-state-title">{title}</p>
      <p className="empty-state-text">{text}</p>
      {action}
    </div>
  )
}

function SkeletonRows() {
  return (
    <div role="status" aria-label="Loading features">
      {Array.from({ length: 5 }, (_, i) => (
        <div key={i} className="skeleton-row" aria-hidden="true">
          <span className="skeleton" style={{ width: 16, height: 16 }} />
          <span className="skeleton-lines">
            <span className="skeleton" style={{ width: `${40 + ((i * 17) % 30)}%`, height: 12 }} />
            <span className="skeleton" style={{ width: `${55 + ((i * 11) % 25)}%`, height: 10 }} />
          </span>
          <span className="skeleton" style={{ width: 88, height: 20, borderRadius: 999 }} />
        </div>
      ))}
    </div>
  )
}

const THEME_LABEL: Record<ThemePreference, string> = { system: 'System', light: 'Light', dark: 'Dark' }
const THEME_ICON = { system: 'monitor', light: 'sun', dark: 'moon' } as const

function ThemeButton({ theme, onClick }: { theme: ThemePreference; onClick: () => void }) {
  return (
    <IconButton
      icon={THEME_ICON[theme]}
      label={`Theme: ${THEME_LABEL[theme]} (click to change)`}
      bordered
      onClick={onClick}
    />
  )
}
