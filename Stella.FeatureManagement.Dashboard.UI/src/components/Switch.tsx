interface SwitchProps {
  checked: boolean
  onChange: (checked: boolean) => void
  label: string
  disabled?: boolean
  showText?: boolean
}

/** Accessible on/off switch; `label` names what is switched (the visible text shows the state). */
export function Switch({ checked, onChange, label, disabled, showText = true }: SwitchProps) {
  return (
    <button
      type="button"
      role="switch"
      aria-checked={checked}
      aria-label={label}
      className="switch"
      disabled={disabled}
      onClick={() => onChange(!checked)}
    >
      <span className="switch-track" aria-hidden="true">
        <span className="switch-thumb" />
      </span>
      {showText && <span className="switch-text" aria-hidden="true">{checked ? 'Enabled' : 'Disabled'}</span>}
    </button>
  )
}
