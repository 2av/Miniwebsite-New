import { useQuery, useQueryClient } from '@tanstack/react-query'
import { Download, Power, PowerOff, RefreshCw, Search, X } from 'lucide-react'
import { useEffect, useMemo, useState } from 'react'
import {
  downloadManageUsersCsv,
  fetchDashboardDetails,
  fetchManageUsers,
  fetchReferralDetails,
  setUserStatus,
  upsertBankDetails,
} from '@/features/manage-users/api'
import type { ManageUserRow, ManageUsersQuery } from '@/shared/types/api'
import { useToast } from '@/shared/ui/Toast'
import { ApiError } from '@/shared/api/client'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Input } from '@/components/ui/input'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import { badgeVariantFromTone } from '@/shared/lib/badgeTone'
import { openInvoiceById } from '@/shared/lib/invoiceDownload'
import { FiltersButton, FiltersDrawer } from '@/shared/ui/FiltersDrawer'

const emptyFilters: ManageUsersQuery = {
  page: 1,
  pageSize: 10,
  search: '',
}

const MONTHS = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec']

function commissionTone(label: string): 'neutral' | 'ok' | 'danger' {
  if (label === 'Eligible') return 'ok'
  if (label === 'Rejected') return 'danger'
  return 'neutral'
}

function statusTone(label: string): 'neutral' | 'ok' {
  return label === 'Active' ? 'ok' : 'neutral'
}

function payoutTone(label: string): 'neutral' | 'ok' | 'danger' | 'warn' {
  if (label.startsWith('Paid')) return 'ok'
  if (label === 'Rejected') return 'danger'
  return 'warn'
}

const COLUMNS = [
  'User ID',
  'User Email',
  'User Name',
  'User Number',
  'Joined On',
  'State',
  'District',
  'Referral Source',
  'User Status',
  'No. of DB',
  'Documents Status',
  'User Dashboard',
  'Total Referred',
  'Total Commission Earning',
  'Referrer Commission Status',
  'SB Payout Status',
  'Partner Invoice Details',
  'Actions',
]

function formatJoined(value?: string | null) {
  if (!value) return ''
  const d = new Date(value)
  if (Number.isNaN(d.getTime())) return ''
  const day = String(d.getDate()).padStart(2, '0')
  const year = String(d.getFullYear()).slice(-2)
  return `${day}-${MONTHS[d.getMonth()]}-${year}`
}

function formatDate(value?: string | null) {
  if (!value) return '-'
  const d = new Date(value)
  if (Number.isNaN(d.getTime())) return '-'
  return d.toLocaleDateString('en-IN', { day: '2-digit', month: 'short', year: 'numeric' })
}

function formatRupee(amount: number) {
  return `₹ ${Math.round(amount).toLocaleString('en-IN')}`
}

export function ManageUsersPage() {
  const toast = useToast()
  const qc = useQueryClient()
  const [filters, setFilters] = useState<ManageUsersQuery>(emptyFilters)
  const [searchInput, setSearchInput] = useState('')
  const [dashboardEmail, setDashboardEmail] = useState<string | null>(null)
  const [referralEmail, setReferralEmail] = useState<string | null>(null)
  const [filtersOpen, setFiltersOpen] = useState(false)
  const [togglingId, setTogglingId] = useState<number | null>(null)

  // Debounce search → API (name / email / phone)
  useEffect(() => {
    const t = window.setTimeout(() => {
      const next = searchInput.trim()
      setFilters((prev) => {
        if ((prev.search || '') === next && (prev.page ?? 1) === 1) return prev
        return { ...prev, search: next || undefined, page: 1 }
      })
    }, 350)
    return () => window.clearTimeout(t)
  }, [searchInput])

  const queryKey = useMemo(() => ['manage-users', filters] as const, [filters])

  const listQuery = useQuery({
    queryKey,
    queryFn: () =>
      fetchManageUsers({
        ...filters,
        search: filters.search || undefined,
      }),
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

  const setStatus = async (user: ManageUserRow) => {
    const next = user.status === 'Active' ? 'INACTIVE' : 'ACTIVE'
    setTogglingId(user.id)
    try {
      const res = await setUserStatus(user.id, next)
      toast.push(res.message || 'Status updated', 'success')
      await qc.invalidateQueries({ queryKey: ['manage-users'] })
    } catch (e) {
      toast.push(e instanceof ApiError ? e.message : 'Status update failed', 'error')
    } finally {
      setTogglingId(null)
    }
  }

  const data = listQuery.data
  const users = data?.users ?? []
  const pages = data ? Math.max(1, Math.ceil(data.totalCount / data.pageSize)) : 1
  const hasSearch = Boolean(searchInput.trim())
  const activeFilterCount = hasSearch ? 1 : 0

  const clearFilters = () => {
    setSearchInput('')
    setFilters((prev) => ({ ...prev, search: undefined, page: 1 }))
  }

  return (
    <div className="flex h-full min-h-0 min-w-0 max-w-full flex-col gap-3">
      <div className="flex shrink-0 flex-wrap items-end justify-between gap-3">
        <div>
          <h1 className="font-[family-name:var(--font-display)] text-3xl font-semibold text-slate-900">SB User Details</h1>
          <p className="mt-1 text-sm text-slate-500">{data?.totalCount ?? '—'} customers</p>
        </div>
        <div className="flex flex-wrap items-center gap-2">
          <FiltersButton activeCount={activeFilterCount} onClick={() => setFiltersOpen(true)} />
          <Button variant="outline" onClick={() => listQuery.refetch()}>
            <RefreshCw size={16} /> Refresh
          </Button>
          <Button
            variant="outline"
            onClick={() =>
              downloadManageUsersCsv({
                ...filters,
                search: filters.search || undefined,
              }).catch((e) => toast.push(e.message, 'error'))
            }
          >
            <Download size={16} /> Export
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
              placeholder="User ID, email, name, or phone…"
              value={searchInput}
              onChange={(e) => setSearchInput(e.target.value)}
              aria-label="Search by email, name, or phone"
            />
            {hasSearch && (
              <button
                type="button"
                className="absolute top-2 right-2 rounded-md p-1 text-muted-foreground hover:bg-muted"
                onClick={() => setSearchInput('')}
                aria-label="Clear search"
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
                {COLUMNS.map((h) => (
                  <TableHead key={h} className="px-3 text-xs font-semibold tracking-wide whitespace-nowrap text-slate-200 uppercase">
                    {h}
                  </TableHead>
                ))}
                </TableRow>
              </TableHeader>
              <TableBody>
              {listQuery.isLoading && (
                <TableRow>
                  <TableCell colSpan={COLUMNS.length} className="px-4 py-10 text-center text-muted-foreground">
                    Loading users…
                  </TableCell>
                </TableRow>
              )}
              {listQuery.isError && (
                <TableRow>
                  <TableCell colSpan={COLUMNS.length} className="px-4 py-10 text-center text-destructive">
                    {(listQuery.error as Error).message}
                    <div className="mt-1 text-xs text-muted-foreground">Start the .NET API on localhost:5209</div>
                  </TableCell>
                </TableRow>
              )}
              {!listQuery.isLoading && !listQuery.isError && users.length === 0 && (
                <TableRow>
                  <TableCell colSpan={COLUMNS.length} className="px-4 py-10 text-center text-muted-foreground">
                    No users found
                  </TableCell>
                </TableRow>
              )}
              {users.map((u) => (
                <UserRow
                  key={u.id}
                  user={u}
                  onDashboard={() => setDashboardEmail(u.email)}
                  onCommission={() => setReferralEmail(u.email)}
                  toggling={togglingId === u.id}
                  onToggleStatus={() => void setStatus(u)}
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
              <Button
                variant="outline"
                size="sm"
                disabled={(filters.page ?? 1) <= 1 || listQuery.isFetching}
                onClick={() => setFilters((f) => ({ ...f, page: Math.max(1, (f.page ?? 1) - 1) }))}
              >
                Prev
              </Button>
              <Button
                variant="outline"
                size="sm"
                disabled={(filters.page ?? 1) >= pages || listQuery.isFetching}
                onClick={() => setFilters((f) => ({ ...f, page: (f.page ?? 1) + 1 }))}
              >
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
            <DialogDescription>ShareBizz details for this customer.</DialogDescription>
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
                      {['SB ID', 'Company', 'Created', 'Validity', 'Status', 'Payment'].map((heading) => (
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
        <DialogContent className="max-h-[85vh] overflow-y-auto sm:max-w-5xl">
          <DialogHeader>
            <DialogTitle>Referrals — {referralEmail ?? ''}</DialogTitle>
            <DialogDescription>Referral earnings, users, and payment details.</DialogDescription>
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

    </div>
  )
}

function UserRow({
  user: u,
  onDashboard,
  onCommission,
  toggling,
  onToggleStatus,
}: {
  user: ManageUserRow
  onDashboard: () => void
  onCommission: () => void
  toggling: boolean
  onToggleStatus: () => void
}) {
  const active = u.status === 'Active'
  return (
    <TableRow className="align-middle hover:bg-rose-50/30">
      <TableCell className="px-3 font-mono font-medium whitespace-nowrap">{u.userCode}</TableCell>
      <TableCell className="max-w-[220px] truncate px-3">{u.email}</TableCell>
      <TableCell className="px-3 whitespace-nowrap">{u.name}</TableCell>
      <TableCell className="px-3 whitespace-nowrap">{u.phone || ''}</TableCell>
      <TableCell className="px-3 whitespace-nowrap">{formatJoined(u.createdAt)}</TableCell>
      <TableCell className="px-3 whitespace-nowrap">{u.state || ''}</TableCell>
      <TableCell className="px-3 whitespace-nowrap">{u.district || ''}</TableCell>
      <TableCell className="px-3 whitespace-nowrap">{u.referralSourceDisplay}</TableCell>
      <TableCell className="px-3">
        <Badge variant={badgeVariantFromTone(statusTone(u.status))}>{u.status}</Badge>
      </TableCell>
      <TableCell className="px-3 text-center font-semibold">{u.websiteCount}</TableCell>
      <TableCell className="px-3 whitespace-nowrap">
        <Badge variant={badgeVariantFromTone(u.documentStatusTone)}>{u.documentStatus || 'Not Submitted'}</Badge>
      </TableCell>
      <TableCell className="px-3">
        <Button variant="outline" size="sm" onClick={onDashboard}>
          View
        </Button>
      </TableCell>
      <TableCell className="px-3 text-center font-semibold">{u.totalReferred ?? 0}</TableCell>
      <TableCell className="px-3 whitespace-nowrap">{formatRupee(u.totalReferralAmount)}</TableCell>
      <TableCell className="px-3 whitespace-nowrap">
        <span className="inline-flex items-center gap-1">
          <Badge variant={badgeVariantFromTone(commissionTone(u.commissionStatus))}>{u.commissionStatus}</Badge>
          <Button variant="link" size="sm" className="h-auto px-1" onClick={onCommission}>
            [View]
          </Button>
        </span>
      </TableCell>
      <TableCell className="px-3 whitespace-nowrap">
        <Badge variant={badgeVariantFromTone(payoutTone(u.payoutStatus || 'Pending'))}>{u.payoutStatus || 'Pending'}</Badge>
      </TableCell>
      <TableCell className="px-3">
        <Button variant="outline" size="sm" disabled={!u.invoiceId} onClick={() => u.invoiceId && openInvoiceById(u.invoiceId)}>
          View
        </Button>
      </TableCell>
      <TableCell className="px-3">
        <Button
          size="icon-sm"
          variant="ghost"
          className={
            active
              ? 'cursor-pointer text-red-600 hover:bg-red-50 hover:text-red-700'
              : 'cursor-pointer text-emerald-600 hover:bg-emerald-50 hover:text-emerald-700'
          }
          title={active ? 'Set InActive' : 'Set Active'}
          aria-label={active ? 'Set InActive' : 'Set Active'}
          disabled={toggling}
          onClick={onToggleStatus}
        >
          {toggling ? (
            <RefreshCw size={14} className="animate-spin" />
          ) : active ? (
            <PowerOff size={14} />
          ) : (
            <Power size={14} />
          )}
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
  onSaveBank: (bank: {
    accountHolderName?: string
    accountNumber?: string
    ifscCode?: string
    bankName?: string
    upiId?: string
    upiName?: string
  }) => Promise<void>
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
          ['Referred SB', String(data.regularReferrals)],
          ['Referred Franchise', String(data.collaborationReferrals)],
        ].map(([k, v]) => (
          <Card key={k}>
            <CardHeader className="gap-1 py-3">
              <div className="text-xs text-muted-foreground">{k}</div>
              <CardTitle className="text-xl">{v}</CardTitle>
            </CardHeader>
          </Card>
        ))}
      </div>

      <Card>
        <CardHeader className="flex-row items-center justify-between">
          <CardTitle>Bank Account Details</CardTitle>
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
        </CardHeader>
        <CardContent className="grid gap-3 md:grid-cols-2">
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
        </CardContent>
      </Card>

      <div className="overflow-x-auto">
        <Table className="w-full min-w-[1000px]">
          <TableHeader>
            <TableRow>
              {['User', 'Email', 'Source', 'Joined', 'SB Status', 'Amount', 'Payment'].map((heading) => (
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
                <TableCell>{r.isCollaboration === 'YES' ? 'Franchisee' : 'ShareBizz'}</TableCell>
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
