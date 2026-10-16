import type {
  PlanCommissionRow,
  PlanCommissionsMeta,
  PlanCommissionsPage,
  UpsertPlanCommissionPayload,
} from '@/shared/types/api'
import { apiGet, apiSend } from '@/shared/api/client'

const BASE = '/api/v1/admin/plan-commissions'

export function fetchPlanCommissions(query: {
  page?: number
  pageSize?: number
  search?: string
  roleId?: number
  pricingPlanId?: number
  status?: string
}) {
  return apiGet<PlanCommissionsPage>(BASE, query as Record<string, unknown>)
}

export function fetchPlanCommissionsMeta() {
  return apiGet<PlanCommissionsMeta>(`${BASE}/meta`)
}

export function createPlanCommissions(payload: UpsertPlanCommissionPayload) {
  return apiSend<PlanCommissionRow[]>('post', BASE, payload)
}

export function updatePlanCommission(id: number, payload: UpsertPlanCommissionPayload) {
  return apiSend<PlanCommissionRow>('put', `${BASE}/${id}`, payload)
}

export function togglePlanCommissionStatus(id: number) {
  return apiSend('patch', `${BASE}/${id}/status`)
}

export function deletePlanCommission(id: number) {
  return apiSend('delete', `${BASE}/${id}`)
}
