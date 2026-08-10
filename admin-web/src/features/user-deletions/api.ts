import type { UserDeletionPage } from '@/shared/types/api'
import { apiGet, apiSend } from '@/shared/api/client'

const BASE = '/api/v1/admin/user-deletions'

export function fetchUserDeletions(query: {
  page?: number
  pageSize?: number
  search?: string
  role?: string
  status?: string
}) {
  return apiGet<UserDeletionPage>(BASE, query as Record<string, unknown>)
}

export function softDeleteUsers(userIds: number[]) {
  return apiSend('post', `${BASE}/soft-delete`, { userIds })
}

export function restoreUsers(userIds: number[]) {
  return apiSend('post', `${BASE}/restore`, { userIds })
}
