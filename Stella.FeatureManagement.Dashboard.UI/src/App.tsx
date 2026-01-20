import { useEffect, useState, useCallback } from 'react'

interface FeatureFilter {
  filterType: string
  parameters: string | null
}

interface FeatureState {
  name: string
  isEnabled: boolean
  description: string | null
  filters: FeatureFilter[]
}

// API base path - use VITE_API_URL if available (Aspire), otherwise fall back to relative path
const API_BASE = import.meta.env.VITE_API_URL 
  ? `${import.meta.env.VITE_API_URL}/features/dashboardapi/features` 
  : '../dashboardapi/features'

export default function App() {
  const [features, setFeatures] = useState<FeatureState[]>([])
  const [loading, setLoading] = useState(true)
  const [updating, setUpdating] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [searchTerm, setSearchTerm] = useState('')
  const [lastUpdated, setLastUpdated] = useState<Date | null>(null)
  const [newFeatureName, setNewFeatureName] = useState('')
  const [creating, setCreating] = useState(false)
  const [showAddModal, setShowAddModal] = useState(false)
  const [deleteTarget, setDeleteTarget] = useState<string | null>(null)
  const [deleting, setDeleting] = useState(false)
  const [expandedFeature, setExpandedFeature] = useState<string | null>(null)
  const [editingFilter, setEditingFilter] = useState<{ featureName: string; filterIndex: number } | null>(null)
  const [editedParams, setEditedParams] = useState('')
  const [jsonError, setJsonError] = useState<string | null>(null)
  const [savingFilter, setSavingFilter] = useState(false)

  const fetchFeatures = useCallback(async () => {
    try {
      setLoading(true)
      setError(null)
      const res = await fetch(API_BASE)
      if (!res.ok) throw new Error(`Failed to fetch features (${res.status})`)
      const data = await res.json()
      setFeatures(data)
      setLastUpdated(new Date())
    } catch (err) {
      setError(err instanceof Error ? err.message : 'An unknown error occurred')
    } finally {
      setLoading(false)
    }
  }, [])

  const toggleFeature = useCallback(async (featureName: string, currentState: boolean) => {
    const newState = !currentState
    setUpdating(featureName)
    
    // Optimistic update
    setFeatures(prev =>
      prev.map(feature =>
        feature.name === featureName
          ? { ...feature, isEnabled: newState }
          : feature
      )
    )

    try {
      const res = await fetch(`${API_BASE}/${featureName}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ isEnabled: newState })
      })

      if (!res.ok) {
        throw new Error(`Failed to update feature (${res.status})`)
      }

      setLastUpdated(new Date())
    } catch (err) {
      // Revert on error
      setFeatures(prev =>
        prev.map(feature =>
          feature.name === featureName
            ? { ...feature, isEnabled: currentState }
            : feature
        )
      )
      setError(err instanceof Error ? err.message : 'Failed to update feature')
    } finally {
      setUpdating(null)
    }
  }, [])

  const createFeature = useCallback(async (e: React.FormEvent) => {
    e.preventDefault()
    const featureName = newFeatureName.trim()
    if (!featureName) return

    setCreating(true)
    setError(null)

    try {
      const res = await fetch(API_BASE, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ name: featureName, isEnabled: false })
      })

      if (res.status === 409) {
        throw new Error(`Feature '${featureName}' already exists`)
      }

      if (!res.ok) {
        throw new Error(`Failed to create feature (${res.status})`)
      }

      const created = await res.json()
      setFeatures(prev => [...prev, created])
      setNewFeatureName('')
      setShowAddModal(false)
      setLastUpdated(new Date())
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create feature')
    } finally {
      setCreating(false)
    }
  }, [newFeatureName])

  const deleteFeature = useCallback(async () => {
    if (!deleteTarget) return

    setDeleting(true)
    setError(null)

    try {
      const res = await fetch(`${API_BASE}/${deleteTarget}`, {
        method: 'DELETE'
      })

      if (!res.ok) {
        throw new Error(`Failed to delete feature (${res.status})`)
      }

      setFeatures(prev => prev.filter(f => f.name !== deleteTarget))
      setDeleteTarget(null)
      setLastUpdated(new Date())
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to delete feature')
    } finally {
      setDeleting(false)
    }
  }, [deleteTarget])

  const startEditingFilter = useCallback((featureName: string, filterIndex: number, currentParams: string | null) => {
    setEditingFilter({ featureName, filterIndex })
    setEditedParams(currentParams ? JSON.stringify(JSON.parse(currentParams), null, 2) : '{}')
    setJsonError(null)
  }, [])

  const cancelEditingFilter = useCallback(() => {
    setEditingFilter(null)
    setEditedParams('')
    setJsonError(null)
  }, [])

  const validateJson = useCallback((value: string): boolean => {
    try {
      JSON.parse(value)
      setJsonError(null)
      return true
    } catch {
      setJsonError('Invalid JSON format')
      return false
    }
  }, [])

  const handleParamsChange = useCallback((value: string) => {
    setEditedParams(value)
    if (value.trim()) {
      validateJson(value)
    } else {
      setJsonError(null)
    }
  }, [validateJson])

  const saveFilterParams = useCallback(async () => {
    if (!editingFilter) return
    if (!validateJson(editedParams)) return

    const feature = features.find(f => f.name === editingFilter.featureName)
    if (!feature) return

    const filter = feature.filters[editingFilter.filterIndex]
    if (!filter) return

    setSavingFilter(true)
    setError(null)

    try {
      const res = await fetch(`${API_BASE}/${feature.name}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          isEnabled: feature.isEnabled,
          description: feature.description,
          filter: {
            filterType: filter.filterType,
            parameters: JSON.stringify(JSON.parse(editedParams))
          }
        })
      })

      if (!res.ok) {
        throw new Error(`Failed to update filter (${res.status})`)
      }

      const updated = await res.json()
      setFeatures(prev =>
        prev.map(f => f.name === feature.name ? updated : f)
      )
      setEditingFilter(null)
      setEditedParams('')
      setLastUpdated(new Date())
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to update filter')
    } finally {
      setSavingFilter(false)
    }
  }, [editingFilter, editedParams, features, validateJson])

  useEffect(() => {
    fetchFeatures()
  }, [fetchFeatures])

  const filteredFeatures = features
    .filter(f => f.name.toLowerCase().includes(searchTerm.toLowerCase()))
    .sort((a, b) => a.name.localeCompare(b.name))

  const enabledCount = features.filter(f => f.isEnabled).length
  const disabledCount = features.length - enabledCount

  return (
    <div className="container">
      <header className="header">
        <div className="header-content">
          <div className="header-title">
            <svg className="header-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
              <path d="M12 2L2 7l10 5 10-5-10-5zM2 17l10 5 10-5M2 12l10 5 10-5" />
            </svg>
            <h1>Feature Management</h1>
          </div>
          <p className="header-subtitle">Monitor and track feature flags in your application</p>
        </div>
        <div className="header-actions">
          <button className="add-btn" onClick={() => setShowAddModal(true)} title="Add new feature">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
              <path d="M12 5v14M5 12h14" />
            </svg>
            <span>Feature</span>
          </button>
          <button className="refresh-btn" onClick={fetchFeatures} disabled={loading} title="Refresh features">
            <svg className={`refresh-icon ${loading ? 'spinning' : ''}`} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
              <path d="M23 4v6h-6M1 20v-6h6M3.51 9a9 9 0 0114.85-3.36L23 10M1 14l4.64 4.36A9 9 0 0020.49 15" />
            </svg>
          </button>
        </div>
      </header>

      {/* Add Feature Modal */}
      {showAddModal && (
        <div className="modal-overlay" onClick={() => setShowAddModal(false)}>
          <div className="modal" onClick={e => e.stopPropagation()}>
            <div className="modal-header">
              <h2>Add New Feature</h2>
              <button className="modal-close" onClick={() => setShowAddModal(false)}>
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                  <path d="M18 6L6 18M6 6l12 12" />
                </svg>
              </button>
            </div>
            <form onSubmit={createFeature}>
              <div className="modal-body">
                <label htmlFor="featureName" className="modal-label">Feature Name</label>
                <input
                  id="featureName"
                  type="text"
                  className="modal-input"
                  placeholder="Enter feature name..."
                  value={newFeatureName}
                  onChange={(e) => setNewFeatureName(e.target.value)}
                  disabled={creating}
                  autoFocus
                />
              </div>
              <div className="modal-footer">
                <button 
                  type="button" 
                  className="modal-btn modal-btn-cancel"
                  onClick={() => setShowAddModal(false)}
                  disabled={creating}
                >
                  Cancel
                </button>
                <button 
                  type="submit" 
                  className="modal-btn modal-btn-primary"
                  disabled={creating || !newFeatureName.trim()}
                >
                  {creating ? <span className="btn-loading"></span> : 'Add Feature'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Stats Cards */}
      <div className="stats-grid">
        <div className="stat-card">
          <div className="stat-value">{features.length}</div>
          <div className="stat-label">Total Features</div>
        </div>
        <div className="stat-card stat-enabled">
          <div className="stat-value">{enabledCount}</div>
          <div className="stat-label">Enabled</div>
        </div>
        <div className="stat-card stat-disabled">
          <div className="stat-value">{disabledCount}</div>
          <div className="stat-label">Disabled</div>
        </div>
      </div>

      {/* Search */}
      <div className="search-container">
        <svg className="search-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
          <circle cx="11" cy="11" r="8" />
          <path d="M21 21l-4.35-4.35" />
        </svg>
        <input
          type="text"
          className="search-input"
          placeholder="Search features..."
          value={searchTerm}
          onChange={(e) => setSearchTerm(e.target.value)}
        />
        {searchTerm && (
          <button className="search-clear" onClick={() => setSearchTerm('')}>
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
              <path d="M18 6L6 18M6 6l12 12" />
            </svg>
          </button>
        )}
      </div>

      <main>
        {error && (
          <div className="error-banner">
            <svg className="error-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
              <circle cx="12" cy="12" r="10" />
              <path d="M12 8v4M12 16h.01" />
            </svg>
            <span>{error}</span>
            <button className="error-retry" onClick={fetchFeatures}>Retry</button>
          </div>
        )}

        {loading && !error && (
          <div className="loading-container">
            <div className="loading-spinner"></div>
            <p>Loading features...</p>
          </div>
        )}

        {!loading && !error && (
          <div className="feature-list">
            {filteredFeatures.length === 0 ? (
              <div className="empty-state">
                <svg className="empty-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5">
                  <path d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2" />
                </svg>
                <p>{searchTerm ? 'No features match your search.' : 'No features configured.'}</p>
              </div>
            ) : (
              filteredFeatures.map(f => (
                <div key={f.name} className={`feature-item ${expandedFeature === f.name ? 'expanded' : ''}`}>
                  <div className="feature-row" onClick={() => setExpandedFeature(expandedFeature === f.name ? null : f.name)}>
                    <button
                      className="delete-btn"
                      onClick={(e) => { e.stopPropagation(); setDeleteTarget(f.name); }}
                      aria-label={`Delete ${f.name}`}
                      title="Delete feature"
                    >
                      <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                        <path d="M3 6h18M19 6v14a2 2 0 01-2 2H7a2 2 0 01-2-2V6m3 0V4a2 2 0 012-2h4a2 2 0 012 2v2M10 11v6M14 11v6" />
                      </svg>
                    </button>
                    <div className="feature-info">
                      <span className="feature-name">
                        {f.name}
                        {f.filters.length > 0 && (
                          <span className="filter-badge" title={`${f.filters.length} filter(s)`}>
                            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                              <path d="M22 3H2l8 9.46V19l4 2v-8.54L22 3z" />
                            </svg>
                            {f.filters.length}
                          </span>
                        )}
                      </span>
                      <span className="feature-description">{f.description}</span>
                    </div>
                    <button
                      className={`toggle-switch ${f.isEnabled ? 'enabled' : 'disabled'}`}
                      onClick={(e) => { e.stopPropagation(); toggleFeature(f.name, f.isEnabled); }}
                      disabled={updating === f.name}
                      aria-label={`Toggle ${f.name}`}
                    >
                      <span className="toggle-track">
                        <span className="toggle-thumb"></span>
                      </span>
                      <span className="toggle-text">{f.isEnabled ? 'Enabled' : 'Disabled'}</span>
                    </button>
                    <svg className={`expand-icon ${expandedFeature === f.name ? 'rotated' : ''}`} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                      <path d="M6 9l6 6 6-6" />
                    </svg>
                  </div>
                  {expandedFeature === f.name && (
                    <div className="feature-details">
                      {f.filters.length === 0 ? (
                        <p className="no-filters">No filters configured for this feature.</p>
                      ) : (
                        <div className="filters-section">
                          {f.filters.map((filter, idx) => {
                            const isEditing = editingFilter?.featureName === f.name && editingFilter?.filterIndex === idx
                            return (
                              <div key={idx} className="filter-item">
                                <div className="filter-header">
                                  <div className="filter-type">{filter.filterType}</div>
                                  {!isEditing && filter.parameters && (
                                    <button
                                      className="edit-filter-btn"
                                      onClick={() => startEditingFilter(f.name, idx, filter.parameters)}
                                      title="Edit parameters"
                                    >
                                      <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                                        <path d="M11 4H4a2 2 0 00-2 2v14a2 2 0 002 2h14a2 2 0 002-2v-7" />
                                        <path d="M18.5 2.5a2.121 2.121 0 013 3L12 15l-4 1 1-4 9.5-9.5z" />
                                      </svg>
                                    </button>
                                  )}
                                </div>
                                {filter.parameters && (
                                  isEditing ? (
                                    <div className="filter-edit">
                                      <textarea
                                        className={`filter-params-input ${jsonError ? 'error' : ''}`}
                                        value={editedParams}
                                        onChange={(e) => handleParamsChange(e.target.value)}
                                        disabled={savingFilter}
                                        rows={6}
                                      />
                                      {jsonError && <span className="json-error">{jsonError}</span>}
                                      <div className="filter-edit-actions">
                                        <button
                                          className="filter-btn filter-btn-cancel"
                                          onClick={cancelEditingFilter}
                                          disabled={savingFilter}
                                        >
                                          Cancel
                                        </button>
                                        <button
                                          className="filter-btn filter-btn-save"
                                          onClick={saveFilterParams}
                                          disabled={savingFilter || !!jsonError}
                                        >
                                          {savingFilter ? <span className="btn-loading"></span> : 'Save'}
                                        </button>
                                      </div>
                                    </div>
                                  ) : (
                                    <pre className="filter-params">{JSON.stringify(JSON.parse(filter.parameters), null, 2)}</pre>
                                  )
                                )}
                              </div>
                            )
                          })}
                        </div>
                      )}
                    </div>
                  )}
                </div>
              ))
            )}
          </div>
        )}
      </main>

      {/* Delete Confirmation Modal */}
      {deleteTarget && (
        <div className="modal-overlay" onClick={() => setDeleteTarget(null)}>
          <div className="modal modal-sm" onClick={e => e.stopPropagation()}>
            <div className="modal-header">
              <h2>Delete Feature</h2>
              <button className="modal-close" onClick={() => setDeleteTarget(null)}>
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                  <path d="M18 6L6 18M6 6l12 12" />
                </svg>
              </button>
            </div>
            <div className="modal-body">
              <p className="delete-confirm-text">
                Are you sure you want to delete <strong>{deleteTarget}</strong>? This action cannot be undone.
              </p>
            </div>
            <div className="modal-footer">
              <button 
                type="button" 
                className="modal-btn modal-btn-cancel"
                onClick={() => setDeleteTarget(null)}
                disabled={deleting}
              >
                Cancel
              </button>
              <button 
                type="button" 
                className="modal-btn modal-btn-danger"
                onClick={deleteFeature}
                disabled={deleting}
              >
                {deleting ? <span className="btn-loading btn-loading-danger"></span> : 'Delete'}
              </button>
            </div>
          </div>
        </div>
      )}

      {lastUpdated && (
        <footer className="footer">
          <span className="footer-updated">Last updated: {lastUpdated.toLocaleTimeString()}</span>
          <div className="footer-brand-container">            
            <a href="https://github.com/rochar/Stella.FeatureManagement.Dashboard" target="_blank" rel="noopener noreferrer" className="footer-brand">Stella.Apps</a>            <span className="footer-version">v{__APP_VERSION__}</span>
          </div>
        </footer>
      )}
    </div>
  )
}
