import { useMutation, useQuery } from '@tanstack/react-query'
import { BatteryCharging, RefreshCw, Search } from 'lucide-react'
import { useEffect, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { lookupFranchiseeWallet, rechargeWallet } from '@/features/wallet-recharge/api'
import type { FranchiseeWalletLookup, WalletRechargeResult } from '@/shared/types/api'
import { ApiError } from '@/shared/api/client'
import { useToast } from '@/shared/ui/Toast'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Textarea } from '@/components/ui/textarea'

export function RechargeWalletPage() {
  const toast = useToast()
  const [searchParams] = useSearchParams()
  const initialEmail = searchParams.get('email')?.trim() || ''
  const initialUserId = Number(searchParams.get('id') || '') || undefined

  const [email, setEmail] = useState(initialEmail)
  const [amount, setAmount] = useState('10')
  const [txnMsg, setTxnMsg] = useState('Promotional Amount')
  const [lookup, setLookup] = useState<FranchiseeWalletLookup | null>(null)
  const [result, setResult] = useState<WalletRechargeResult | null>(null)

  const autoLookup = useQuery({
    queryKey: ['wallet-recharge-lookup', initialEmail, initialUserId],
    queryFn: () => lookupFranchiseeWallet({ email: initialEmail || undefined, userId: initialUserId }),
    enabled: Boolean(initialEmail || initialUserId),
    retry: false,
  })

  useEffect(() => {
    if (autoLookup.data) {
      setLookup(autoLookup.data)
      setEmail(autoLookup.data.email)
    }
  }, [autoLookup.data])

  const lookupMut = useMutation({
    mutationFn: () => lookupFranchiseeWallet({ email: email.trim() }),
    onSuccess: (data) => {
      setLookup(data)
      setEmail(data.email)
      setResult(null)
      toast.push('Franchisee found', 'success')
    },
    onError: (e: Error) => toast.push(e instanceof ApiError ? e.message : 'Lookup failed', 'error'),
  })

  const rechargeMut = useMutation({
    mutationFn: () =>
      rechargeWallet({
        userEmail: email.trim(),
        amount: Number(amount),
        txnMsg: txnMsg.trim() || undefined,
      }),
    onSuccess: (res) => {
      const data = res.data
      if (data) {
        setResult(data)
        setLookup((prev) =>
          prev
            ? {
                ...prev,
                currentBalance: data.newBalance,
                currentBalanceDisplay: data.newBalanceDisplay,
              }
            : prev,
        )
      }
      toast.push(res.message || data?.message || 'Wallet recharged', 'success')
    },
    onError: (e: Error) => toast.push(e instanceof ApiError ? e.message : 'Recharge failed', 'error'),
  })

  const submit = (e: React.FormEvent) => {
    e.preventDefault()
    const amt = Number(amount)
    if (!email.trim()) return toast.push('Franchisee email is required', 'error')
    if (!(amt >= 10 && amt <= 1000)) return toast.push('Amount must be between ₹10 and ₹1000', 'error')
    if (!txnMsg.trim()) return toast.push('Comment is required', 'error')
    rechargeMut.mutate()
  }

  return (
    <div className="mx-auto flex h-full min-h-0 w-full max-w-xl flex-col gap-4 overflow-auto">
      <div>
        <h1 className="font-[family-name:var(--font-display)] flex items-center gap-2 text-3xl font-semibold tracking-tight">
          <BatteryCharging size={28} className="text-rose-600" />
          Recharge Wallet
        </h1>
        <p className="mt-1 text-sm text-muted-foreground">
          Credit promotional balance to a franchisee wallet (₹10–₹1000)
        </p>
      </div>

      {result && (
        <Card className="border-emerald-200 bg-emerald-50">
          <CardHeader className="pb-2">
            <CardTitle className="text-base text-emerald-900">Success</CardTitle>
            <CardDescription className="text-emerald-800">{result.message}</CardDescription>
          </CardHeader>
          <CardContent className="text-sm text-emerald-900">
            <div>
              {result.userName} · {result.userEmail}
            </div>
            <div className="mt-1 font-semibold">
              Credited {result.amountDisplay} · New balance {result.newBalanceDisplay}
            </div>
          </CardContent>
        </Card>
      )}

      <Card>
        <CardHeader>
          <CardTitle className="text-lg">Credit amount</CardTitle>
          <CardDescription>Verify the franchisee, then submit the recharge</CardDescription>
        </CardHeader>
        <CardContent>
          <form className="space-y-4" onSubmit={submit}>
            <div className="space-y-1.5">
              <label className="text-xs font-medium text-muted-foreground">Franchisee Email *</label>
              <div className="flex gap-2">
                <Input
                  type="email"
                  value={email}
                  onChange={(e) => {
                    setEmail(e.target.value)
                    setLookup(null)
                    setResult(null)
                  }}
                  placeholder="franchisee@email.com"
                  required
                />
                <Button
                  type="button"
                  variant="outline"
                  disabled={lookupMut.isPending || !email.trim()}
                  onClick={() => lookupMut.mutate()}
                >
                  {lookupMut.isPending ? <RefreshCw size={16} className="animate-spin" /> : <Search size={16} />}
                  Lookup
                </Button>
              </div>
            </div>

            {lookup && (
              <div className="rounded-lg border bg-muted/40 px-3 py-2 text-sm">
                <div className="font-medium">{lookup.name}</div>
                <div className="text-muted-foreground">
                  ID {String(lookup.userId).padStart(5, '0')}
                  {lookup.phone ? ` · ${lookup.phone}` : ''}
                </div>
                <div className="mt-1 font-semibold text-emerald-700">
                  Current balance: {lookup.currentBalanceDisplay}
                </div>
              </div>
            )}

            <div className="space-y-1.5">
              <label className="text-xs font-medium text-muted-foreground">Amount (₹10–₹1000) *</label>
              <Input
                type="number"
                min={10}
                max={1000}
                step="1"
                value={amount}
                onChange={(e) => setAmount(e.target.value)}
                required
              />
            </div>

            <div className="space-y-1.5">
              <label className="text-xs font-medium text-muted-foreground">Comment *</label>
              <Textarea
                value={txnMsg}
                onChange={(e) => setTxnMsg(e.target.value)}
                rows={3}
                placeholder="Promotional Amount"
                required
              />
            </div>

            <Button type="submit" className="w-full" disabled={rechargeMut.isPending}>
              {rechargeMut.isPending ? 'Processing…' : 'Recharge Wallet'}
            </Button>
          </form>
        </CardContent>
      </Card>
    </div>
  )
}
