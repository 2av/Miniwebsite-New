import type { RoleAccessMatrix, UpdateRoleAccessSettingPayload } from '@/shared/types/api'
import { apiGet, apiSend } from '@/shared/api/client'

const BASE = '/api/v1/admin/role-access-settings'

export function fetchRoleAccessMatrix() {
  return apiGet<RoleAccessMatrix>(`${BASE}/matrix`)
}

export function updateRoleAccessSetting(id: number, payload: UpdateRoleAccessSettingPayload) {
  return apiSend('patch', `${BASE}/${id}`, payload)
}
