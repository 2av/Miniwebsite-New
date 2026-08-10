import { useQuery, useQueryClient } from '@tanstack/react-query'
import { Download, RefreshCw, Search, X } from 'lucide-react'
import { useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { fetchFranchiseDistributors, setInfluencer } from '@/features/franchise-distributors/api'
import {
  fetchDashboardDetails,
  fetchReferralDetails,
  mapDeal,
  removeDeal,
  resetPassword,
  setCollaboration,
  upsertBankDetails,
} from '@/features/manage-users/api'
import type { BankDetails, FranchiseDistributorRow } from '@/shared/types/api'
import { ApiError } from '@/shared/api/client'
import { badgeVariantFromTone } from '@/shared/lib/badgeTone'
import { openInvoiceById } from '@/shared/lib/invoiceDownload'
import { FiltersButton, FiltersDrawer } from '@/shared/ui/FiltersDrawer'
import { useToast } from '@/shared/ui/Toast'
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/components/ui/alert-dialog'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'
import {
  Dialog,
  DialogContent,
  DialogDescription,
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
import { Switch } from '@/components/ui/switch'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'

function formatDate(value?: string | null) {
  if (!value) return '-'
  const d = new Date(value)
  if (Number.isNaN(d.getTime())) return '-'
  return d.toLocaleDateString('en-IN', { day: '2-digit', month: 'short', year: 'numeric' })
}

function cardsSearchUrl(email: string) {
  return `/miniwebsite-details?search=${encodeURIComponent(email)}`
}

type ConfirmState = {
  title: string
  description: string
  actionLabel: string
  destructive?: boolean
  action: () => Promise<void> | void
}

export function FranchiseDistributorsPage() {
  const toast = useToast()
  const qc = useQueryClient()
  const [page, setPage] = useState(1)
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [dashboardEmail, setDashboardEmail] = useState<string | null>(null)
  const [referralEmail, setReferralEmail] = useState<string | null>(null)
  const [resetTarget, setResetTarget] = useState<string | null>(null)
  const [resetPw, setResetPw] = useState('')
  const [resetConfirm, setResetConfirm] = useState('')
  const [confirm, setConfirm] = useState<ConfirmState | null>(null)
  const [confirmBusy, setConfirmBusy] = useState(false)
  const [filtersOpen, setFiltersOpen] = useState(false)

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
  const queryKey = useMemo(() => ['franchise-distributors', filters] as const, [filters])

  const listQuery = useQuery({
    queryKey,
    queryFn: () => fetchFranchiseDistributors(filters),
  })

  const dashboardQuery = useQuery({
    queryKey: ['dashboard-details', dashboardEmail],
    queryFn: () => fetchDashboardDetails(dashboardEmail!),
    enabled: !!dashboardEmail,
  })

  const referralQuery = useQuery({
    queryKey: ['referral-details', referralEmail],
    queryFn: () => fetchReferralDetails(referralEmail!),
    enabled: !!referralEmail,
  })

  const invalidate = () => qc.invalidateQueries({ queryKey: ['franchise-distributors'] })

  const run = async (fn: () => Promise<unknown>, ok: string) => {
    try {
      await fn()
      toast.push(ok, 'success')
      await invalidate()
    } catch (e) {
      toast.push(e instanceof ApiError ? e.message : 'Request failed', 'error')
    }
  }

  const runConfirm = async () => {
    if (!confirm) return
    setConfirmBusy(true)
    try {
      await confirm.action()
      setConfirm(null)
    } finally {
      setConfirmBusy(false)
    }
  }

  const data = listQuery.data
  const users = data?.users ?? []
  const pages = data ? Math.max(1, Math.ceil(data.totalCount / data.pageSize)) : 1
  const hasSearch = Boolean(searchInput.trim())
  const activeFilterCount = hasSearch ? 1 : 0

  const clearFilters = () => {
    setSearchInput('')
    setSearch('')
    setPage(1)
  }

  return (
    <div className="flex h-full min-h-0 min-w-0 max-w-full flex-col gap-3">
      <div className="flex shrink-0 flex-wrap items-end justify-between gap-3">
        <div>
          <h1 className="font-[family-name:var(--font-display)] text-3xl font-semibold text-slate-900">
            Franchisee Distributor
          </h1>
          <p className="mt-1 text-sm text-slate-500">
            Collaboration-enabled customers · {data?.totalCount ?? '—'} total
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
              placeholder="Name / email / phone…"
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
          <div className="min-h-0 flex-1 overflow-auto overscroll-contain">
            <Table className="w-max min-w-full">
              <TableHeader className="sticky top-0 z-10 bg-slate-900">
                <TableRow className="border-slate-800 hover:bg-slate-900">
                {[
                  'User ID',
                  'Email',
                  'Name',
                  'Phone',
                  'Joined',
                  'Referral',
                  'Company',
                  'FRD Status',
                  'Influencer',
                  'Open Cards',
                  'Card Pay',
                  'FRD Fee',
                  'Invoice',
                  'MW',
                  'Pending ₹',
                  'Dashboard',
                  'Referrals',
                  'Deal MW',
                  'Deal FR',
                  'Collab',
                  'Password',
                ].map((h) => (
                  <TableHead key={h} className="px-3 text-xs font-semibold tracking-wide text-slate-200 uppercase">
                    {h}
                  </TableHead>
                ))}
                </TableRow>
              </TableHeader>
              <TableBody>
              {listQuery.isLoading && (
                <TableRow>
                  <TableCell colSpan={21} className="px-4 py-10 text-center text-muted-foreground">
                    Loading…
                  </TableCell>
                </TableRow>
              )}
              {listQuery.isError && (
                <TableRow>
                  <TableCell colSpan={21} className="px-4 py-10 text-center text-destructive">
                    {(listQuery.error as Error).message}
                  </TableCell>
                </TableRow>
              )}
              {!listQuery.isLoading && !listQuery.isError && users.length === 0 && (
                <TableRow>
                  <TableCell colSpan={21} className="px-4 py-10 text-center text-muted-foreground">
                    No collaboration-enabled users found
                  </TableCell>
                </TableRow>
              )}
              {users.map((u) => (
                <Row
                  key={u.id}
                  user={u}
                  mwDeals={data?.mwDeals ?? []}
                  franchiseDeals={data?.franchiseDeals ?? []}
                  onMapDeal={(dealId) => run(() => mapDeal(u.email, dealId), 'Deal mapped')}
                  onRemoveDeal={(id) => run(() => removeDeal(id), 'Deal removed')}
                  onCollab={(yes) =>
                    run(() => setCollaboration(u.email, yes ? 'YES' : 'NO'), 'Collaboration updated')
                  }
                  onInfluencer={(status) => run(() => setInfluencer(u.email, status), 'Influencer updated')}
                  onDashboard={() => setDashboardEmail(u.email)}
                  onReferral={() => setReferralEmail(u.email)}
                  onReset={() => {
                    setResetTarget(u.email)
                    setResetPw('')
                    setResetConfirm('')
                  }}
                  onConfirm={setConfirm}
                />
              ))}
              </TableBody>
            </Table>
          </div>

          <div className="flex shrink-0 items-center justify-between border-t px-4 py-3 text-sm">
            <div className="text-muted-foreground">
              Page {data?.page ?? 1} of {pages} · {data?.totalCount ?? 0} users
            </div>
            <div className="flex gap-2">
              <Button variant="outline" size="sm" disabled={page <= 1} onClick={() => setPage((p) => Math.max(1, p - 1))}>
                Prev
              </Button>
              <Button variant="outline" size="sm" disabled={page >= pages} onClick={() => setPage((p) => p + 1)}>
                Next
              </Button>
            </div>
          </div>
        </CardContent>
      </Card>

      <Dialog open={!!dashboardEmail} onOpenChange={(open) => !open && setDashboardEmail(null)}>
        <DialogContent className="max-h-[85vh] overflow-y-auto sm:max-w-4xl">
          <DialogHeader>
            <DialogTitle>Dashboard — {dashboardEmail ?? ''}</DialogTitle>
            <DialogDescription>Mini website dashboard details for this distributor.</DialogDescription>
          </DialogHeader>
          {dashboardQuery.isLoading && <p className="text-muted-foreground">Loading…</p>}
          {dashboardQuery.isError && <p className="text-destructive">{(dashboardQuery.error as Error).message}</p>}
          {dashboardQuery.data && (
            <div className="overflow-x-auto">
              {dashboardQuery.data.websites.length === 0 ? (
                <p className="rounded-xl bg-sky-50 px-4 py-3 text-sky-900">No websites created yet.</p>
              ) : (
                <Table>
                  <TableHeader>
                    <TableRow>
                      {['MW ID', 'Company', 'Created', 'Validity', 'Status', 'Payment'].map((heading) => (
                        <TableHead key={heading}>{heading}</TableHead>
                      ))}
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {dashboardQuery.data.websites.map((w) => (
                      <TableRow key={w.id}>
                        <TableCell>{w.id}</TableCell>
                        <TableCell>{w.companyName || 'Unnamed'}</TableCell>
                        <TableCell>{formatDate(w.uploadedDate)}</TableCell>
                        <TableCell>{w.validityDisplay}</TableCell>
                        <TableCell>
                          <Badge variant={badgeVariantFromTone(w.statusClass === 'bg-success' ? 'ok' : 'warn')}>
                            {w.statusText}
                          </Badge>
                        </TableCell>
                        <TableCell>{w.paymentLabel}</TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              )}
            </div>
          )}
        </DialogContent>
      </Dialog>

      <Dialog open={!!referralEmail} onOpenChange={(open) => !open && setReferralEmail(null)}>
        <DialogContent className="max-h-[85vh] overflow-y-auto sm:max-w-6xl">
          <DialogHeader>
            <DialogTitle>Referrals — {referralEmail ?? ''}</DialogTitle>
            <DialogDescription>Referral earnings, bank details, and referred users.</DialogDescription>
          </DialogHeader>
          {referralQuery.isLoading && <p className="text-muted-foreground">Loading…</p>}
          {referralQuery.isError && <p className="text-destructive">{(referralQuery.error as Error).message}</p>}
          {referralQuery.data && (
            <ReferralPanel
              data={referralQuery.data}
              onSaveBank={async (bank) => {
                try {
                  await upsertBankDetails({ userEmail: referralQuery.data.referrerEmail, ...bank })
                  toast.push('Bank details saved', 'success')
                  await qc.invalidateQueries({ queryKey: ['referral-details', referralEmail] })
                } catch (e) {
                  toast.push(e instanceof ApiError ? e.message : 'Failed', 'error')
                }
              }}
            />
          )}
        </DialogContent>
      </Dialog>

      <Dialog open={!!resetTarget} onOpenChange={(open) => !open && setResetTarget(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Reset password</DialogTitle>
            <DialogDescription>
              Updating password for <strong>{resetTarget}</strong>
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-3">
            <Input type="password" placeholder="New password (min 6)" value={resetPw} onChange={(e) => setResetPw(e.target.value)} />
            <Input type="password" placeholder="Confirm password" value={resetConfirm} onChange={(e) => setResetConfirm(e.target.value)} />
            <Button
              variant="destructive"
              onClick={() => {
                if (!resetTarget) return
                if (resetPw.length < 6) return toast.push('Password must be at least 6 characters', 'error')
                if (resetPw !== resetConfirm) return toast.push('Passwords do not match', 'error')
                const email = resetTarget
                setConfirm({
                  title: 'Confirm password reset',
                  description: `Reset the password for ${email}?`,
                  actionLabel: 'Reset Password',
                  destructive: true,
                  action: async () => {
                    await run(() => resetPassword(email, resetPw), 'Password updated')
                    setResetTarget(null)
                  },
                })
              }}
            >
              Update Password
            </Button>
          </div>
        </DialogContent>
      </Dialog>

      <AlertDialog open={!!confirm} onOpenChange={(open) => !open && !confirmBusy && setConfirm(null)}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>{confirm?.title}</AlertDialogTitle>
            <AlertDialogDescription>{confirm?.description}</AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel disabled={confirmBusy}>Cancel</AlertDialogCancel>
            <AlertDialogAction
              variant={confirm?.destructive ? 'destructive' : 'default'}
              disabled={confirmBusy}
              onClick={(event) => {
                event.preventDefault()
                void runConfirm()
              }}
            >
              {confirmBusy ? 'Please wait…' : confirm?.actionLabel}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  )
}

function Row({
  user: u,
  mwDeals,
  franchiseDeals,
  onMapDeal,
  onRemoveDeal,
  onCollab,
  onInfluencer,
  onDashboard,
  onReferral,
  onReset,
  onConfirm,
}: {
  user: FranchiseDistributorRow
  mwDeals: { id: number; dealName: string }[]
  franchiseDeals: { id: number; dealName: string }[]
  onMapDeal: (dealId: number) => void
  onRemoveDeal: (mappingId: number) => void
  onCollab: (yes: boolean) => void
  onInfluencer: (status: string) => void
  onDashboard: () => void
  onReferral: () => void
  onReset: () => void
  onConfirm: (confirm: ConfirmState) => void
}) {
  const invoiceId = u.franchiseInvoiceId || u.joiningDealInvoiceId

  return (
    <TableRow className="hover:bg-rose-50/30">
      <TableCell className="px-3 py-3 font-medium">{u.id}</TableCell>
      <TableCell className="max-w-[180px] truncate px-3 py-3">{u.email}</TableCell>
      <TableCell className="px-3 py-3">{u.name}</TableCell>
      <TableCell className="px-3 py-3">{u.phone || '-'}</TableCell>
      <TableCell className="px-3 py-3 text-muted-foreground">{formatDate(u.createdAt)}</TableCell>
      <TableCell className="px-3 py-3">{u.referralSourceDisplay}</TableCell>
      <TableCell className="max-w-[140px] truncate px-3 py-3">{u.companyName}</TableCell>
      <TableCell className="px-3 py-3">
        <Badge variant={badgeVariantFromTone(u.frdStatusLabel === 'Active' ? 'ok' : 'neutral')}>
          {u.frdStatusLabel}
        </Badge>
      </TableCell>
      <TableCell className="min-w-[100px] px-3 py-3">
        <Select
          value={u.influencer}
          onValueChange={(value) => {
            if (value === u.influencer) return
            onConfirm({
              title: 'Confirm influencer status',
              description: `${value === 'YES' ? 'Enable' : 'Disable'} influencer for ${u.email}?`,
              actionLabel: value === 'YES' ? 'Enable' : 'Disable',
              action: () => onInfluencer(value),
            })
          }}
        >
          <SelectTrigger className="w-full">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="NO">No</SelectItem>
            <SelectItem value="YES">Yes</SelectItem>
          </SelectContent>
        </Select>
      </TableCell>
      <TableCell className="px-3 py-3">
        <Link to={cardsSearchUrl(u.email)} className="text-sm font-semibold text-rose-600 hover:underline">
          Open
        </Link>
      </TableCell>
      <TableCell className="px-3 py-3">{u.cardPaymentStatus}</TableCell>
      <TableCell className="px-3 py-3">{u.frdFeeDisplay}</TableCell>
      <TableCell className="px-3 py-3">
        {invoiceId ? (
          <Button
            variant="ghost"
            size="icon-sm"
            title="Download invoice"
            onClick={() => openInvoiceById(invoiceId)}
          >
            <Download size={16} />
          </Button>
        ) : (
          <span className="text-slate-300">-</span>
        )}
      </TableCell>
      <TableCell className="px-3 py-3 text-center font-semibold">{u.websiteCount}</TableCell>
      <TableCell className="px-3 py-3">₹{Math.round(u.pendingReferralAmount).toLocaleString('en-IN')}</TableCell>
      <TableCell className="px-3 py-3">
        <Button variant="ghost" size="sm" onClick={onDashboard}>
          View
        </Button>
      </TableCell>
      <TableCell className="px-3 py-3">
        <Button variant="ghost" size="sm" onClick={onReferral}>
          View
        </Button>
      </TableCell>
      <TableCell className="min-w-[150px] px-3 py-3">
        {u.mwDeal ? (
          <Badge variant="destructive">
            {u.mwDeal.dealName.slice(0, 14)}…
            <button
              type="button"
              aria-label={`Remove ${u.mwDeal.dealName}`}
              onClick={() =>
                onConfirm({
                  title: 'Remove deal mapping',
                  description: `Remove "${u.mwDeal!.dealName}" from ${u.email}?`,
                  actionLabel: 'Remove Deal',
                  destructive: true,
                  action: () => onRemoveDeal(u.mwDeal!.mappingId),
                })
              }
            >
              ×
            </button>
          </Badge>
        ) : (
          <Select
            value=""
            onValueChange={(value) => {
              const id = Number(value)
              if (!id) return
              const name = mwDeals.find((deal) => deal.id === id)?.dealName || 'deal'
              onConfirm({
                title: 'Map Mini Website deal',
                description: `Map "${name}" to ${u.email} for Mini Website?`,
                actionLabel: 'Map Deal',
                action: () => onMapDeal(id),
              })
            }}
          >
            <SelectTrigger className="w-full">
              <SelectValue placeholder="Select Deal" />
            </SelectTrigger>
            <SelectContent>
              {mwDeals.map((d) => (
                <SelectItem key={d.id} value={String(d.id)}>
                  {d.dealName.slice(0, 24)}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        )}
      </TableCell>
      <TableCell className="min-w-[150px] px-3 py-3">
        {u.franchiseDeal ? (
          <Badge variant="secondary">
            {u.franchiseDeal.dealName.slice(0, 14)}…
            <button
              type="button"
              aria-label={`Remove ${u.franchiseDeal.dealName}`}
              onClick={() =>
                onConfirm({
                  title: 'Remove deal mapping',
                  description: `Remove "${u.franchiseDeal!.dealName}" from ${u.email}?`,
                  actionLabel: 'Remove Deal',
                  destructive: true,
                  action: () => onRemoveDeal(u.franchiseDeal!.mappingId),
                })
              }
            >
              ×
            </button>
          </Badge>
        ) : (
          <Select
            value=""
            onValueChange={(value) => {
              const id = Number(value)
              if (!id) return
              const name = franchiseDeals.find((deal) => deal.id === id)?.dealName || 'deal'
              onConfirm({
                title: 'Map Franchise deal',
                description: `Map "${name}" to ${u.email} for Franchise?`,
                actionLabel: 'Map Deal',
                action: () => onMapDeal(id),
              })
            }}
          >
            <SelectTrigger className="w-full">
              <SelectValue placeholder="Select Deal" />
            </SelectTrigger>
            <SelectContent>
              {franchiseDeals.map((d) => (
                <SelectItem key={d.id} value={String(d.id)}>
                  {d.dealName.slice(0, 24)}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        )}
      </TableCell>
      <TableCell className="px-3 py-3 text-center">
        <Switch
          checked={u.collaborationEnabled === 'YES'}
          onCheckedChange={(checked) => {
            onConfirm({
              title: 'Confirm collaboration status',
              description: `${checked ? 'Enable' : 'Disable'} collaboration for ${u.email}?`,
              actionLabel: checked ? 'Enable' : 'Disable',
              action: () => onCollab(checked),
            })
          }}
          aria-label={`Collaboration for ${u.email}`}
        />
      </TableCell>
      <TableCell className="px-3 py-3">
        <Button variant="ghost" size="sm" onClick={onReset}>
          Reset
        </Button>
      </TableCell>
    </TableRow>
  )
}

function ReferralPanel({
  data,
  onSaveBank,
}: {
  data: NonNullable<Awaited<ReturnType<typeof fetchReferralDetails>>>
  onSaveBank: (bank: BankDetails) => Promise<void>
}) {
  const [edit, setEdit] = useState(false)
  const [bank, setBank] = useState({
    bankName: data.bank.bankName || '',
    accountHolderName: data.bank.accountHolderName || '',
    accountNumber: data.bank.accountNumber || '',
    ifscCode: data.bank.ifscCode || '',
    upiId: data.bank.upiId || '',
    upiName: data.bank.upiName || '',
  })

  return (
    <div className="space-y-4">
      <div>
        <div className="text-lg font-semibold">{data.userName}</div>
        <div className="text-sm text-slate-500">{data.referrerEmail}</div>
      </div>
      <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
        {[
          ['Pending', `₹${Math.round(data.pendingAmount).toLocaleString('en-IN')}`],
          ['Total Earning', `₹${Math.round(data.totalReferralAmount).toLocaleString('en-IN')}`],
          ['Referred MW', String(data.regularReferrals)],
          ['Referred Franchise', String(data.collaborationReferrals)],
        ].map(([k, v]) => (
          <Card key={k} size="sm">
            <CardContent>
              <div className="text-xs text-muted-foreground">{k}</div>
              <div className="mt-1 text-xl font-semibold">{v}</div>
            </CardContent>
          </Card>
        ))}
      </div>

      <Card>
        <CardContent>
          <div className="mb-3 flex items-center justify-between">
            <h4 className="font-semibold">Bank Account Details</h4>
            {!edit ? (
              <Button variant="outline" onClick={() => setEdit(true)}>
                Edit
              </Button>
            ) : (
              <Button
                onClick={async () => {
                  await onSaveBank(bank)
                  setEdit(false)
                }}
              >
                Save
              </Button>
            )}
          </div>
          <div className="grid gap-3 md:grid-cols-2">
            {(
              [
                ['bankName', 'Bank Name'],
                ['accountHolderName', 'Account Holder'],
                ['accountNumber', 'Account No.'],
                ['ifscCode', 'IFSC'],
                ['upiId', 'UPI ID'],
                ['upiName', 'UPI Name'],
              ] as const
            ).map(([key, label]) => (
              <label key={key} className="text-sm">
                <span className="mb-1 block text-muted-foreground">{label}</span>
                {edit ? (
                  <Input value={bank[key]} onChange={(e) => setBank((b) => ({ ...b, [key]: e.target.value }))} />
                ) : (
                  <div className="font-medium">{bank[key] || '-'}</div>
                )}
              </label>
            ))}
          </div>
        </CardContent>
      </Card>

      <div className="overflow-x-auto">
        <Table className="min-w-[1000px]">
          <TableHeader>
            <TableRow>
              {['User', 'Email', 'Source', 'Joined', 'MW Status', 'Amount', 'Payment'].map((heading) => (
                <TableHead key={heading}>{heading}</TableHead>
              ))}
            </TableRow>
          </TableHeader>
          <TableBody>
            {data.referredUsers.length === 0 && (
              <TableRow>
                <TableCell colSpan={7} className="py-6 text-muted-foreground">
                  No referrals found
                </TableCell>
              </TableRow>
            )}
            {data.referredUsers.map((r) => (
              <TableRow key={r.referralId}>
                <TableCell>{r.userName || 'Unknown'}</TableCell>
                <TableCell>{r.referredEmail}</TableCell>
                <TableCell>{r.isCollaboration === 'YES' ? 'Franchisee' : 'MiniWebsite'}</TableCell>
                <TableCell>{formatDate(r.referralDate)}</TableCell>
                <TableCell>{r.mwStatusText}</TableCell>
                <TableCell>₹{Math.round(r.amount).toLocaleString('en-IN')}</TableCell>
                <TableCell>{r.paymentStatus === 'Success' ? 'Paid' : 'Pending'}</TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </div>
    </div>
  )
}
