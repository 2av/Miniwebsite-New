import type { AllOrdersPage } from '@/shared/types/api'
import { apiGet } from '@/shared/api/client'

const BASE = '/api/v1/admin/all-orders'

export function fetchAllOrders(query: {
  page?: number
  pageSize?: number
  search?: string
}) {
  return apiGet<AllOrdersPage>(BASE, query as Record<string, unknown>)
}
