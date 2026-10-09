import { api, apiSend } from '@/shared/api/client'
import { getSession, setSession, type AdminSession } from './session'

type AuthResponse = {
  userId: number
  email: string
  name: string
  role: string
  accessToken: string
  refreshToken: string
}

export function toSession(r: AuthResponse): AdminSession {
  return {
    userId: r.userId,
    email: r.email,
    name: r.name,
    accessToken: r.accessToken,
    refreshToken: r.refreshToken,
  }
}

export async function login(email: string, password: string) {
  const { data } = await apiSend<AuthResponse>('post', '/api/v1/auth/login', {
    userId: email,
    password,
    role: 'ADMIN',
  })
  if (!data) throw new Error('Login failed')
  setSession(toSession(data))
}

export function logout() {
  const session = getSession()
  setSession(null)
  if (!session) return
  void api
    .post(
      '/api/v1/auth/logout',
      { refreshToken: session.refreshToken },
      { headers: { Authorization: `Bearer ${session.accessToken}` } },
    )
    .catch(() => {
      // The local session is already gone; a failed revoke only leaves the refresh token to expire.
    })
}
