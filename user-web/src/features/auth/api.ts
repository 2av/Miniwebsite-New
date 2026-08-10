import { apiSend } from '@/shared/api/client'
import type { UserRole } from '@/shared/types/api'

export type AuthApiResponse = {
  userId: number
  email: string
  name: string
  role: string
  accessToken: string
  refreshToken: string
  accessTokenExpiresAt: string
  collaborationEnabled?: string | null
  saleskitEnabled?: string | null
}

function pick<T>(obj: Record<string, unknown>, camel: string, pascal: string): T {
  return (obj[camel] ?? obj[pascal]) as T
}

export function normalizeAuthResponse(raw: unknown): AuthApiResponse {
  const o = (raw ?? {}) as Record<string, unknown>
  return {
    userId: Number(pick(o, 'userId', 'UserId')),
    email: String(pick(o, 'email', 'Email') ?? ''),
    name: String(pick(o, 'name', 'Name') ?? ''),
    role: String(pick(o, 'role', 'Role') ?? '').toUpperCase(),
    accessToken: String(pick(o, 'accessToken', 'AccessToken') ?? ''),
    refreshToken: String(pick(o, 'refreshToken', 'RefreshToken') ?? ''),
    accessTokenExpiresAt: String(pick(o, 'accessTokenExpiresAt', 'AccessTokenExpiresAt') ?? ''),
    collaborationEnabled: (pick(o, 'collaborationEnabled', 'CollaborationEnabled') as string) ?? null,
    saleskitEnabled: (pick(o, 'saleskitEnabled', 'SaleskitEnabled') as string) ?? null,
  }
}

export async function loginApi(userId: string, password: string, role: UserRole) {
  const { data } = await apiSend<unknown>('post', '/api/v1/auth/login', {
    userId,
    password,
    role,
  })
  return normalizeAuthResponse(data)
}

export async function forgotPasswordApi(email: string) {
  return apiSend('post', '/api/v1/auth/forgot-password', { email })
}

export async function resetPasswordApi(email: string, token: string, newPassword: string) {
  return apiSend('post', '/api/v1/auth/reset-password', { email, token, newPassword })
}
