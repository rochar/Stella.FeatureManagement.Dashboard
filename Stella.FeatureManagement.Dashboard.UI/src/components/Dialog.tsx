import { useEffect, useId, useRef, type FormEvent, type KeyboardEvent, type ReactNode } from 'react'
import { createPortal } from 'react-dom'
import { Button, IconButton } from './Button'
import { Icon } from './Icon'

const FOCUSABLE = 'a[href], button:not([disabled]), input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])'

interface DialogProps {
  title: string
  description?: ReactNode
  onClose: () => void
  /** While busy (a request is in flight) Escape, the overlay and the close button do nothing. */
  busy?: boolean
  size?: 'sm' | 'md' | 'lg'
  /** When set, body + footer are wrapped in a <form> so Enter submits. */
  onSubmit?: (e: FormEvent) => void
  footer?: ReactNode
  children: ReactNode
}

/**
 * Modal dialog: labelled for assistive tech, traps Tab inside, closes on Escape/overlay,
 * focuses the first `[data-autofocus]` (else first focusable) and restores focus on close.
 */
export function Dialog({ title, description, onClose, busy = false, size = 'md', onSubmit, footer, children }: DialogProps) {
  const titleId = useId()
  const descriptionId = useId()
  const dialogRef = useRef<HTMLDivElement>(null)
  const busyRef = useRef(busy)
  busyRef.current = busy

  useEffect(() => {
    const previouslyFocused = document.activeElement as HTMLElement | null
    const dialog = dialogRef.current
    const target = dialog?.querySelector<HTMLElement>('[data-autofocus]')
      ?? dialog?.querySelector<HTMLElement>(`.dialog-body ${FOCUSABLE}`)
      ?? dialog
    target?.focus()

    const { overflow } = document.body.style
    document.body.style.overflow = 'hidden'
    return () => {
      document.body.style.overflow = overflow
      previouslyFocused?.focus?.()
    }
  }, [])

  const requestClose = () => { if (!busyRef.current) onClose() }

  const onKeyDown = (e: KeyboardEvent) => {
    if (e.key === 'Escape') {
      e.stopPropagation()
      requestClose()
      return
    }
    if (e.key !== 'Tab' || !dialogRef.current) return
    const focusable = Array.from(dialogRef.current.querySelectorAll<HTMLElement>(FOCUSABLE))
    if (focusable.length === 0) { e.preventDefault(); return }
    const first = focusable[0]
    const last = focusable[focusable.length - 1]
    if (e.shiftKey && (document.activeElement === first || document.activeElement === dialogRef.current)) {
      e.preventDefault()
      last.focus()
    } else if (!e.shiftKey && document.activeElement === last) {
      e.preventDefault()
      first.focus()
    }
  }

  const content = (
    <>
      <div className="dialog-body">{children}</div>
      {footer && <div className="dialog-footer">{footer}</div>}
    </>
  )

  return createPortal(
    <div className="dialog-overlay" onMouseDown={(e) => { if (e.target === e.currentTarget) requestClose() }} onKeyDown={onKeyDown}>
      <div
        ref={dialogRef}
        className={`dialog ${size !== 'md' ? `dialog-${size}` : ''}`}
        role="dialog"
        aria-modal="true"
        aria-labelledby={titleId}
        aria-describedby={description ? descriptionId : undefined}
        tabIndex={-1}
      >
        <div className="dialog-header">
          <div>
            <h2 id={titleId} className="dialog-title">{title}</h2>
            {description && <p id={descriptionId} className="dialog-description">{description}</p>}
          </div>
          <IconButton icon="x" label="Close" onClick={requestClose} disabled={busy} />
        </div>
        {onSubmit ? <form className="dialog-form" onSubmit={onSubmit} noValidate>{content}</form> : content}
      </div>
    </div>,
    document.body
  )
}

interface ConfirmDialogProps {
  title: string
  children: ReactNode
  confirmLabel: string
  busy: boolean
  error?: string | null
  onConfirm: () => void
  onClose: () => void
}

/** Destructive confirmation; focus starts on Cancel so Enter never deletes by accident. */
export function ConfirmDialog({ title, children, confirmLabel, busy, error, onConfirm, onClose }: ConfirmDialogProps) {
  return (
    <Dialog
      title={title}
      size="sm"
      busy={busy}
      onClose={onClose}
      footer={
        <>
          <Button onClick={onClose} disabled={busy} data-autofocus>Cancel</Button>
          <Button variant="danger" onClick={onConfirm} loading={busy}>{confirmLabel}</Button>
        </>
      }
    >
      <div className="confirm-body">
        <span className="confirm-icon"><Icon name="warning" /></span>
        <div>
          <p className="confirm-text">{children}</p>
          {error && <p className="field-error confirm-error" role="alert"><Icon name="alert" />{error}</p>}
        </div>
      </div>
    </Dialog>
  )
}
