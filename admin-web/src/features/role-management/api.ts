import type { ManageRoleRow, ManageRolesMeta, ManageRolesPage, UpsertRolePayload } from '@/shared/types/api'
import { apiGet, apiSend } from '@/shared/api/client'

const BASE = '/api/v1/admin/role-management'

export function fetchManageRoles(query: {
  page?: number
  pageSize?: number
  search?: string
  salesType?: string
}) {
  return apiGet<ManageRolesPage>(BASE, query as Record<string, unknown>)
}

export function fetchManageRolesMeta() {
  return apiGet<ManageRolesMeta>(`${BASE}/meta`)
}

export function createRole(payload: UpsertRolePayload) {
  return apiSend<ManageRoleRow>('post', BASE, payload)
}

export function updateRole(roleId: number, payload: UpsertRolePayload) {
  return apiSend<ManageRoleRow>('put', `${BASE}/${roleId}`, payload)
}