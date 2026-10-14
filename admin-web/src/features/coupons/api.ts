import type { CouponRow, CouponsMeta, CouponsPage, UpsertCouponPayload } from '@/shared/types/api'
import { apiGet, apiSend } from '@/shared/api/client'

const BASE = '/api/v1/admin/coupons'

export function fetchCoupons(query: {
  page?: number
  pageSize?: number
  search?: string
  roleId?: number
  status?: string
}) {
  return apiGet<CouponsPage>(BASE, query as Record<string, unknown>)
}

export function fetchCouponsMeta() {
  return apiGet<CouponsMeta>(`${BASE}/meta`)
}

export function generateCouponCode() {
  return apiGet<{ code: string }>(`${BASE}/generate-code`)
}

export function createCoupon(payload: UpsertCouponPayload) {
  return apiSend<CouponRow>('post', BASE, payload)
}

export function updateCoupon(id: number, payload: UpsertCouponPayload) {
  return apiSend<CouponRow>('put', `${BASE}/${id}`, payload)
}

export function deleteCoupon(id: number) {
  return apiSend('delete', `${BASE}/${id}`)
}
