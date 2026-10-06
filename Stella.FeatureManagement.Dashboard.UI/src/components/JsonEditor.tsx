import { useId } from 'react'
import { formatJson, safeParse } from '../json'
import { Button } from './Button'
import { Icon } from './Icon'

interface JsonEditorProps {
  label: string
  value: string
  onChange: (value: string) => void
  /** Default settings of the filter type; enables "Reset to defaults". */
  defaults?: string | null
  /** Server-side validation message from the last save attempt. */
  serverError?: string | null
  disabled?: boolean
  rows?: number
  autoFocus?: boolean
}

/** Monospace JSON field with live syntax checking, Format and Reset to defaults. */
export function JsonEditor({ label, value, onChange, defaults, serverError, disabled, rows = 8, autoFocus }: JsonEditorProps) {
  const id = useId()
  const parsed = value.trim() ? safeParse(value) : null
  const syntaxError = parsed && !parsed.ok ? parsed.error : null
  const error = syntaxError ?? serverError ?? null
  // Reuse the parse above instead of parsing the same text again on every keystroke.
  const formatted = parsed?.ok ? JSON.stringify(parsed.value, null, 2) : formatJson(value)
  const formattedDefaults = defaults != null ? formatJson(defaults) : null

  return (
    <div className="json-editor">
      <div className="json-editor-toolbar">
        <label htmlFor={id} className="field-label">{label}</label>
        <div className="json-editor-tools">
          <Button size="sm" variant="ghost" icon="braces" onClick={() => onChange(formatted)} disabled={disabled || !!syntaxError || formatted === value}>
            Format
          </Button>
          {formattedDefaults !== null && (
            <Button size="sm" variant="ghost" icon="rotateCcw" onClick={() => onChange(formattedDefaults)} disabled={disabled || formattedDefaults === value}>
              Reset to defaults
            </Button>
          )}
        </div>
      </div>
      <textarea
        id={id}
        className="textarea textarea-code"
        value={value}
        onChange={e => onChange(e.target.value)}
        disabled={disabled}
        rows={rows}
        spellCheck={false}
        autoCapitalize="off"
        autoComplete="off"
        aria-invalid={!!error}
        aria-describedby={`${id}-status`}
        autoFocus={autoFocus}
        data-autofocus={autoFocus || undefined}
      />
      <div id={`${id}-status`} aria-live="polite">
        {error ? (
          <p className="field-error"><Icon name="alert" />{syntaxError ? `Invalid JSON: ${syntaxError}` : error}</p>
        ) : parsed?.ok ? (
          <p className="json-status"><Icon name="check" />Valid JSON</p>
        ) : null}
      </div>
    </div>
  )
}
