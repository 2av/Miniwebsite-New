import { useState } from 'react'
import { apiSend, ApiError } from '@/shared/api/client'
import { PortalPageHeader } from '@/components/layout/PortalPageHeader'
import { useAuthSession } from '@/shared/auth/useAuthSession'

type FieldKey = 'current' | 'next' | 'confirm'

export function ChangePasswordPage() {
  const session = useAuthSession()
  const [currentPassword, setCurrent] = useState('')
  const [newPassword, setNew] = useState('')
  const [confirmPassword, setConfirm] = useState('')
  const [show, setShow] = useState<Record<FieldKey, boolean>>({
    current: false,
    next: false,
    confirm: false,
  })
  const [msg, setMsg] = useState<string | null>(null)
  const [err, setErr] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  function toggle(key: FieldKey) {
    setShow((s) => ({ ...s, [key]: !s[key] }))
  }

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault()
    setMsg(null)
    setErr(null)

    if (newPassword.length < 6) {
      setErr('Password must be at least 6 characters long')
      return
    }
    if (newPassword !== confirmPassword) {
      setErr('New password and confirm password do not match')
      return
    }

    setBusy(true)
    try {
      const res = await apiSend('post', '/api/v1/auth/change-password', {
        email: session?.email,
        currentPassword,
        newPassword,
        confirmPassword,
      })
      setMsg(res.message || 'Password changed successfully!')
      setCurrent('')
      setNew('')
      setConfirm('')
    } catch (e) {
      setErr(e instanceof ApiError ? e.message : 'Failed to change password')
    } finally {
      setBusy(false)
    }
  }

  return (
    <>
      <PortalPageHeader
        title="Change Password"
        subtitle="Update your account password for better security"
      />

      <div className="mw-card mb-4">
        <div className="mw-card-body">
          <div className="mx-auto w-full max-w-xl">
            <div className="change-password-container rounded-[10px] bg-white px-6 py-8 shadow-sm sm:px-10">
              {msg ? (
                <div className="mw-alert mw-alert-success mb-4" role="alert">
                  <i className="fa fa-check-circle mw-alert-icon" aria-hidden />
                  <div className="mw-alert-body">{msg}</div>
                </div>
              ) : null}
              {err ? (
                <div className="mw-alert mw-alert-danger mb-4" role="alert">
                  <i className="fa fa-exclamation-circle mw-alert-icon" aria-hidden />
                  <div className="mw-alert-body">{err}</div>
                </div>
              ) : null}

              <form id="changePasswordForm" className="mw-form space-y-6" onSubmit={onSubmit} noValidate>
                <div className="mw-form-group">
                  <label className="mb-2 block text-base font-bold text-slate-900" htmlFor="current_password">
                    Current Password
                  </label>
                  <div className="relative">
                    <input
                      type={show.current ? 'text' : 'password'}
                      id="current_password"
                      name="current_password"
                      className="form-control mw-input !rounded-lg !border-slate-200 !px-4 !py-3 !text-base focus:!border-[#ffbe17] focus:!shadow-[0_0_0_3px_rgba(255,190,23,0.25)]"
                      value={currentPassword}
                      onChange={(e) => setCurrent(e.target.value)}
                      required
                      autoComplete="current-password"
                    />
                    <button
                      type="button"
                      className="absolute top-1/2 right-3 -translate-y-1/2 border-0 bg-transparent p-1 text-slate-400 hover:text-slate-600"
                      aria-label={show.current ? 'Hide password' : 'Show password'}
                      onClick={() => toggle('current')}
                    >
                      <i className={`fa ${show.current ? 'fa-eye-slash' : 'fa-eye'}`} aria-hidden />
                    </button>
                  </div>
                </div>

                <div className="mw-form-group">
                  <label className="mb-2 block text-base font-bold text-slate-900" htmlFor="new_password">
                    New Password
                  </label>
                  <div className="relative">
                    <input
                      type={show.next ? 'text' : 'password'}
                      id="new_password"
                      name="new_password"
                      className="form-control mw-input !rounded-lg !border-slate-200 !px-4 !py-3 !text-base focus:!border-[#ffbe17] focus:!shadow-[0_0_0_3px_rgba(255,190,23,0.25)]"
                      value={newPassword}
                      onChange={(e) => setNew(e.target.value)}
                      required
                      minLength={6}
                      autoComplete="new-password"
                    />
                    <button
                      type="button"
                      className="absolute top-1/2 right-3 -translate-y-1/2 border-0 bg-transparent p-1 text-slate-400 hover:text-slate-600"
                      aria-label={show.next ? 'Hide password' : 'Show password'}
                      onClick={() => toggle('next')}
                    >
                      <i className={`fa ${show.next ? 'fa-eye-slash' : 'fa-eye'}`} aria-hidden />
                    </button>
                  </div>
                  <p className="mt-1.5 mb-0 text-xs text-slate-500">Password must be at least 6 characters long</p>
                </div>

                <div className="mw-form-group">
                  <label className="mb-2 block text-base font-bold text-slate-900" htmlFor="confirm_password">
                    Confirm Password
                  </label>
                  <div className="relative">
                    <input
                      type={show.confirm ? 'text' : 'password'}
                      id="confirm_password"
                      name="confirm_password"
                      className="form-control mw-input !rounded-lg !border-slate-200 !px-4 !py-3 !text-base focus:!border-[#ffbe17] focus:!shadow-[0_0_0_3px_rgba(255,190,23,0.25)]"
                      value={confirmPassword}
                      onChange={(e) => setConfirm(e.target.value)}
                      required
                      minLength={6}
                      autoComplete="new-password"
                    />
                    <button
                      type="button"
                      className="absolute top-1/2 right-3 -translate-y-1/2 border-0 bg-transparent p-1 text-slate-400 hover:text-slate-600"
                      aria-label={show.confirm ? 'Hide password' : 'Show password'}
                      onClick={() => toggle('confirm')}
                    >
                      <i className={`fa ${show.confirm ? 'fa-eye-slash' : 'fa-eye'}`} aria-hidden />
                    </button>
                  </div>
                </div>

                <button
                  type="submit"
                  id="submitBtn"
                  className="mw-btn mw-btn-accent mt-2 w-full !rounded-lg !border-0 !bg-[#ffbe17] !px-5 !py-3.5 !text-base !font-bold !text-slate-900 hover:!bg-[#f0b000] disabled:!bg-slate-300 disabled:!text-slate-600"
                  disabled={busy}
                >
                  {busy ? (
                    <>
                      <i className="fa fa-spinner fa-spin me-2" aria-hidden />
                      Updating…
                    </>
                  ) : (
                    'Update Password'
                  )}
                </button>
              </form>
            </div>
          </div>
        </div>
      </div>
    </>
  )
}
