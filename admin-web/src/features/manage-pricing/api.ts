import type {
  ManagePricingMeta,
  ManagePricingPage,
  ManagePricingRow,
  UpsertPricingPayload,
} from '@/shared/types/api'
import { apiGet, apiSend } from '@/shared/api/client'

const BASE = '/api/v1/admin/manage-pricing'

export function fetchManagePricing(query: {
  page?: number
  pageSize?: number
  search?: string
  roleId?: number
  status?: string
}) {
  return apiGet<ManagePricingPage>(BASE, query as Record<string, unknown>)
}

export function fetchManagePricingMeta() {
  return apiGet<ManagePricingMeta>(`${BASE}/meta`)
}

export function createPricing(payload: UpsertPricingPayload) {
  return apiSend<ManagePricingRow>('post', BASE, payload)
}

export function updatePricing(id: number, payload: UpsertPricingPayload) {
  return apiSend<ManagePricingRow>('put', `${BASE}/${id}`, payload)
}

export function togglePricingStatus(id: number) {
  return apiSend('patch', `${BASE}/${id}/status`)
}

export function deletePricing(id: number) {
  return apiSend('delete', `${BASE}/${id}`)
}
