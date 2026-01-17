import { useEffect, useState, useCallback } from 'react'

interface FeatureState {
  featureName: string
  isEnabled: boolean
}

// API base path - use VITE_API_URL if available (Aspire), otherwise fall back to relative path
const API_BASE = import.meta.env.VITE_API_URL 
  ? `${import.meta.env.VITE_API_URL}/features` 
  : '../'

export default function App() {
  const [features, setFeatures] = useState<FeatureState[]>([])
  const [loading, setLoading] = useState(true)
  const [updating, setUpdating] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [searchTerm, setSearchTerm] = useState('')
  const [lastUpdated, setLastUpdated] = useState<Date | null>(null)

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
        feature.featureName === featureName
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
          feature.featureName === featureName
            ? { ...feature, isEnabled: currentState }
            : feature
        )
      )
      setError(err instanceof Error ? err.message : 'Failed to update feature')
    } finally {
      setUpdating(null)
    }
  }, [])

  useEffect(() => {
    fetchFeatures()
  }, [fetchFeatures])

  const filteredFeatures = features
    .filter(f => f.featureName.toLowerCase().includes(searchTerm.toLowerCase()))
    .sort((a, b) => a.featureName.localeCompare(b.featureName))

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
        <button className="refresh-btn" onClick={fetchFeatures} disabled={loading} title="Refresh features">
          <svg className={`refresh-icon ${loading ? 'spinning' : ''}`} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
            <path d="M23 4v6h-6M1 20v-6h6M3.51 9a9 9 0 0114.85-3.36L23 10M1 14l4.64 4.36A9 9 0 0020.49 15" />
          </svg>
        </button>
      </header>

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
                <div key={f.featureName} className="feature-item">
                  <div className="feature-info">
                    <span className="feature-name">{f.featureName}</span>
                  </div>
                  <button
                    className={`toggle-switch ${f.isEnabled ? 'enabled' : 'disabled'}`}
                    onClick={() => toggleFeature(f.featureName, f.isEnabled)}
                    disabled={updating === f.featureName}
                    aria-label={`Toggle ${f.featureName}`}
                  >
                    <span className="toggle-track">
                      <span className="toggle-thumb"></span>
                    </span>
                    <span className="toggle-text">{f.isEnabled ? 'Enabled' : 'Disabled'}</span>
                  </button>
                </div>
              ))
            )}
          </div>
        )}
      </main>

      {lastUpdated && (
        <footer className="footer">
          Last updated: {lastUpdated.toLocaleTimeString()}
        </footer>
      )}
    </div>
  )
}
