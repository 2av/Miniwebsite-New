import { useEffect, useState } from 'react'
import { Link, Navigate } from 'react-router-dom'
import { LOGIN_PATH } from '@/shared/auth/session'
import { useSiteStyles } from '@/shared/site/useSiteStyles'

export function RegistrationSuccessPage() {
  useSiteStyles()
  const loginPath = LOGIN_PATH
  const [seconds, setSeconds] = useState(3)
  const raw = sessionStorage.getItem('mw_registration_success')
  const ok = Boolean(raw)

  useEffect(() => {
    if (!ok) return
    const t = window.setInterval(() => setSeconds((s) => s - 1), 1000)
    return () => window.clearInterval(t)
  }, [ok])

  useEffect(() => {
    if (ok && seconds <= 0) {
      sessionStorage.removeItem('mw_registration_success')
      window.location.href = loginPath
    }
  }, [ok, seconds, loginPath])

  if (!ok) return <Navigate to={loginPath} replace />

  return (
    <div className="login-wrap">
      <div className="login-container" style={{ textAlign: 'center' }}>
        <h2 className="heading">Registration Successful</h2>
        <p className="text-white">Your account has been created successfully.</p>
        <p className="text-white">Redirecting to login in {Math.max(seconds, 0)}s...</p>
        <Link to={loginPath} className="btn btn-login" style={{ display: 'inline-block', marginTop: 16 }}>
          Go to Login
        </Link>
      </div>
    </div>
  )
}
