import type { FranchiseeDashboard, FranchiseePage } from '@/shared/types/api'
import { apiGet, apiSend } from '@/shared/api/client'

const BASE = '/api/v1/admin/franchisees'

export function fetchFranchisees(query: {
  page?: number
  pageSize?: number
  search?: string
}) {
  return apiGet<FranchiseePage>(BASE, query as Record<string, unknown>)
}

export function createFranchisee(payload: {
  name: string
  email: string
  phone: string
  password: string
}) {
  return apiSend('post', BASE, payload)
}

export function activateFranchisee(id: number) {
  return apiSend('post', `${BASE}/activate`, { id })
}

export function fetchFranchiseeDashboard(email: string) {
  return apiGet<FranchiseeDashboard>(`${BASE}/dashboard`, { email })
}
