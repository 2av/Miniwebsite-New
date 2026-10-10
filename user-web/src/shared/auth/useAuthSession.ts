import { useEffect, useState } from 'react'
import { getAuthSession, isSessionValid, redirectToLogin } from '@/shared/auth/session'
import type { AuthSession } from '@/shared/types/api'

export function useAuthSession() {
  const [session, setSession] = useState<AuthSession | null>(() => {
    const s = getAuthSession()
    return isSessionValid(s) ? s : null
  })

  useEffect(() => {
    const sync = () => {
      const s = getAuthSession()
      if (s && !isSessionValid(s)) {
        redirectToLogin('expired')
        setSession(null)
        return
      }
      setSession(isSessionValid(s) ? s : null)
    }
    sync()
    window.addEventListener('storage', sync)
    window.addEventListener('mw-auth-changed', sync)
    return () => {
      window.removeEventListener('storage', sync)
      window.removeEventListener('mw-auth-changed', sync)
    }
  }, [])

  return session
}
