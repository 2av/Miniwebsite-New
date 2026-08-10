export type ApiResult<T = unknown> = {
  success: boolean
  message?: string | null
  data?: T | null
  errors?: Record<string, string[]> | null
}

export type UserRole = 'CUSTOMER' | 'FRANCHISEE' | 'TEAM' | 'ADMIN'

export type AuthSession = {
  token: string
  refreshToken: string
  role: UserRole
  email: string
  name: string
  userId?: number
  collaborationEnabled?: boolean
  saleskitEnabled?: boolean
  /** ISO timestamp from login API (`accessTokenExpiresAt`). */
  accessTokenExpiresAt?: string
}
