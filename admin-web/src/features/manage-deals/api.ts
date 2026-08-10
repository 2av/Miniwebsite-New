import type { ManageDealRow, ManageDealsMeta, ManageDealsPage, UpsertDealPayload } from '@/shared/types/api'
import { apiGet, apiSend } from '@/shared/api/client'

const BASE = '/api/v1/admin/manage-deals'

export function fetchManageDeals(query: {
  page?: number
  pageSize?: number
  search?: string
  planType?: string
  status?: string
}) {
  return apiGet<ManageDealsPage>(BASE, query as Record<string, unknown>)
}

export function fetchManageDealsMeta() {
  return apiGet<ManageDealsMeta>(`${BASE}/meta`)
}

export function createDeal(payload: UpsertDealPayload) {
  return apiSend<ManageDealRow>('post', BASE, payload)
}

export function updateDeal(id: number, payload: UpsertDealPayload) {
  return apiSend<ManageDealRow>('put', `${BASE}/${id}`, payload)
}

export function toggleDealStatus(id: number) {
  return apiSend('patch', `${BASE}/${id}/status`)
}

export function deleteDeal(id: number) {
  return apiSend('delete', `${BASE}/${id}`)
}
