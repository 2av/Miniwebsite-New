import { apiGet } from '@/shared/api/client'

export type PeriodStats = {
  orders: number
  collected: number
  newProfiles: number
}

export type DashboardQueue = {
  partnerInvoices: number
  eligiblePayouts: number
  eligiblePayoutAmount: number
  documentsPending: number
  suspendedProfiles: number
  expiringProfiles: number
}

export type PartnerCount = {
  slug: string
  label: string
  count: number
}

export type RecentOrder = {
  invoiceId: number
  orderId: string
  user: string
  amount: number
  paymentStatus: string
  when: string
}

export type RecentProfile = {
  cardId: number
  dbId: string
  company: string
  status: string
  created: string
}

export type AdminDashboard = {
  asOf: string
  today: PeriodStats
  month: PeriodStats
  commissionEarnedThisMonth: number
  payoutOutstanding: number
  queue: DashboardQueue
  users: { active: number; inActive: number }
  profiles: { active: number; suspended: number; expired: number }
  partners: PartnerCount[]
  recentOrders: RecentOrder[]
  recentProfiles: RecentProfile[]
}

export function fetchAdminDashboard() {
  return apiGet<AdminDashboard>('/api/v1/admin/dashboard')
}
