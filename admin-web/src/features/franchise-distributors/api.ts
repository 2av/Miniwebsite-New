import type { FranchiseDistributorPage } from '@/shared/types/api'
import { apiGet, apiSend } from '@/shared/api/client'

const BASE = '/api/v1/admin/franchise-distributors'

export function fetchFranchiseDistributors(query: {
  page?: number
  pageSize?: number
  search?: string
}) {
  return apiGet<FranchiseDistributorPage>(BASE, query as Record<string, unknown>)
}

export function setInfluencer(email: string, status: string) {
  return apiSend('patch', `${BASE}/influencer`, { email, status })
}
