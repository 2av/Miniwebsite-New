import type { InputHTMLAttributes, ReactNode, SelectHTMLAttributes, TextareaHTMLAttributes } from 'react'
import { Link } from 'react-router-dom'

type FieldProps = {
  label: string
  required?: boolean
  htmlFor?: string
  wide?: boolean
  large?: boolean
  children: ReactNode
}

export function MwField({ label, required, htmlFor, wide, large, children }: FieldProps) {
  return (
    <div className={`mw-form-group${wide ? ' mw-form-span' : ''}`}>
      <label className={`mw-label${large ? ' mw-label-lg' : ''}`} htmlFor={htmlFor}>
        {label}
        {required ? <span className="req">*</span> : null}
      </label>
      {children}
    </div>
  )
}

export function MwInput({ className, ...props }: InputHTMLAttributes<HTMLInputElement>) {
  return <input className={['form-control', 'mw-input', className].filter(Boolean).join(' ')} {...props} />
}

export function MwTextarea({ className, ...props }: TextareaHTMLAttributes<HTMLTextAreaElement>) {
  return <textarea className={['form-control', 'mw-input', className].filter(Boolean).join(' ')} {...props} />
}

export function MwSelect({ className, children, ...props }: SelectHTMLAttributes<HTMLSelectElement>) {
  return (
    <select className={['form-control', 'mw-input', className].filter(Boolean).join(' ')} {...props}>
      {children}
    </select>
  )
}

export function MwAlert({ tone, children }: { tone: 'danger' | 'success'; children: ReactNode }) {
  const icon = tone === 'danger' ? 'fa-exclamation-circle' : 'fa-check-circle'
  return (
    <div className={`alert alert-${tone} mw-alert mw-alert-${tone}`} role="alert">
      <i className={`fa ${icon} mw-alert-icon`} aria-hidden />
      <div className="mw-alert-body">{children}</div>
    </div>
  )
}

export function MwStepActions({
  back,
  next,
  nextLabel = 'Next',
  saveLabel,
  busyLabel = 'Saving...',
  busy,
  disabled,
  submit = true,
  onSave,
}: {
  back?: string
  next?: string
  nextLabel?: string
  saveLabel?: string
  busyLabel?: string
  busy?: boolean
  disabled?: boolean
  submit?: boolean
  onSave?: () => void
}) {
  return (
    <div className="Product-ServicesBtn mw-btn-row d-flex flex-wrap justify-content-between align-items-center gap-2 mt-4">
      {back ? (
        <Link to={back} className="mw-btn mw-btn-back">
          <span className="mw-btn-angle">
            <i className="fa fa-angle-left" aria-hidden />
          </span>
          <span>Back</span>
        </Link>
      ) : (
        <span aria-hidden />
      )}
      {saveLabel ? (
        <button
          type={submit ? 'submit' : 'button'}
          className="mw-btn mw-btn-save"
          disabled={busy || disabled}
          onClick={submit ? undefined : onSave}
        >
          <span>{busy ? busyLabel : saveLabel}</span>
        </button>
      ) : (
        <span aria-hidden />
      )}
      {next ? (
        <Link to={next} className="mw-btn mw-btn-next">
          <span>{nextLabel}</span>
          <span className="mw-btn-angle">
            <i className="fa fa-angle-right" aria-hidden />
          </span>
        </Link>
      ) : (
        <span aria-hidden />
      )}
    </div>
  )
}
