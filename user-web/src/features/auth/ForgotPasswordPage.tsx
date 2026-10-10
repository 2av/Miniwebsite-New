import { useState } from 'react'
import { Link } from 'react-router-dom'
import { forgotPasswordApi, resetPasswordApi } from '@/features/auth/api'
import { ApiError } from '@/shared/api/client'
import { LOGIN_PATH } from '@/shared/auth/session'
import { useSiteStyles } from '@/shared/site/useSiteStyles'

type Step = 'email' | 'reset' | 'done'

export function ForgotPasswordPage() {
  useSiteStyles()

  const [step, setStep] = useState<Step>('email')
  const [email, setEmail] = useState('')
  const [token, setToken] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [message, setMessage] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  async function sendCode(e: React.FormEvent) {
    e.preventDefault()
    setError(null)
    setMessage(null)
    const em = email.trim().toLowerCase()
    if (!em || !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(em)) {
      setError('Please enter a valid email address.')
      return
    }
    setBusy(true)
    try {
      const res = await forgotPasswordApi(em)
      setMessage(res.message || 'If the email exists, a reset code has been sent.')
      setStep('reset')
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Unable to send reset code.')
    } finally {
      setBusy(false)
    }
  }

  async function resetPwd(e: React.FormEvent) {
    e.preventDefault()
    setError(null)
    setMessage(null)
    if (!token.trim()) {
      setError('Please enter the reset code from your email.')
      return
    }
    if (newPassword.length < 6) {
      setError('Password must be at least 6 characters.')
      return
    }
    if (newPassword !== confirmPassword) {
      setError('Passwords do not match.')
      return
    }
    setBusy(true)
    try {
      const res = await resetPasswordApi(email.trim().toLowerCase(), token.trim(), newPassword)
      setMessage(res.message || 'Password reset successfully.')
      setStep('done')
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Unable to reset password.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="login-wrap">
      <div className="login-container">
        <h2 className="heading">
          <Link to={LOGIN_PATH}>
            <i className="fa fa-angle-left" aria-hidden="true" />
          </Link>
          Reset Password
        </h2>
        <p className="text-white">Enter your registered email to reset your password.</p>

        {step === 'email' && (
          <form onSubmit={sendCode} autoComplete="off">
            <div className="mb-4">
              <label htmlFor="fp_email">Email</label>
              <input
                id="fp_email"
                type="email"
                className="form-control"
                placeholder="Email address*"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                required
              />
            </div>
            <input
              type="submit"
              className="btn btn-login"
              value={busy ? 'SENDING...' : 'SEND RESET CODE'}
              disabled={busy}
            />
          </form>
        )}

        {step === 'reset' && (
          <form onSubmit={resetPwd} autoComplete="off">
            <div className="mb-3">
              <label htmlFor="fp_token">Reset code</label>
              <input
                id="fp_token"
                type="text"
                className="form-control"
                placeholder="Code from email*"
                value={token}
                onChange={(e) => setToken(e.target.value)}
                required
              />
            </div>
            <div className="mb-3">
              <label htmlFor="fp_new">New password</label>
              <input
                id="fp_new"
                type="password"
                className="form-control"
                placeholder="New password*"
                value={newPassword}
                onChange={(e) => setNewPassword(e.target.value)}
                required
              />
            </div>
            <div className="mb-4">
              <label htmlFor="fp_confirm">Confirm password</label>
              <input
                id="fp_confirm"
                type="password"
                className="form-control"
                placeholder="Confirm password*"
                value={confirmPassword}
                onChange={(e) => setConfirmPassword(e.target.value)}
                required
              />
            </div>
            <input
              type="submit"
              className="btn btn-login"
              value={busy ? 'SAVING...' : 'RESET PASSWORD'}
              disabled={busy}
            />
          </form>
        )}

        {step === 'done' && (
          <div className="Reset-foot">
            <p>Your password has been updated. You can now log in.</p>
            <Link to={LOGIN_PATH} className="btn btn-login" style={{ display: 'inline-block', textAlign: 'center' }}>
              Back to Login
            </Link>
          </div>
        )}

        {message && <div className="alert success error-msg" style={{ marginTop: 16 }}>{message}</div>}
        {error && <div className="alert danger error-msg" style={{ marginTop: 16 }}>{error}</div>}

        <div className="CreateAccount-wrap">
          <Link to={LOGIN_PATH} className="create-account">
            Back to Login
          </Link>
        </div>
      </div>
    </div>
  )
}
