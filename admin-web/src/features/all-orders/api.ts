import type { AllOrdersPage } from '@/shared/types/api'
import { apiGet, apiSend } from '@/shared/api/client'

const BASE = '/api/v1/admin/all-orders'

export function fetchAllOrders(query: {
  page?: number
  pageSize?: number
  search?: string
}) {
  return apiGet<AllOrdersPage>(BASE, query as Record<string, unknown>)
}

export function updateOrderComment(invoiceId: number, comment: string) {
  return apiSend('patch', `${BASE}/${invoiceId}/comment`, { comment })
}
