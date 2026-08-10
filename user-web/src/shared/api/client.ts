import axios, { type AxiosError } from 'axios'
import type { ApiResult } from '@/shared/types/api'
import {
  getAuthToken,
  ensureValidSessionOrRedirect,
  redirectToLogin,
  getAuthSession,
} from '@/shared/auth/session'

const baseURL = import.meta.env.VITE_API_BASE_URL || ''

export const api = axios.create({
  baseURL,
  headers: {
    Accept: 'application/json',
    'Content-Type': 'application/json',
  },
})

api.interceptors.request.use((config) => {
  const url = String(config.url || '')
  const isAuthPublic =
    url.includes('/api/v1/auth/') ||
    url.includes('/api/v1/public/') ||
    url.includes('/api/v1/registration/')

  // Only gate when a local session exists but is expired (don't block anonymous calls).
  if (!isAuthPublic) {
    const session = getAuthSession()
    if (session && !ensureValidSessionOrRedirect()) {
      return Promise.reject(new ApiError('Your session has expired. Please log in again.', 401))
    }
  }

  const token = getAuthToken()
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  // Browser must set multipart boundary for FormData
  if (typeof FormData !== 'undefined' && config.data instanceof FormData) {
    if (config.headers && typeof config.headers.set === 'function') {
      config.headers.set('Content-Type', undefined as unknown as string)
    } else if (config.headers) {
      delete (config.headers as Record<string, unknown>)['Content-Type']
    }
  }
  return config
})

api.interceptors.response.use(
  (res) => res,
  (error: AxiosError<ApiResult>) => {
    const status = error.response?.status
    if (status === 401 || status === 403) {
      redirectToLogin('unauthorized', getAuthSession()?.role)
    }
    return Promise.reject(error)
  },
)

export class ApiError extends Error {
  status?: number
  errors?: Record<string, string[]> | null

  constructor(message: string, status?: number, errors?: Record<string, string[]> | null) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.errors = errors
  }
}

function unwrap<T>(payload: ApiResult<T>, status: number): T {
  const success = (payload as { Success?: boolean }).Success ?? payload.success
  const message = (payload as { Message?: string }).Message ?? payload.message
  const data = ((payload as { Data?: T }).Data ?? payload.data) as T | null | undefined
  const errors = (payload as { Errors?: Record<string, string[]> }).Errors ?? payload.errors

  if (!success) {
    throw new ApiError(message || 'Request failed', status, errors)
  }
  return data as T
}

export async function apiGet<T>(url: string, params?: Record<string, unknown>): Promise<T> {
  try {
    const res = await api.get<ApiResult<T>>(url, { params })
    return unwrap(res.data, res.status)
  } catch (e) {
    throw toApiError(e)
  }
}

/** Multipart form upload (do not set JSON Content-Type). */
export async function apiUploadForm<T = unknown>(
  url: string,
  formData: FormData,
): Promise<{ message?: string | null; data?: T }> {
  try {
    const res = await api.post<ApiResult<T>>(url, formData)
    const payload = res.data
    const success = (payload as { Success?: boolean }).Success ?? payload.success
    const message = (payload as { Message?: string }).Message ?? payload.message
    const data = ((payload as { Data?: T }).Data ?? payload.data) as T | undefined
    const errors = (payload as { Errors?: Record<string, string[]> }).Errors ?? payload.errors
    if (!success) throw new ApiError(message || 'Upload failed', res.status, errors)
    return { message, data }
  } catch (e) {
    throw toApiError(e)
  }
}

export async function apiSend<T = unknown>(
  method: 'post' | 'put' | 'patch' | 'delete',
  url: string,
  body?: unknown,
): Promise<{ message?: string | null; data?: T }> {
  try {
    const res = await api.request<ApiResult<T>>({ method, url, data: body })
    const payload = res.data
    const success = (payload as { Success?: boolean }).Success ?? payload.success
    const message = (payload as { Message?: string }).Message ?? payload.message
    const data = ((payload as { Data?: T }).Data ?? payload.data) as T | undefined
    const errors = (payload as { Errors?: Record<string, string[]> }).Errors ?? payload.errors
    if (!success) throw new ApiError(message || 'Request failed', res.status, errors)
    return { message, data }
  } catch (e) {
    throw toApiError(e)
  }
}

function toApiError(e: unknown): ApiError {
  if (e instanceof ApiError) return e
  const ax = e as AxiosError<ApiResult>
  if (ax?.response?.data) {
    const d = ax.response.data
    // Proxy/HTML 500 bodies are often strings — fall back to ax.message
    const fromBody =
      typeof d === 'string'
        ? null
        : ((d as { Message?: string }).Message ?? d.message)
    let message = fromBody || ax.message || 'Request failed'
    if (ax.response.status >= 500 && /status code 5\d\d/i.test(message)) {
      message = 'Server is unavailable. Please try again in a moment.'
    }
    return new ApiError(message, ax.response.status, typeof d === 'object' ? ((d as { Errors?: Record<string, string[]> }).Errors ?? d.errors) : null)
  }
  if (ax?.code === 'ERR_NETWORK' || ax?.message === 'Network Error') {
    return new ApiError('Server is unavailable. Please try again in a moment.')
  }
  return new ApiError(e instanceof Error ? e.message : 'Network error', ax?.response?.status)
}
