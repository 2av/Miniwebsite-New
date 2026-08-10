import type {
  ManageReferralBankDetails,
  ManageReferralsPage,
  ReferralPaymentHistory,
  ReferrerPaymentDetails,
} from '@/shared/types/api'
import { apiGet, apiSend } from '@/shared/api/client'

const BASE = '/api/v1/admin/manage-referrals'

export function fetchManageReferrals(query: {
  page?: number
  pageSize?: number
  search?: string
}) {
  return apiGet<ManageReferralsPage>(BASE, query as Record<string, unknown>)
}

export function fetchReferrerPaymentDetails(referrerEmail: string) {
  return apiGet<ReferrerPaymentDetails>(`${BASE}/referrer-details`, { referrerEmail })
}

export function fetchReferralPaymentHistory(referralId: number) {
  return apiGet<ReferralPaymentHistory>(`${BASE}/${referralId}/payment-history`)
}

export function processReferralPayment(payload: {
  referralId: number
  amount: number
  transactionNumber: string
  paymentMethod: string
  paymentNotes?: string
  processedBy?: string
}) {
  return apiSend('post', `${BASE}/payments`, payload)
}

export function fetchReferralBankDetails(userEmail: string) {
  return apiGet<ManageReferralBankDetails>(`${BASE}/bank-details`, { userEmail })
}
