import type { BankDetails, DashboardDetails, ManageUsersPage, ManageUsersQuery, ReferralDetails } from '@/shared/types/api'
import { api, apiGet, apiSend } from '@/shared/api/client'

const BASE = '/api/v1/admin/manage-users'

export function fetchManageUsers(query: ManageUsersQuery) {
  return apiGet<ManageUsersPage>(BASE, query as Record<string, unknown>)
}

export function mapDeal(userEmail: string, dealId: number, createdBy = 'admin') {
  return apiSend('post', `${BASE}/deals/map`, { userEmail, dealId, createdBy })
}

export function removeDeal(mappingId: number) {
  return apiSend('delete', `${BASE}/deals/${mappingId}`)
}

export function setCollaboration(email: string, status: string) {
  return apiSend('patch', `${BASE}/collaboration`, { email, status })
}

export function setSaleskit(email: string, status: string) {
  return apiSend('patch', `${BASE}/saleskit`, { email, status })
}

export function setRefund(email: string, refundStatus: string) {
  return apiSend('patch', `${BASE}/refund`, { email, refundStatus })
}

export function resetPassword(email: string, newPassword: string, role = 'CUSTOMER') {
  return apiSend('post', `${BASE}/reset-password`, { email, role, newPassword })
}

export function fetchDashboardDetails(userEmail: string) {
  return apiGet<DashboardDetails>(`${BASE}/dashboard-details`, { userEmail })
}

export function fetchReferralDetails(referrerEmail: string) {
  return apiGet<ReferralDetails>(`${BASE}/referral-details`, { referrerEmail })
}

export function upsertBankDetails(payload: { userEmail: string } & BankDetails) {
  return apiSend('put', `${BASE}/bank-details`, {
    userEmail: payload.userEmail,
    accountHolderName: payload.accountHolderName,
    accountNumber: payload.accountNumber,
    ifscCode: payload.ifscCode,
    bankName: payload.bankName,
    upiId: payload.upiId,
    upiName: payload.upiName,
  })
}

export async function downloadManageUsersCsv(query: ManageUsersQuery) {
  const res = await api.get(`${BASE}/export`, {
    params: query,
    responseType: 'blob',
  })
  const blob = new Blob([res.data], { type: 'text/csv' })
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = 'manage_users_export.csv'
  a.click()
  URL.revokeObjectURL(url)
}
