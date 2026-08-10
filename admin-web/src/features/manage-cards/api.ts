import type { ManageCardsPage } from '@/shared/types/api'
import { apiGet, apiSend } from '@/shared/api/client'

const BASE = '/api/v1/admin/manage-cards'

export type ManageCardsQuery = {
  page?: number
  pageSize?: number
  search?: string
  paymentFilter?: string
}

export function fetchManageCards(query: ManageCardsQuery) {
  return apiGet<ManageCardsPage>(BASE, query as Record<string, unknown>)
}

export function setComplimentary(cardId: number, status: 'Yes' | 'No') {
  return apiSend('patch', `${BASE}/${cardId}/complimentary`, { status })
}
