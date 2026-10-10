import { useEffect, useState } from 'react'
import { Link, Navigate, useNavigate, useSearchParams } from 'react-router-dom'
import { loginApi } from '@/features/auth/api'
import { ApiError } from '@/shared/api/client'
import { setAuthSession, getAuthSession, dashboardPathForRole, isSessionValid, clearAuthSession } from '@/shared/auth/session'
import type { UserRole } from '@/shared/types/api'
import { assetUrl } from '@/lib/utils'

const fieldClass =
  'h-11 w-full rounded-xl border border-[#D1D5DB] bg-white px-4 text-[15px] text-sb-ink outline-none transition placeholder:text-sb-muted focus:border-sb-primary focus:ring-4 focus:ring-sb-primary/15'

export function LoginPage() {
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
        roleName: auth.roleName || undefined,
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
    <main className="relative flex min-h-dvh items-center justify-center overflow-hidden bg-sb-bg px-4 py-10">
      <div className="pointer-events-none absolute -top-24 -left-16 h-72 w-72 rounded-full bg-sb-primary/20 blur-3xl" />
      <div className="pointer-events-none absolute top-10 right-0 h-64 w-64 rounded-full bg-sb-pink/20 blur-3xl" />
      <div className="pointer-events-none absolute bottom-0 left-1/3 h-56 w-56 rounded-full bg-sb-orange/20 blur-3xl" />

      <div className="relative w-full max-w-[440px] rounded-xl border border-sb-border bg-white px-6 py-8 shadow-[0_12px_32px_rgb(0_0_20/0.08)] sm:px-10 sm:py-10">
        <Link to="/" className="mb-8 inline-flex" aria-label="ShareBizz home">
          <img src={assetUrl('images/main-logo.png')} alt="ShareBizz" className="h-10 w-auto" />
        </Link>

        <div className="mb-6 flex items-center gap-2">
          <Link to="/" className="inline-flex h-9 w-9 items-center justify-center rounded-lg text-sb-ink hover:bg-sb-active" aria-label="Back to home">
            <i className="fa fa-angle-left text-2xl" aria-hidden="true" />
          </Link>
          <h1 className="text-[32px] leading-tight font-bold text-sb-ink">Login</h1>
        </div>
        <p className="mb-6 text-[15px] text-sb-muted">Please enter your details</p>

        <form onSubmit={onSubmit} autoComplete="off" id="login" className="space-y-4">
          <div>
            <label htmlFor="user_id" className="mb-1.5 block text-sm font-medium text-sb-ink">
              Email or mobile
            </label>
            <input
              type="text"
              className={fieldClass}
              name="user_id"
              id="user_id"
              placeholder="Email ID or 10-digit mobile"
              value={userId}
              onChange={(e) => setUserId(e.target.value)}
              required
            />
          </div>
          <div>
            <label htmlFor="user_password" className="mb-1.5 block text-sm font-medium text-sb-ink">
              Password
            </label>
            <div className="relative">
              <input
                type={showPassword ? 'text' : 'password'}
                className={`${fieldClass} pr-12`}
                name="user_password"
                id="user_password"
                placeholder="Enter password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                required
              />
              <button
                type="button"
                className="absolute top-1/2 right-3 inline-flex h-8 w-8 -translate-y-1/2 items-center justify-center text-sb-icon hover:text-sb-primary"
                onClick={() => setShowPassword((v) => !v)}
                title={showPassword ? 'Hide Password' : 'Show Password'}
                aria-label={showPassword ? 'Hide Password' : 'Show Password'}
              >
                <i className={`fa ${showPassword ? 'fa-eye' : 'fa-eye-slash'}`} aria-hidden="true" />
              </button>
            </div>
            <div className="mt-2 text-right">
              <Link to="/login/forgot-password" className="text-sm font-medium text-sb-primary hover:underline">
                Forgot Password?
              </Link>
            </div>
          </div>

          <button type="submit" className="sb-btn-primary mt-2 w-full text-[15px] disabled:cursor-not-allowed disabled:opacity-60" disabled={submitting}>
            {submitting ? 'Logging in...' : 'Log in'}
          </button>
        </form>

        {success && (
          <div className="mt-4 rounded-xl bg-[#ECFDF5] px-4 py-3 text-sm font-medium text-[#067647]" role="status">
            Login successful. Redirecting...
          </div>
        )}
        {error && (
          <div className="mt-4 rounded-xl bg-[#FEF2F2] px-4 py-3 text-sm font-medium text-[#be185d]" role="alert">
            {error}
          </div>
        )}

        <p className="mt-6 text-center text-[15px] text-sb-muted">
          Don&apos;t have an account?{' '}
          <Link to="/registration" className="font-semibold text-sb-primary hover:underline">
            Create Account
          </Link>
        </p>
      </div>
    </main>
  )
}
