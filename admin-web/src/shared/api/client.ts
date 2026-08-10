import axios, { type AxiosError } from 'axios'
import type { ApiResult } from '@/shared/types/api'

const baseURL = import.meta.env.VITE_API_BASE_URL || ''

export const api = axios.create({
  baseURL,
  headers: {
    Accept: 'application/json',
    'Content-Type': 'application/json',
  },
})

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
  const ax = e as AxiosError<ApiResult>
  if (ax?.response?.data) {
    const d = ax.response.data
    const message = (d as { Message?: string }).Message ?? d.message ?? ax.message
    const errors = (d as { Errors?: Record<string, string[]> }).Errors ?? d.errors
    return new ApiError(message || 'Request failed', ax.response.status, errors)
  }
  if (e instanceof ApiError) return e
  return new ApiError(e instanceof Error ? e.message : 'Network error')
}
