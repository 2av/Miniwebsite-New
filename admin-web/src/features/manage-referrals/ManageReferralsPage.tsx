import { useQuery, useQueryClient } from '@tanstack/react-query'
import { Download, Eye, History, RefreshCw, Search, X } from 'lucide-react'
import { useEffect, useMemo, useState } from 'react'
import {
  fetchManageReferrals,
  fetchReferralBankDetails,
  fetchReferralPaymentHistory,
  fetchReferrerPaymentDetails,
  processReferralPayment,
} from '@/features/manage-referrals/api'
import { upsertBankDetails } from '@/features/manage-users/api'
import type { ManageReferralRow, ReferrerPaymentLine } from '@/shared/types/api'
import { ApiError } from '@/shared/api/client'
import { badgeVariantFromTone } from '@/shared/lib/badgeTone'
import { openInvoiceByCardId } from '@/shared/lib/invoiceDownload'
import { FiltersButton, FiltersDrawer } from '@/shared/ui/FiltersDrawer'
import { useToast } from '@/shared/ui/Toast'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Input } from '@/components/ui/input'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import { Textarea } from '@/components/ui/textarea'

function formatDate(value?: string | null) {
  if (!value) return '-'
  const d = new Date(value)
  if (Number.isNaN(d.getTime())) return '-'
  return d.toLocaleDateString('en-IN', { day: '2-digit', month: 'short', year: 'numeric' })
}

function formatMoney(n: number) {
  return `₹${n.toLocaleString('en-IN', { maximumFractionDigits: 0 })}`
}

export function ManageReferralsPage() {
  const toast = useToast()
  const qc = useQueryClient()
  const [page, setPage] = useState(1)
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [filtersOpen, setFiltersOpen] = useState(false)

  const [detailsEmail, setDetailsEmail] = useState<string | null>(null)
  const [bankEmail, setBankEmail] = useState<string | null>(null)
  const [bankEdit, setBankEdit] = useState(false)
  const [bankForm, setBankForm] = useState({
    accountHolderName: '',
    accountNumber: '',
    ifscCode: '',
    bankName: '',
    upiId: '',
    upiName: '',
  })
  const [bankBusy, setBankBusy] = useState(false)

  const [paymentTarget, setPaymentTarget] = useState<{
    referralId: number
    amount: number
  } | null>(null)
  const [payAmount, setPayAmount] = useState('')
  const [payTxn, setPayTxn] = useState('')
  const [payMethod, setPayMethod] = useState('')
  const [payNotes, setPayNotes] = useState('')
  const [payBusy, setPayBusy] = useState(false)

  const [historyId, setHistoryId] = useState<number | null>(null)

  useEffect(() => {
    const t = window.setTimeout(() => {
      const next = searchInput.trim()
      setSearch((prev) => {
        if (prev === next) return prev
        setPage(1)
        return next
      })
    }, 350)
    return () => window.clearTimeout(t)
  }, [searchInput])

  const filters = useMemo(() => ({ page, pageSize: 10, search: search || undefined }), [page, search])
  const queryKey = useMemo(() => ['manage-referrals', filters] as const, [filters])

  const listQuery = useQuery({
    queryKey,
    queryFn: () => fetchManageReferrals(filters),
  })

  const detailsQuery = useQuery({
    queryKey: ['manage-referrals-details', detailsEmail],
    queryFn: () => fetchReferrerPaymentDetails(detailsEmail!),
    enabled: !!detailsEmail,
  })

  const bankQuery = useQuery({
    queryKey: ['manage-referrals-bank', bankEmail],
    queryFn: () => fetchReferralBankDetails(bankEmail!),
    enabled: !!bankEmail,
  })

  const historyQuery = useQuery({
    queryKey: ['manage-referrals-history', historyId],
    queryFn: () => fetchReferralPaymentHistory(historyId!),
    enabled: historyId != null,
  })

  useEffect(() => {
    if (!bankQuery.data) return
    setBankForm({
      accountHolderName: bankQuery.data.accountHolderName || '',
      accountNumber: bankQuery.data.accountNumber || '',
      ifscCode: bankQuery.data.ifscCode || '',
      bankName: bankQuery.data.bankName || '',
      upiId: bankQuery.data.upiId || '',
      upiName: bankQuery.data.upiName || '',
    })
    setBankEdit(false)
  }, [bankQuery.data])

  const data = listQuery.data
  const rows = data?.referrals ?? []
  const pages = data ? Math.max(1, Math.ceil(data.totalCount / data.pageSize)) : 1
  const hasSearch = Boolean(searchInput.trim())
  const activeFilterCount = hasSearch ? 1 : 0

  const clearFilters = () => {
    setSearchInput('')
    setSearch('')
    setPage(1)
  }

  const invalidate = () => {
    void qc.invalidateQueries({ queryKey: ['manage-referrals'] })
    void qc.invalidateQueries({ queryKey: ['manage-referrals-details'] })
    void qc.invalidateQueries({ queryKey: ['manage-referrals-history'] })
  }

  const openPayment = (line: ReferrerPaymentLine) => {
    setPaymentTarget({ referralId: line.referralId, amount: line.pendingAmount })
    setPayAmount(String(line.pendingAmount))
    setPayTxn('')
    setPayMethod('')
    setPayNotes('')
  }

  const submitPayment = async () => {
    if (!paymentTarget) return
    const amount = Number(payAmount)
    if (!(amount > 0)) return toast.push('Enter a valid amount', 'error')
    if (!payTxn.trim()) return toast.push('Transaction number is required', 'error')
    if (!payMethod) return toast.push('Select a payment method', 'error')

    setPayBusy(true)
    try {
      const res = await processReferralPayment({
        referralId: paymentTarget.referralId,
        amount,
        transactionNumber: payTxn.trim(),
        paymentMethod: payMethod,
        paymentNotes: payNotes.trim() || undefined,
      })
      toast.push(res.message || 'Payment processed', 'success')
      setPaymentTarget(null)
      invalidate()
    } catch (e) {
      toast.push(e instanceof ApiError ? e.message : 'Payment failed', 'error')
    } finally {
      setPayBusy(false)
    }
  }

  const saveBank = async () => {
    if (!bankEmail) return
    if (!bankForm.accountHolderName.trim() || !bankForm.accountNumber.trim() || !bankForm.ifscCode.trim() || !bankForm.bankName.trim()) {
      return toast.push('Account holder, number, IFSC and bank name are required', 'error')
    }
    setBankBusy(true)
    try {
      const res = await upsertBankDetails({ userEmail: bankEmail, ...bankForm })
      toast.push(res.message || 'Bank details updated', 'success')
      setBankEdit(false)
      await qc.invalidateQueries({ queryKey: ['manage-referrals-bank', bankEmail] })
    } catch (e) {
      toast.push(e instanceof ApiError ? e.message : 'Update failed', 'error')
    } finally {
      setBankBusy(false)
    }
  }

  return (
    <div className="flex h-full min-h-0 min-w-0 max-w-full flex-col gap-3">
      <div className="flex shrink-0 flex-wrap items-end justify-between gap-3">
        <div>
          <h1 className="font-[family-name:var(--font-display)] text-3xl font-semibold tracking-tight">
            Manage Referrals
          </h1>
          <p className="mt-1 text-sm text-muted-foreground">
            {data?.totalCount ?? '—'} referrals · payment & bank details
          </p>
        </div>
        <div className="flex flex-wrap items-center gap-2">
          <FiltersButton activeCount={activeFilterCount} onClick={() => setFiltersOpen(true)} />
          <Button variant="outline" onClick={() => listQuery.refetch()}>
            <RefreshCw size={16} /> Refresh
          </Button>
        </div>
      </div>

      <FiltersDrawer
        open={filtersOpen}
        onOpenChange={setFiltersOpen}
        onClear={activeFilterCount > 0 ? clearFilters : undefined}
      >
        <div className="space-y-1.5">
          <label className="text-xs font-medium text-muted-foreground">Search</label>
          <div className="relative">
            <Search size={16} className="pointer-events-none absolute top-2.5 left-3 text-muted-foreground" />
            <Input
              className="pr-9 pl-9"
              placeholder="Referrer or referred email…"
              value={searchInput}
              onChange={(e) => setSearchInput(e.target.value)}
            />
            {hasSearch && (
              <button
                type="button"
                className="absolute top-2 right-2 rounded-md p-1 text-muted-foreground hover:bg-muted"
                onClick={() => setSearchInput('')}
              >
                <X size={14} />
              </button>
            )}
          </div>
        </div>
      </FiltersDrawer>

      <Card className="flex min-h-0 min-w-0 max-w-full flex-1 flex-col overflow-hidden py-0">
        <CardContent className="flex min-h-0 flex-1 flex-col p-0">
          <div className="min-h-0 flex-1 overflow-auto">
            <Table className="w-max min-w-full">
              <TableHeader className="sticky top-0 z-10 bg-slate-900">
                <TableRow className="border-slate-800 hover:bg-slate-900">
                  {[
                    'USER ID',
                    'User Email',
                    'User Name',
                    'User Number',
                    'Referred to',
                    'Referral Details',
                    'Referral Amt.',
                    'Refund',
                    'MW Payment Status',
                    'Bank Details',
                    'MW Payment Details',
                  ].map((h) => (
                    <TableHead key={h} className="text-xs font-semibold tracking-wide text-slate-200 uppercase">
                      {h}
                    </TableHead>
                  ))}
                </TableRow>
              </TableHeader>
              <TableBody>
                {listQuery.isLoading && (
                  <TableRow>
                    <TableCell colSpan={11} className="py-10 text-center text-muted-foreground">
                      Loading…
                    </TableCell>
                  </TableRow>
                )}
                {listQuery.isError && (
                  <TableRow>
                    <TableCell colSpan={11} className="py-10 text-center text-destructive">
                      {(listQuery.error as Error).message}
                    </TableCell>
                  </TableRow>
                )}
                {!listQuery.isLoading && !listQuery.isError && rows.length === 0 && (
                  <TableRow>
                    <TableCell colSpan={11} className="py-10 text-center text-muted-foreground">
                      No referral records found
                    </TableCell>
                  </TableRow>
                )}
                {rows.map((r) => (
                  <ReferralRow
                    key={r.referralId}
                    row={r}
                    onDetails={() => setDetailsEmail(r.referrerEmail)}
                    onBank={() => setBankEmail(r.referrerEmail)}
                  />
                ))}
              </TableBody>
            </Table>
          </div>

          <div className="flex shrink-0 items-center justify-between border-t px-4 py-3 text-sm">
            <div className="text-muted-foreground">
              Page {data?.page ?? 1} of {pages} · {data?.totalCount ?? 0} · 10 per page
            </div>
            <div className="flex gap-2">
              <Button
                variant="outline"
                size="sm"
                disabled={page <= 1 || listQuery.isFetching}
                onClick={() => setPage((p) => Math.max(1, p - 1))}
              >
                Prev
              </Button>
              <Button
                variant="outline"
                size="sm"
                disabled={page >= pages || listQuery.isFetching}
                onClick={() => setPage((p) => p + 1)}
              >
                Next
              </Button>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Referral details */}
      <Dialog open={!!detailsEmail} onOpenChange={(o) => !o && setDetailsEmail(null)}>
        <DialogContent className="flex max-h-[85vh] max-w-5xl flex-col overflow-hidden">
          <DialogHeader>
            <DialogTitle>Referral Details</DialogTitle>
            <DialogDescription>
              {detailsQuery.data
                ? `${detailsQuery.data.referrerName} (${detailsQuery.data.referrerEmail})`
                : detailsEmail}
            </DialogDescription>
          </DialogHeader>
          <div className="min-h-0 flex-1 overflow-auto">
            {detailsQuery.isLoading && <p className="py-6 text-center text-muted-foreground">Loading…</p>}
            {detailsQuery.isError && (
              <p className="py-6 text-center text-destructive">{(detailsQuery.error as Error).message}</p>
            )}
            {detailsQuery.data && (
              <Table>
                <TableHeader>
                  <TableRow>
                    {[
                      'Referred User',
                      'Date',
                      'User Payment',
                      'Total',
                      'Paid',
                      'Pending',
                      'Status',
                      'Action',
                    ].map((h) => (
                      <TableHead key={h}>{h}</TableHead>
                    ))}
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {detailsQuery.data.lines.length === 0 && (
                    <TableRow>
                      <TableCell colSpan={8} className="py-6 text-center text-muted-foreground">
                        No referral details found
                      </TableCell>
                    </TableRow>
                  )}
                  {detailsQuery.data.lines.map((line) => (
                    <TableRow key={line.referralId}>
                      <TableCell>
                        <div className="font-medium">{line.referredName}</div>
                        <div className="text-xs text-muted-foreground">{line.referredEmail}</div>
                      </TableCell>
                      <TableCell className="whitespace-nowrap">{formatDate(line.referralDate)}</TableCell>
                      <TableCell>
                        <Badge variant={badgeVariantFromTone(line.userPaymentStatusTone)}>
                          {line.userPaymentStatusLabel}
                        </Badge>
                      </TableCell>
                      <TableCell>{formatMoney(line.totalAmount)}</TableCell>
                      <TableCell className="text-emerald-700">{formatMoney(line.paidAmount)}</TableCell>
                      <TableCell className="text-rose-700">{formatMoney(line.pendingAmount)}</TableCell>
                      <TableCell>
                        <Badge variant={badgeVariantFromTone(line.statusTone)}>{line.statusLabel}</Badge>
                      </TableCell>
                      <TableCell>
                        <div className="flex flex-col gap-1">
                          {line.canAddPayment && (
                            <Button size="sm" onClick={() => openPayment(line)}>
                              Add Payment
                            </Button>
                          )}
                          {line.hasHistory && (
                            <Button
                              size="sm"
                              variant="outline"
                              onClick={() => setHistoryId(line.referralId)}
                            >
                              <History size={14} /> History
                            </Button>
                          )}
                        </div>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            )}
          </div>
        </DialogContent>
      </Dialog>

      {/* Bank details */}
      <Dialog
        open={!!bankEmail}
        onOpenChange={(o) => {
          if (!o) {
            setBankEmail(null)
            setBankEdit(false)
          }
        }}
      >
        <DialogContent className="max-w-lg">
          <DialogHeader>
            <DialogTitle>Bank Details</DialogTitle>
            <DialogDescription>{bankEmail}</DialogDescription>
          </DialogHeader>
          {bankQuery.isLoading && <p className="py-4 text-muted-foreground">Loading…</p>}
          {bankQuery.isError && (
            <p className="py-4 text-destructive">{(bankQuery.error as Error).message}</p>
          )}
          {!bankQuery.isLoading && !bankQuery.isError && (
            <div className="grid gap-3">
              {(
                [
                  ['accountHolderName', 'Account Holder Name'],
                  ['accountNumber', 'Account Number'],
                  ['ifscCode', 'IFSC Code'],
                  ['bankName', 'Bank Name'],
                  ['upiId', 'UPI ID'],
                  ['upiName', 'UPI Name'],
                ] as const
              ).map(([key, label]) => (
                <div key={key} className="space-y-1">
                  <label className="text-xs font-medium text-muted-foreground">{label}</label>
                  {bankEdit ? (
                    <Input
                      value={bankForm[key]}
                      onChange={(e) => setBankForm((f) => ({ ...f, [key]: e.target.value }))}
                    />
                  ) : (
                    <div className="rounded-md border bg-muted/30 px-3 py-2 text-sm">
                      {bankForm[key] || '—'}
                    </div>
                  )}
                </div>
              ))}
            </div>
          )}
          <DialogFooter>
            {!bankEdit ? (
              <Button onClick={() => setBankEdit(true)}>Edit</Button>
            ) : (
              <>
                <Button variant="outline" disabled={bankBusy} onClick={() => setBankEdit(false)}>
                  Cancel
                </Button>
                <Button disabled={bankBusy} onClick={() => void saveBank()}>
                  {bankBusy ? 'Saving…' : 'Save'}
                </Button>
              </>
            )}
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Process payment */}
      <Dialog open={!!paymentTarget} onOpenChange={(o) => !o && !payBusy && setPaymentTarget(null)}>
        <DialogContent className="max-w-md">
          <DialogHeader>
            <DialogTitle>Process Referral Payment</DialogTitle>
            <DialogDescription>Record a payment against this referral earning</DialogDescription>
          </DialogHeader>
          <div className="space-y-3">
            <div className="space-y-1">
              <label className="text-xs font-medium text-muted-foreground">Amount (₹)</label>
              <Input type="number" value={payAmount} onChange={(e) => setPayAmount(e.target.value)} />
            </div>
            <div className="space-y-1">
              <label className="text-xs font-medium text-muted-foreground">Transaction Number</label>
              <Input value={payTxn} onChange={(e) => setPayTxn(e.target.value)} placeholder="Reference / UTR" />
            </div>
            <div className="space-y-1">
              <label className="text-xs font-medium text-muted-foreground">Payment Method</label>
              <Select value={payMethod} onValueChange={setPayMethod}>
                <SelectTrigger className="w-full">
                  <SelectValue placeholder="Select method" />
                </SelectTrigger>
                <SelectContent>
                  {['Bank Transfer', 'UPI', 'Cash', 'Cheque', 'Other'].map((m) => (
                    <SelectItem key={m} value={m}>
                      {m}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-1">
              <label className="text-xs font-medium text-muted-foreground">Notes (optional)</label>
              <Textarea value={payNotes} onChange={(e) => setPayNotes(e.target.value)} rows={3} />
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" disabled={payBusy} onClick={() => setPaymentTarget(null)}>
              Cancel
            </Button>
            <Button disabled={payBusy} onClick={() => void submitPayment()}>
              {payBusy ? 'Processing…' : 'Process Payment'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Payment history */}
      <Dialog open={historyId != null} onOpenChange={(o) => !o && setHistoryId(null)}>
        <DialogContent className="max-w-3xl">
          <DialogHeader>
            <DialogTitle>Payment History</DialogTitle>
            <DialogDescription>
              {historyQuery.data
                ? `${historyQuery.data.referrerName} → ${historyQuery.data.referredName} · ${formatMoney(historyQuery.data.referralAmount)}`
                : 'Loading…'}
            </DialogDescription>
          </DialogHeader>
          {historyQuery.isLoading && <p className="py-4 text-muted-foreground">Loading…</p>}
          {historyQuery.isError && (
            <p className="py-4 text-destructive">{(historyQuery.error as Error).message}</p>
          )}
          {historyQuery.data && (
            <div className="overflow-auto">
              {historyQuery.data.items.length === 0 ? (
                <p className="py-4 text-muted-foreground">No payment history found.</p>
              ) : (
                <Table>
                  <TableHeader>
                    <TableRow>
                      {['Date', 'Amount', 'Transaction No.', 'Method', 'Notes', 'Processed By'].map((h) => (
                        <TableHead key={h}>{h}</TableHead>
                      ))}
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {historyQuery.data.items.map((item, i) => (
                      <TableRow key={`${item.transactionNumber}-${i}`}>
                        <TableCell className="whitespace-nowrap">{formatDate(item.paymentDate)}</TableCell>
                        <TableCell>{formatMoney(item.amount)}</TableCell>
                        <TableCell>{item.transactionNumber}</TableCell>
                        <TableCell>{item.paymentMethod}</TableCell>
                        <TableCell>{item.paymentNotes || 'N/A'}</TableCell>
                        <TableCell>{item.processedBy}</TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              )}
            </div>
          )}
        </DialogContent>
      </Dialog>
    </div>
  )
}

function ReferralRow({
  row: r,
  onDetails,
  onBank,
}: {
  row: ManageReferralRow
  onDetails: () => void
  onBank: () => void
}) {
  return (
    <TableRow>
      <TableCell className="font-medium whitespace-nowrap">{r.userIdDisplay}</TableCell>
      <TableCell className="max-w-[200px] truncate">{r.referrerEmail}</TableCell>
      <TableCell>{r.userName}</TableCell>
      <TableCell>{r.userPhone}</TableCell>
      <TableCell className="whitespace-nowrap">{r.referredToDisplay}</TableCell>
      <TableCell>
        <Button size="sm" variant="outline" onClick={onDetails}>
          <Eye size={14} /> View
        </Button>
      </TableCell>
      <TableCell className="font-semibold whitespace-nowrap">{r.referralAmountDisplay}</TableCell>
      <TableCell>{r.refundStatus}</TableCell>
      <TableCell>
        <Badge variant={badgeVariantFromTone(r.mwPaymentStatusTone)}>{r.mwPaymentStatusLabel}</Badge>
      </TableCell>
      <TableCell>
        <Button size="sm" variant="outline" onClick={onBank}>
          View
        </Button>
      </TableCell>
      <TableCell>
        {r.hasInvoice && r.latestCardId ? (
          <Button
            size="sm"
            variant="ghost"
            title="Download invoice"
            onClick={() => openInvoiceByCardId(r.latestCardId!)}
          >
            <Download size={16} />
          </Button>
        ) : (
          <span className="text-muted-foreground">-</span>
        )}
      </TableCell>
    </TableRow>
  )
}
