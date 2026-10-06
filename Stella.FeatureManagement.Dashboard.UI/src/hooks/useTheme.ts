import { useCallback, useEffect, useState } from 'react'

export type ThemePreference = 'system' | 'light' | 'dark'

// Same key the inline script in index.html reads, so the first paint already uses the stored theme.
const STORAGE_KEY = 'ff-dashboard-theme'
const ORDER: ThemePreference[] = ['system', 'light', 'dark']

function readStored(): ThemePreference {
  try {
    const value = localStorage.getItem(STORAGE_KEY)
    return value === 'light' || value === 'dark' ? value : 'system'
  } catch {
    return 'system'
  }
}

export function useTheme() {
  const [theme, setTheme] = useState<ThemePreference>(readStored)

  useEffect(() => {
    const root = document.documentElement
    if (theme === 'system') root.removeAttribute('data-theme')
    else root.setAttribute('data-theme', theme)
    try {
      if (theme === 'system') localStorage.removeItem(STORAGE_KEY)
      else localStorage.setItem(STORAGE_KEY, theme)
    } catch {
      // Storage blocked (private mode, sandboxed iframe): the theme just won't persist.
    }
  }, [theme])

  const cycleTheme = useCallback(() => setTheme(t => ORDER[(ORDER.indexOf(t) + 1) % ORDER.length]), [])

  return { theme, cycleTheme }
}
