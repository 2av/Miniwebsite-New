import type { ManageFaqRow, ManageFaqsMeta, ManageFaqsPage, UpsertFaqPayload } from '@/shared/types/api'
import { apiGet, apiSend } from '@/shared/api/client'

const BASE = '/api/v1/admin/manage-faqs'

export function fetchManageFaqs(query: {
  page?: number
  pageSize?: number
  search?: string
  pageType?: string
  status?: string
}) {
  return apiGet<ManageFaqsPage>(BASE, query as Record<string, unknown>)
}

export function fetchManageFaqsMeta() {
  return apiGet<ManageFaqsMeta>(`${BASE}/meta`)
}

export function createFaq(payload: UpsertFaqPayload) {
  return apiSend<ManageFaqRow>('post', BASE, payload)
}

export function updateFaq(id: number, payload: UpsertFaqPayload) {
  return apiSend<ManageFaqRow>('put', `${BASE}/${id}`, payload)
}

export function deleteFaq(id: number) {
  return apiSend('delete', `${BASE}/${id}`)
}
