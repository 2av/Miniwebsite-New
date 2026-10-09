import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useEffect, useState } from 'react'
import { lookupFranchiseeWallet, rechargeWallet } from '@/features/wallet-recharge/api'
import { ApiError } from '@/shared/api/client'
import { useToast } from '@/shared/ui/Toast'
import { Button } from '@/components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Input } from '@/components/ui/input'
import { Textarea } from '@/components/ui/textarea'

export type RechargeWalletTarget = { id: number; email: string; name: string }

const MIN_AMOUNT = 10
const MAX_AMOUNT = 1000

export function RechargeWalletDialog({
  target,
  onClose,
}: {
  target: RechargeWalletTarget | null
  onClose: () => void
}) {
  const toast = useToast()
  const qc = useQueryClient()
  const [amount, setAmount] = useState(String(MIN_AMOUNT))
  const [txnMsg, setTxnMsg] = useState('Promotional Amount')

  useEffect(() => {
    if (target) {
      setAmount(String(MIN_AMOUNT))
      setTxnMsg('Promotional Amount')
    }
  }, [target])

  const balanceQuery = useQuery({
    queryKey: ['wallet-recharge-lookup', target?.id],
    queryFn: () => lookupFranchiseeWallet({ userId: target!.id }),
    enabled: !!target,
    retry: false,
  })

  const rechargeMut = useMutation({
    mutationFn: () =>
      rechargeWallet({
        userEmail: target!.email,
        amount: Number(amount),
        txnMsg: txnMsg.trim(),
      }),
    onSuccess: async (res) => {
      toast.push(res.data?.message || res.message || 'Wallet recharged', 'success')
      await qc.invalidateQueries({ queryKey: ['wallet-recharge-lookup'] })
      onClose()
    },
    onError: (e: Error) => toast.push(e instanceof ApiError ? e.message : 'Recharge failed', 'error'),
  })

  const submit = () => {
    const amt = Number(amount)
    if (!(amt >= MIN_AMOUNT && amt <= MAX_AMOUNT))
      return toast.push(`Amount must be between ₹${MIN_AMOUNT} and ₹${MAX_AMOUNT}`, 'error')
    if (!txnMsg.trim()) return toast.push('Comment is required', 'error')
    rechargeMut.mutate()
  }

  const busy = rechargeMut.isPending

  return (
    <Dialog open={!!target} onOpenChange={(o) => !o && !busy && onClose()}>
      <DialogContent className="max-w-md">
        <DialogHeader>
          <DialogTitle>Recharge Wallet</DialogTitle>
          <DialogDescription>
            {target?.name} · {target?.email}
          </DialogDescription>
        </DialogHeader>

        <div className="rounded-lg border bg-muted/40 px-3 py-2 text-sm">
          {balanceQuery.isLoading && <span className="text-muted-foreground">Loading balance…</span>}
          {balanceQuery.isError && (
            <span className="text-destructive">{(balanceQuery.error as Error).message}</span>
          )}
          {balanceQuery.data && (
            <span className="font-semibold text-emerald-700">
              Current balance: {balanceQuery.data.currentBalanceDisplay}
            </span>
          )}
        </div>

        <div className="space-y-1.5">
          <label className="text-xs font-medium text-muted-foreground">
            Amount (₹{MIN_AMOUNT}–₹{MAX_AMOUNT}) *
          </label>
          <Input
            type="number"
            min={MIN_AMOUNT}
            max={MAX_AMOUNT}
            step="1"
            value={amount}
            onChange={(e) => setAmount(e.target.value)}
          />
        </div>
        <div className="space-y-1.5">
          <label className="text-xs font-medium text-muted-foreground">Comment *</label>
          <Textarea
            value={txnMsg}
            onChange={(e) => setTxnMsg(e.target.value)}
            rows={3}
            placeholder="Promotional Amount"
          />
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={onClose} disabled={busy}>
            Cancel
          </Button>
          <Button onClick={submit} disabled={busy || balanceQuery.isError}>
            {busy ? 'Processing…' : 'Recharge Wallet'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
