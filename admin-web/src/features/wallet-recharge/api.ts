import type { FranchiseeWalletLookup, WalletRechargeResult } from '@/shared/types/api'
import { apiGet, apiSend } from '@/shared/api/client'

const BASE = '/api/v1/admin/wallet-recharge'

export function lookupFranchiseeWallet(query: { email?: string; userId?: number }) {
  return apiGet<FranchiseeWalletLookup>(`${BASE}/lookup`, query as Record<string, unknown>)
}

export function rechargeWallet(payload: {
  userEmail: string
  amount: number
  txnMsg?: string
}) {
  return apiSend<WalletRechargeResult>('post', BASE, payload)
}
