import { createContext, useCallback, useContext, useEffect, useMemo, useRef, useState, type ReactNode } from 'react'
import { IconButton } from './Button'
import { Icon } from './Icon'

type ToastTone = 'success' | 'error'

interface ToastItem {
  id: number
  tone: ToastTone
  message: string
}

interface ToastApi {
  success: (message: string) => void
  error: (message: string) => void
}

const ToastContext = createContext<ToastApi | null>(null)

const SUCCESS_TIMEOUT = 4000
const MAX_TOASTS = 4

export function ToastProvider({ children }: { children: ReactNode }) {
  const [toasts, setToasts] = useState<ToastItem[]>([])
  const nextId = useRef(0)

  const dismiss = useCallback((id: number) => setToasts(prev => prev.filter(t => t.id !== id)), [])

  const push = useCallback((tone: ToastTone, message: string) => {
    const id = ++nextId.current
    setToasts(prev => [...prev.slice(-(MAX_TOASTS - 1)), { id, tone, message }])
  }, [])

  const api = useMemo<ToastApi>(() => ({
    success: message => push('success', message),
    error: message => push('error', message),
  }), [push])

  return (
    <ToastContext.Provider value={api}>
      {children}
      {/* Two live regions: errors interrupt (assertive), confirmations wait their turn (polite). */}
      <div className="toast-region">
        <div role="status" aria-live="polite" className="toast-stack">
          {toasts.filter(t => t.tone === 'success').map(t => <Toast key={t.id} toast={t} onDismiss={dismiss} />)}
        </div>
        <div role="alert" aria-live="assertive" className="toast-stack">
          {toasts.filter(t => t.tone === 'error').map(t => <Toast key={t.id} toast={t} onDismiss={dismiss} />)}
        </div>
      </div>
    </ToastContext.Provider>
  )
}

// Success toasts dismiss themselves; errors stay until dismissed so they can be read.
function Toast({ toast, onDismiss }: { toast: ToastItem; onDismiss: (id: number) => void }) {
  useEffect(() => {
    if (toast.tone !== 'success') return
    const timer = window.setTimeout(() => onDismiss(toast.id), SUCCESS_TIMEOUT)
    return () => window.clearTimeout(timer)
  }, [toast, onDismiss])

  return (
    <div className={`toast toast-${toast.tone}`}>
      <Icon name={toast.tone === 'success' ? 'checkCircle' : 'alert'} className="toast-icon" />
      <p className="toast-message">{toast.message}</p>
      <IconButton icon="x" label="Dismiss notification" onClick={() => onDismiss(toast.id)} />
    </div>
  )
}

export function useToast(): ToastApi {
  const api = useContext(ToastContext)
  if (!api) throw new Error('useToast must be used inside <ToastProvider>')
  return api
}
