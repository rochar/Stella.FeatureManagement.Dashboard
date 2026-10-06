import type { ButtonHTMLAttributes, ReactNode } from 'react'
import { Icon, type IconName } from './Icon'

type Variant = 'primary' | 'secondary' | 'ghost' | 'danger' | 'dashed'

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: Variant
  size?: 'sm' | 'md'
  icon?: IconName
  loading?: boolean
  children: ReactNode
}

export function Button({ variant = 'secondary', size = 'md', icon, loading, disabled, className, children, type = 'button', ...rest }: ButtonProps) {
  const classes = ['btn', `btn-${variant}`, size === 'sm' && 'btn-sm', className].filter(Boolean).join(' ')
  return (
    <button type={type} className={classes} disabled={disabled || loading} aria-busy={loading || undefined} {...rest}>
      {loading ? <span className="spinner" aria-hidden="true" /> : icon && <Icon name={icon} />}
      {children}
    </button>
  )
}

interface IconButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  icon: IconName
  label: string
  tone?: 'default' | 'danger'
  bordered?: boolean
  iconClassName?: string
}

/** Icon-only button: `label` is its accessible name and tooltip. */
export function IconButton({ icon, label, tone = 'default', bordered, iconClassName, className, type = 'button', ...rest }: IconButtonProps) {
  const classes = ['icon-btn', tone === 'danger' && 'icon-btn-danger', bordered && 'icon-btn-bordered', className].filter(Boolean).join(' ')
  return (
    <button type={type} className={classes} aria-label={label} title={label} {...rest}>
      <Icon name={icon} className={iconClassName} />
    </button>
  )
}
