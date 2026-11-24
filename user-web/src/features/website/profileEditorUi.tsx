import { createContext, createElement, useCallback, useContext, useEffect, useRef, useState, type ChangeEvent, type CSSProperties, type KeyboardEvent, type MutableRefObject, type ReactNode } from 'react'
import { createPortal } from 'react-dom'
import { WEEK_DAYS, parseSchedule, serializeSchedule } from '@/features/website/profileFieldRules'

const iconButtonStyle: CSSProperties = {
  width: 28,
  height: 28,
  borderRadius: 999,
  border: 0,
  padding: 0,
  cursor: 'pointer',
  display: 'inline-flex',
  alignItems: 'center',
  justifyContent: 'center',
  boxShadow: '0 2px 8px rgba(0,0,0,0.25)',
}

export function MiniIcon({
  label,
  icon,
  onClick,
  tone = 'dark',
  spin = false,
}: {
  label: string
  icon: string
  onClick: () => void
  tone?: 'dark' | 'danger' | 'save'
  spin?: boolean
}) {
  return (
    <button
      type="button"
      aria-label={label}
      title={label}
      onClick={(e) => {
        e.preventDefault()
        e.stopPropagation()
        onClick()
      }}
      style={{
        ...iconButtonStyle,
        background: tone === 'danger' ? '#b42318' : tone === 'save' ? '#e2c27a' : '#111827',
        color: tone === 'save' ? '#2c1a08' : '#fff',
      }}
    >
      <i className={`fas ${icon}${spin ? ' fa-spin' : ''}`} style={{ fontSize: 11, lineHeight: 1 }} />
    </button>
  )
}

const DraftContext = createContext<((id: string, dirty: boolean, commit: () => Promise<void>) => void) | null>(null)

export function EditSaveBar({
  enabled,
  editing,
  editLabel,
  onEdit,
  children,
}: {
  enabled: boolean
  editing: boolean
  editLabel: string
  onEdit: () => void
  children: ReactNode
}) {
  const [dirty, setDirty] = useState(false)
  const [saving, setSaving] = useState(false)
  const saveRef = useRef<(() => Promise<void>) | null>(null)
  if (!enabled) return <>{children}</>
  return (
    <DraftScope onDirty={setDirty} saveRef={saveRef}>
      <span style={{ position: 'absolute', top: 12, right: 12, zIndex: 5, display: 'flex', gap: 6 }}>
        <MiniIcon label={editLabel} icon="fa-pen" onClick={onEdit} />
        {editing && dirty ? (
          <MiniIcon
            label={saving ? 'Saving' : 'Save'}
            icon={saving ? 'fa-spinner' : 'fa-save'}
            spin={saving}
            tone="save"
            onClick={() => {
              if (saving) return
              setSaving(true)
              void Promise.resolve(saveRef.current?.()).finally(() => setSaving(false))
            }}
          />
        ) : null}
      </span>
      {children}
    </DraftScope>
  )
}

function DraftScope({
  children,
  onDirty,
  saveRef,
}: {
  children: ReactNode
  onDirty: (dirty: boolean) => void
  saveRef: MutableRefObject<(() => Promise<void>) | null>
}) {
  const entries = useRef(new Map<string, { dirty: boolean; commit: () => Promise<void> }>())
  const onDirtyRef = useRef(onDirty)
  onDirtyRef.current = onDirty
  const report = useCallback((id: string, dirty: boolean, commit: () => Promise<void>) => {
    entries.current.set(id, { dirty, commit })
    onDirtyRef.current([...entries.current.values()].some((item) => item.dirty))
  }, [])
  useEffect(() => {
    saveRef.current = async () => {
      const pending = [...entries.current.values()].filter((item) => item.dirty)
      for (const item of pending) await item.commit()
    }
    return () => {
      saveRef.current = null
    }
  }, [saveRef])
  return createElement(DraftContext.Provider, { value: report }, children)
}

export function InlineText({
  value,
  placeholder,
  onSave,
  multiline = false,
  block = false,
  className,
  draftId,
  validate,
  normalize,
  maxLength,
}: {
  value: string
  placeholder: string
  onSave: (next: string) => Promise<void>
  multiline?: boolean
  block?: boolean
  className?: string
  draftId?: string
  validate?: (value: string) => string | null
  normalize?: (value: string) => string
  maxLength?: number
}) {
  const [draft, setDraft] = useState(value)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const cancelRef = useRef(false)
  const report = useContext(DraftContext)
  const manualSave = Boolean(report && draftId)

  useEffect(() => {
    setDraft(value)
  }, [value])

  async function commit() {
    if (cancelRef.current) {
      cancelRef.current = false
      return
    }
    const problem = validate?.(draft) ?? null
    if (problem) {
      setError(problem)
      return
    }
    if (draft === value) {
      setError(null)
      return
    }
    setBusy(true)
    setError(null)
    try {
      await onSave(draft)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Save failed')
    } finally {
      setBusy(false)
    }
  }

  const commitRef = useRef(commit)
  commitRef.current = commit

  useEffect(() => {
    if (!report || !draftId) return
    report(draftId, draft !== value, () => commitRef.current())
  }, [report, draftId, draft, value])

  useEffect(() => {
    if (!report || !draftId) return
    return () => report(draftId, false, async () => {})
  }, [report, draftId])

  function onKeyDown(e: KeyboardEvent) {
    if (e.key === 'Escape') {
      e.preventDefault()
      cancelRef.current = true
      setDraft(value)
      setError(null)
      ;(e.target as HTMLInputElement).blur()
    } else if (e.key === 'Enter' && !multiline) {
      e.preventDefault()
      ;(e.target as HTMLInputElement).blur()
    }
  }

  const fieldStyle: CSSProperties = {
    font: 'inherit',
    color: 'inherit',
    background: 'rgba(127,127,127,0.12)',
    border: '1px solid rgba(127,127,127,0.45)',
    borderRadius: 8,
    padding: '4px 8px',
    width: block ? '100%' : '9em',
    maxWidth: '100%',
  }

  const fieldProps = {
    value: draft,
    disabled: busy,
    className,
    style: fieldStyle,
    'aria-label': placeholder,
    placeholder,
    onChange: (e: ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
      const next = normalize ? normalize(e.target.value) : e.target.value
      setDraft(maxLength ? next.slice(0, maxLength) : next)
    },
    onBlur: () => {
      if (!manualSave) void commit()
    },
    onKeyDown,
  }

  return (
    <span style={{ display: 'block', maxWidth: '100%' }}>
      {multiline ? <textarea {...fieldProps} rows={4} maxLength={maxLength} /> : <input {...fieldProps} maxLength={maxLength} />}
      {error ? <span style={{ color: '#b42318', fontSize: 12, display: 'block', marginTop: 4 }}>{error}</span> : null}
    </span>
  )
}

export function HoursField({
  rows,
  raw,
  active,
  onSave,
  draftId,
}: {
  rows: { days: string; hours: string }[]
  raw: string
  active: boolean
  onSave: (next: string) => Promise<void>
  draftId?: string
}) {
  const [draft, setDraft] = useState(raw)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const cancelRef = useRef(false)
  const report = useContext(DraftContext)

  useEffect(() => {
    setDraft(raw)
  }, [raw])

  async function commit() {
    if (cancelRef.current) {
      cancelRef.current = false
      return
    }
    if (draft === raw) return
    setBusy(true)
    setError(null)
    try {
      await onSave(draft)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Save failed')
    } finally {
      setBusy(false)
    }
  }

  const commitRef = useRef(commit)
  commitRef.current = commit

  useEffect(() => {
    if (!report || !draftId) return
    report(draftId, draft !== raw, () => commitRef.current())
  }, [report, draftId, draft, raw])

  useEffect(() => {
    if (!report || !draftId) return
    return () => report(draftId, false, async () => {})
  }, [report, draftId])

  if (!active) {
    return (
      <div className="space-y-3 text-sm md:text-base border-t border-b border-white/10 py-6">
        {rows.length === 0 ? (
          <p className="text-textmain">Add business hours</p>
        ) : (
          rows.map((bh, i) => (
            <div key={i} className="flex justify-between items-center gap-4">
              <span className="text-textmain">{bh.days}</span>
              <span className="text-heading font-medium">{bh.hours}</span>
            </div>
          ))
        )}
      </div>
    )
  }

  const schedule = parseSchedule(draft.trim() ? draft : rows.map((row) => `${row.days}: ${row.hours}`).join('\n'))
  const updateDay = (day: string, patch: Partial<{ off: boolean; open: string; close: string }>) => {
    const next = schedule.map((row) => (row.day === day ? { ...row, ...patch } : row))
    setDraft(serializeSchedule(next))
  }

  return (
    <div className="mw-hours-editor">
      <style>{HOURS_EDITOR_CSS}</style>
      <div className="mw-hours-heading">Business Hour's Settings</div>
      <div className="mw-hours-sub">Working Days &amp; Timings</div>
      <div className="mw-hours-table">
        <div className="mw-hours-row mw-hours-head">
          <span>Day</span>
          <span>Open</span>
          <span>Close</span>
        </div>
        {WEEK_DAYS.map((day) => {
          const row = schedule.find((item) => item.day === day) ?? { day, off: false, open: '10:30', close: '20:00' }
          return (
            <div key={day} className="mw-hours-row">
              <label className="mw-hours-day">
                <input
                  type="checkbox"
                  checked={!row.off}
                  disabled={busy}
                  aria-label={`${day} is open`}
                  onChange={(e) => updateDay(day, { off: !e.target.checked })}
                />
                <span>{day}</span>
              </label>
              {row.off ? (
                <>
                  <input className="mw-hours-time" value="Closed" disabled aria-label={`${day} opening time`} />
                  <input className="mw-hours-time" value="Closed" disabled aria-label={`${day} closing time`} />
                </>
              ) : (
                <>
                  <input
                    className="mw-hours-time"
                    type="time"
                    value={/^\d{2}:\d{2}$/.test(row.open) ? row.open : '10:30'}
                    disabled={busy}
                    aria-label={`${day} opening time`}
                    onChange={(e) => updateDay(day, { open: e.target.value || '10:30' })}
                  />
                  <input
                    className="mw-hours-time"
                    type="time"
                    value={/^\d{2}:\d{2}$/.test(row.close) ? row.close : '20:00'}
                    disabled={busy}
                    aria-label={`${day} closing time`}
                    onChange={(e) => updateDay(day, { close: e.target.value || '20:00' })}
                  />
                </>
              )}
            </div>
          )
        })}
      </div>
      {error ? <p className="mw-hours-error">{error}</p> : null}
    </div>
  )
}

const HOURS_EDITOR_CSS = `
.mw-hours-editor {
  background: #fff;
  color: #1f2937;
  color-scheme: light;
  border: 1px solid #e5e7eb;
  border-radius: 16px;
  padding: 22px 22px 16px;
  box-shadow: 0 8px 24px rgba(15, 23, 42, 0.06);
  font-family: Poppins, Inter, sans-serif;
}
.mw-hours-heading { font-size: 18px; font-weight: 700; color: #111827; }
.mw-hours-sub { margin: 4px 0 16px; color: #6b7280; font-size: 14px; }
.mw-hours-table { display: flex; flex-direction: column; gap: 8px; }
.mw-hours-row {
  display: grid;
  grid-template-columns: minmax(140px, 1.2fr) 1fr 1fr;
  gap: 16px;
  align-items: center;
}
.mw-hours-head {
  background: #f3f4f6;
  color: #6b7280;
  font-size: 14px;
  font-weight: 500;
  border-radius: 10px;
  min-height: 42px;
  padding: 0 14px;
}
.mw-hours-day {
  display: flex;
  align-items: center;
  gap: 12px;
  color: #374151;
  font-size: 15px;
  margin: 0;
}
.mw-hours-editor input[type="checkbox"] {
  width: 18px !important;
  height: 18px !important;
  margin: 0;
  accent-color: #2563eb;
  flex: 0 0 auto;
}
.mw-hours-editor input.mw-hours-time {
  width: 100% !important;
  height: 42px;
  border: 1px solid #e5e7eb !important;
  border-radius: 10px !important;
  background: #fff !important;
  color: #111827 !important;
  padding: 0 12px !important;
  font-size: 14px !important;
  font-family: inherit !important;
  box-shadow: none !important;
}
.mw-hours-editor input.mw-hours-time:disabled {
  background: #f3f4f6 !important;
  color: #9ca3af !important;
}
.mw-hours-error { color: #b42318; font-size: 12px; margin: 8px 0 0; }
`

const EDITOR_CSS = `
.mw-profile-editor-ui, .mw-profile-editor-ui * { box-sizing: border-box; font-family: Poppins, Inter, sans-serif; }
.mw-editor-sheet {
  display: flex;
  flex-direction: column;
  width: min(600px, 100%);
  max-height: min(88vh, 820px);
  background: #fff;
  color: #1f2937;
  border: 1px solid #e5e7eb;
  border-radius: 16px;
  overflow: hidden;
  box-shadow: 0 24px 64px rgba(0, 0, 20, 0.22);
}
.mw-editor-accent { height: 4px; background: linear-gradient(90deg, #6C2EFF, #FF38BD, #FF8A00); }
.mw-editor-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  padding: 16px 18px;
  border-bottom: 1px solid #e5e7eb;
  background: #fff;
}
.mw-editor-title { font-size: 18px; font-weight: 650; line-height: 1.3; color: #1f2937; }
.mw-editor-close {
  width: 36px;
  height: 36px;
  flex: 0 0 auto;
  display: grid;
  place-items: center;
  border-radius: 10px;
  border: 1px solid #e5e7eb;
  background: #fff;
  color: #1f2937;
  cursor: pointer;
  padding: 0;
}
.mw-editor-close:hover { background: #f3f0ff; border-color: #d9ccff; color: #6C2EFF; }
.mw-editor-close:disabled { opacity: 0.5; cursor: default; }
.mw-editor-body { flex: 1; overflow: auto; padding: 18px 18px 16px; }
.mw-editor-field { display: block; margin-bottom: 14px; }
.mw-editor-label { display: block; margin-bottom: 6px; font-size: 13px; font-weight: 600; color: #1f2937; }
.mw-profile-editor-ui input:not([type="checkbox"]):not([type="radio"]):not([type="file"]),
.mw-profile-editor-ui textarea,
.mw-profile-editor-ui select {
  width: 100%;
  height: 44px;
  color: #1f2937 !important;
  background: #fff !important;
  border: 1px solid #d1d5db !important;
  border-radius: 12px;
  padding: 0 12px;
  font-size: 14px !important;
  font-family: inherit !important;
  box-shadow: none;
}
.mw-profile-editor-ui textarea {
  height: auto;
  min-height: 112px;
  padding: 12px;
  line-height: 1.5;
  resize: vertical;
}
.mw-profile-editor-ui input[type="file"] {
  width: 100%;
  height: auto;
  padding: 10px 12px;
  border: 1px dashed #d1d5db !important;
  border-radius: 12px;
  background: #f8fafc !important;
  color: #1f2937 !important;
  font-size: 13px !important;
  font-family: inherit !important;
}
.mw-profile-editor-ui input:not([type="checkbox"]):not([type="radio"]):not([type="file"]):focus,
.mw-profile-editor-ui textarea:focus,
.mw-profile-editor-ui select:focus {
  outline: none;
  border-color: #6C2EFF !important;
  box-shadow: 0 0 0 3px rgba(108, 46, 255, 0.16);
}
.mw-profile-editor-ui input[type="checkbox"], .mw-profile-editor-ui input[type="radio"] {
  width: 18px;
  height: 18px;
  margin: 0;
  padding: 0;
  accent-color: #6C2EFF;
  flex: 0 0 auto;
}
.mw-editor-actions {
  display: flex;
  justify-content: flex-end;
  align-items: center;
  gap: 10px;
  position: sticky;
  bottom: 0;
  margin: 16px -18px -16px;
  padding: 14px 18px;
  border-top: 1px solid #e5e7eb;
  background: #f8fafc;
  z-index: 1;
}
.mw-profile-editor-ui button.mw-editor-primary {
  background: linear-gradient(90deg, #6C2EFF, #FF38BD 55%, #FF8A00);
  color: #fff;
  border: 0;
  border-radius: 12px;
  min-height: 44px;
  padding: 0 20px;
  font-weight: 600;
  font-size: 14px;
  cursor: pointer;
}
.mw-profile-editor-ui button.mw-editor-primary:disabled { opacity: 0.6; cursor: default; }
.mw-profile-editor-ui button.mw-editor-ghost {
  background: #fff;
  color: #6C2EFF;
  border: 1px solid #6C2EFF;
  border-radius: 12px;
  min-height: 44px;
  padding: 0 16px;
  font-weight: 600;
  font-size: 14px;
  cursor: pointer;
}
.mw-profile-editor-ui button.mw-editor-ghost:disabled { opacity: 0.6; cursor: default; }
.mw-profile-editor-ui button[role="switch"] { border: 0; padding: 3px; }
`

export function EditorDialog({
  title,
  onClose,
  busy = false,
  children,
}: {
  title: string
  onClose: () => void
  busy?: boolean
  children: ReactNode
}) {
  const skipBackdropClose = useRef(false)
  useEffect(() => {
    const onKey = (event: KeyboardEvent) => {
      if (event.key === 'Escape' && !busy) onClose()
    }
    const previousOverflow = document.body.style.overflow
    document.body.style.overflow = 'hidden'
    window.addEventListener('keydown', onKey)
    return () => {
      document.body.style.overflow = previousOverflow
      window.removeEventListener('keydown', onKey)
    }
  }, [busy, onClose])

  useEffect(() => {
    const markFilePick = (event: Event) => {
      const target = event.target
      const node = target instanceof Element ? target : null
      const fileInput = node?.closest('label')?.querySelector('input[type="file"]')
      if ((target instanceof HTMLInputElement && target.type === 'file') || fileInput) skipBackdropClose.current = true
    }
    const releaseFilePick = () => {
      window.setTimeout(() => {
        skipBackdropClose.current = false
      }, 600)
    }
    document.addEventListener('pointerdown', markFilePick, true)
    window.addEventListener('focus', releaseFilePick)
    return () => {
      document.removeEventListener('pointerdown', markFilePick, true)
      window.removeEventListener('focus', releaseFilePick)
    }
  }, [])

  return createPortal(
    <div
      className="mw-profile-editor-ui"
      style={{
        position: 'fixed',
        inset: 0,
        zIndex: 80,
        background: 'rgba(0, 0, 128, 0.45)',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        padding: 16,
      }}
      onMouseDown={(event) => {
        if (busy || event.target !== event.currentTarget || skipBackdropClose.current) return
        onClose()
      }}
    >
      <div className="mw-editor-sheet" role="dialog" aria-modal="true" aria-label={title} onMouseDown={(e) => e.stopPropagation()}>
        <div className="mw-editor-accent" />
        <div className="mw-editor-head">
          <div className="mw-editor-title">{title}</div>
          <button type="button" className="mw-editor-close" aria-label="Close" onClick={onClose} disabled={busy}>
            <svg width="14" height="14" viewBox="0 0 14 14" aria-hidden="true">
              <path d="M2 2l10 10M12 2L2 12" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" />
            </svg>
          </button>
        </div>
        <div className="mw-editor-body">{children}</div>
        <style>{EDITOR_CSS}</style>
      </div>
    </div>,
    document.body,
  )
}

export function EditorField({ label, children }: { label: string; children: ReactNode }) {
  return (
    <label className="mw-editor-field">
      <span className="mw-editor-label">{label}</span>
      {children}
    </label>
  )
}

export function EditorActions({ children }: { children: ReactNode }) {
  return <div className="mw-editor-actions">{children}</div>
}
