import type { AuthSession } from '@/shared/types/api'

const STORAGE_KEY = 'mw_user_session'

let redirectingToLogin = false

export function getAuthSession(): AuthSession | null {
  try {
    const raw = localStorage.getItem(STORAGE_KEY)
    if (!raw) return null
    return JSON.parse(raw) as AuthSession
  } catch {
    return null
  }
}

export function getAuthToken(): string | null {
  return getAuthSession()?.token ?? null
}

export function setAuthSession(session: AuthSession) {
  localStorage.setItem(STORAGE_KEY, JSON.stringify(session))
  window.dispatchEvent(new Event('mw-auth-changed'))
}

export function clearAuthSession() {
  localStorage.removeItem(STORAGE_KEY)
  window.dispatchEvent(new Event('mw-auth-changed'))
}

export function isAuthenticated() {
  return isSessionValid(getAuthSession())
}

/** Default post-login path by role. */
export function dashboardPathForRole(_role: string) {
  return '/dashboard'
}

export const LOGIN_PATH = '/login'

/** Read JWT `exp` (seconds) when present. */
function jwtExpiresAtMs(token?: string | null): number | null {
  if (!token) return null
  try {
    const parts = token.split('.')
    if (parts.length < 2) return null
    const json = atob(parts[1].replace(/-/g, '+').replace(/_/g, '/'))
    const payload = JSON.parse(json) as { exp?: number }
    if (!payload.exp || typeof payload.exp !== 'number') return null
    return payload.exp * 1000
  } catch {
    return null
  }
}

export function getSessionExpiresAtMs(session: AuthSession | null): number | null {
  if (!session) return null
  if (session.accessTokenExpiresAt) {
    const t = Date.parse(session.accessTokenExpiresAt)
    if (!Number.isNaN(t)) return t
  }
  return jwtExpiresAtMs(session.token)
}

/** True when session exists and token is not past expiry (30s skew). */
export function isSessionValid(session: AuthSession | null): boolean {
  if (!session?.token || !session.email) return false
  const exp = getSessionExpiresAtMs(session)
  if (exp == null) return true // no expiry info — treat as valid until API says otherwise
  return Date.now() < exp - 30_000
}

/**
 * Clear session and send user to the login page.
 * Safe to call multiple times (single redirect).
 */
export function redirectToLogin(reason: 'expired' | 'unauthorized' = 'expired') {
  clearAuthSession()

  if (typeof window === 'undefined') return
  if (redirectingToLogin) return
  if (window.location.pathname.startsWith('/login')) return

  redirectingToLogin = true
  const q = reason === 'expired' || reason === 'unauthorized' ? '?session=expired' : ''
  window.location.assign(`${LOGIN_PATH}${q}`)
}

/** If local session is missing/expired, redirect and return true. */
export function ensureValidSessionOrRedirect(): boolean {
  const session = getAuthSession()
  if (isSessionValid(session)) return true
  redirectToLogin(session ? 'expired' : 'unauthorized')
  return false
}
