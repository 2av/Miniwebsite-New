import type {
  SalesChannelPartnerRow,
  SalesChannelPartnersPage,
  UpsertSalesChannelPartnerPayload,
} from '@/shared/types/api'
import { apiGet, apiSend } from '@/shared/api/client'

const BASE = '/api/v1/admin/sales-channel-partners'

export function fetchSalesChannelPartners(
  slug: string,
  query: { page?: number; pageSize?: number; search?: string; status?: string },
) {
  return apiGet<SalesChannelPartnersPage>(`${BASE}/${slug}`, query as Record<string, unknown>)
}

export function createSalesChannelPartner(slug: string, payload: UpsertSalesChannelPartnerPayload & { password: string }) {
  return apiSend<SalesChannelPartnerRow>('post', `${BASE}/${slug}`, payload)
}

export function updateSalesChannelPartner(slug: string, id: number, payload: UpsertSalesChannelPartnerPayload) {
  return apiSend<SalesChannelPartnerRow>('put', `${BASE}/${slug}/${id}`, payload)
}

export function toggleSalesChannelPartnerStatus(slug: string, id: number, newStatus?: string) {
  return apiSend<SalesChannelPartnerRow>('post', `${BASE}/${slug}/${id}/toggle-status`, { newStatus })
}

export function resetSalesChannelPartnerPassword(slug: string, id: number, newPassword: string) {
  return apiSend('post', `${BASE}/${slug}/${id}/reset-password`, { newPassword })
}
