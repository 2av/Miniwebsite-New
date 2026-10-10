import { useEffect, useState } from 'react'
import { Link, Navigate, useNavigate, useSearchParams } from 'react-router-dom'
import { loginApi } from '@/features/auth/api'
import { ApiError } from '@/shared/api/client'
import { setAuthSession, getAuthSession, dashboardPathForRole, isSessionValid, clearAuthSession } from '@/shared/auth/session'
import type { UserRole } from '@/shared/types/api'
import { useSiteStyles } from '@/shared/site/useSiteStyles'

export function LoginPage() {
  useSiteStyles()
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const existing = getAuthSession()

  const [userId, setUserId] = useState('')
  const [password, setPassword] = useState('')
  const [showPassword, setShowPassword] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)
  const [success, setSuccess] = useState(false)

  useEffect(() => {
    if (searchParams.get('session') === 'expired') {
      setError('Your session has expired. Please log in again.')
    }
    const existingSession = getAuthSession()
    if (existingSession && !isSessionValid(existingSession)) {
      clearAuthSession()
    }
  }, [searchParams])

  if (existing && isSessionValid(existing) && !success) {
    return <Navigate to={dashboardPathForRole(existing.role)} replace />
  }

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError(null)

    const id = userId.trim()
    const pwd = password.trim()
    if (!id || !pwd) {
      setError('Please enter both Email/Mobile and Password.')
      return
    }
    const isEmail = /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(id)
    const isPhone = /^[0-9]{10}$/.test(id)
    if (!isEmail && !isPhone) {
      setError('Enter a valid Email ID or 10-digit Mobile Number.')
      return
    }

    setSubmitting(true)
    try {
      const auth = await loginApi(id, pwd)
      setAuthSession({
        token: auth.accessToken,
        refreshToken: auth.refreshToken,
        role: (auth.role as UserRole) || 'CUSTOMER',
        email: auth.email,
        name: auth.name,
        userId: auth.userId,
        collaborationEnabled: auth.collaborationEnabled === 'YES',
        saleskitEnabled: auth.saleskitEnabled === 'YES',
        accessTokenExpiresAt: auth.accessTokenExpiresAt || undefined,
      })
      setSuccess(true)
      window.setTimeout(() => {
        navigate(dashboardPathForRole(auth.role), { replace: true })
      }, 1200)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Login failed. Please try again.')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="login-wrap">
      <div className="login-container">
        <h2 className="login-heading">
          <Link to="/" className="login-heading-back" aria-label="Back to home">
            <i className="fa fa-angle-left" aria-hidden="true" />
          </Link>
          <span className="login-heading-text">Login</span>
        </h2>
        <p className="text-white">Please enter your details</p>

        <form onSubmit={onSubmit} autoComplete="off" id="login">
          <div className="mb-4">
            <input
              type="text"
              className="form-control"
              name="user_id"
              id="user_id"
              placeholder="Enter Email ID or Mobile Number*"
              value={userId}
              onChange={(e) => setUserId(e.target.value)}
              required
            />
          </div>
          <div className="mb-5 position-relative">
            <div className="password-field mb-2">
              <input
                type={showPassword ? 'text' : 'password'}
                className="form-control"
                name="user_password"
                id="user_password"
                placeholder="Password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                required
              />
              <button
                type="button"
                className="password-toggle"
                onClick={() => setShowPassword((v) => !v)}
                title={showPassword ? 'Hide Password' : 'Show Password'}
                aria-label={showPassword ? 'Hide Password' : 'Show Password'}
              >
                <i className={`fa ${showPassword ? 'fa-eye' : 'fa-eye-slash'}`} aria-hidden="true" />
              </button>
            </div>
            <Link to="/login/forgot-password" className="forgot-password">
              Forgot Password?
            </Link>
          </div>
          <input
            type="submit"
            value={submitting ? 'LOGGING IN...' : 'LOG IN'}
            className="btn btn-login"
            disabled={submitting}
          />
        </form>

        {success && (
          <div className="alert success" style={{ color: '#fff' }}>
            Login Successful! Redirecting...
          </div>
        )}
        {error && <div className="alert danger error-msg">{error}</div>}

        <div className="CreateAccount-wrap">
          <span className="text-white">
            Don&apos;t have an account? &nbsp;
            <Link to="/registration" className="create-account">
              Create Account
            </Link>
          </span>
        </div>
      </div>
    </div>
  )
}
