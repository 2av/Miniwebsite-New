import { useQuery, useQueryClient } from '@tanstack/react-query'
import { BatteryCharging, KeyRound, Pencil, Plus, Power, PowerOff, RefreshCw, Search, X } from 'lucide-react'
import { useEffect, useMemo, useState, type ReactNode } from 'react'
import { Navigate, useParams } from 'react-router-dom'
import { fetchDashboardDetails, fetchReferralDetails } from '@/features/manage-users/api'
import {
  createSalesChannelPartner,
  fetchSalesChannelPartners,
  resetSalesChannelPartnerPassword,
  toggleSalesChannelPartnerStatus,
  updateSalesChannelPartner,
} from '@/features/sales-channel-partners/api'
import {
  SALES_CHANNEL_PARTNER_BASE,
  SALES_CHANNEL_PARTNERS,
  findSalesChannelPartner,
  type SalesChannelPartnerType,
} from '@/features/sales-channel-partners/partners'
import {
  RechargeWalletDialog,
  type RechargeWalletTarget,
} from '@/features/wallet-recharge/RechargeWalletDialog'
import type { SalesChannelPartnerRow } from '@/shared/types/api'
import { ApiError } from '@/shared/api/client'
import { badgeVariantFromTone } from '@/shared/lib/badgeTone'
import { openInvoiceById } from '@/shared/lib/invoiceDownload'
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
import {
  Drawer,
  DrawerClose,
  DrawerContent,
  DrawerDescription,
  DrawerFooter,
  DrawerHeader,
  DrawerTitle,
} from '@/components/ui/drawer'
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

type FormState = {
  firstName: string
  lastName: string
  email: string
  phone: string
  district: string
  state: string
  password: string
}

const emptyForm: FormState = {
  firstName: '',
  lastName: '',
  email: '',
  phone: '',
  district: '',
  state: '',
  password: '',
}

const ACTION_BTN =
  'cursor-pointer transition-transform duration-150 ease-out hover:scale-115 active:scale-95 disabled:cursor-not-allowed disabled:hover:scale-100'

const COLUMNS = ['User ID', 'Name', 'Email', 'Mobile', 'District', 'State', 'Status', 'Created', 'Actions']

const DMP_COLUMNS = [
  'User ID',
  'User Email',
  'User Name',
  'User Number',
  'Joined On',
  'State',
  'District',
  'Referral Source',
  'Date Created',
  'Expiry Date',
  'User Status',
  'User Payment Status',
  'Joining Fees',
  'Promo Discount',
  'Bill Details',
  'No. of DB',
  'Documents Status',
  'Total Referred',
  'Total Referral Earning',
  'Commission Status',
  'User Dashboard',
  'Refund',
  'Actions',
]

const COUPON_COLUMNS = [
  'User ID',
  'User Email',
  'User Name',
  'User Number',
  'Joined On',
  'State',
  'District',
  'Referral Source',
  'User Status',
  'User Payment Status',
  'Joining Fees',
  'Coupon Code',
  'Bill Details',
  'No. of DB',
  'Documents Status',
  'Total Referred',
  'Total Referral Earning',
  'Commission Status',
  'User Dashboard',
  'Refund',
  'Actions',
]

function commissionTone(label: string): 'neutral' | 'ok' | 'danger' {
  if (label === 'Eligible') return 'ok'
  if (label === 'Rejected') return 'danger'
  return 'neutral'
}

function paymentTone(label: string): 'neutral' | 'ok' {
  return label.startsWith('Paid') ? 'ok' : 'neutral'
}

export function SalesChannelPartnerRoute() {
  const { slug } = useParams()
  const partner = findSalesChannelPartner(slug)
  if (!partner) return <Navigate to={`${SALES_CHANNEL_PARTNER_BASE}/${SALES_CHANNEL_PARTNERS[0].slug}`} replace />
  return <SalesChannelPartnerPage key={partner.slug} partner={partner} />
}

function SalesChannelPartnerPage({ partner }: { partner: SalesChannelPartnerType }) {
  const toast = useToast()
  const qc = useQueryClient()
  const slug = partner.slug

  const [page, setPage] = useState(1)
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [status, setStatus] = useState('all')
  const [filtersOpen, setFiltersOpen] = useState(false)

  const [formOpen, setFormOpen] = useState(false)
  const [editing, setEditing] = useState<SalesChannelPartnerRow | null>(null)
  const [form, setForm] = useState<FormState>(emptyForm)
  const [formBusy, setFormBusy] = useState(false)

  const [resetTarget, setResetTarget] = useState<SalesChannelPartnerRow | null>(null)
  const [newPassword, setNewPassword] = useState('')
  const [resetBusy, setResetBusy] = useState(false)
  const [togglingId, setTogglingId] = useState<number | null>(null)
  const [rechargeTarget, setRechargeTarget] = useState<RechargeWalletTarget | null>(null)
  const [dashboardEmail, setDashboardEmail] = useState<string | null>(null)
  const [referralEmail, setReferralEmail] = useState<string | null>(null)
  const [documents, setDocuments] = useState<SalesChannelPartnerRow | null>(null)
  const canRechargeWallet = slug === 'franchisee-partner'
  const isDetailed =
    slug === 'digital-marketing-partner' ||
    slug === 'print-shop-partner' ||
    slug === 'field-sales-partner'
  const isCoupon =
    slug === 'social-media-partner' ||
    slug === 'field-sales-team' ||
    slug === 'franchisee-partner'
  const columns = isDetailed ? DMP_COLUMNS : isCoupon ? COUPON_COLUMNS : COLUMNS

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

  const filters = useMemo(
    () => ({
      page,
      pageSize: 10,
      search: search || undefined,
      status: status === 'all' ? undefined : status,
    }),
    [page, search, status],
  )

  const listQuery = useQuery({
    queryKey: ['sales-channel-partners', slug, filters],
    queryFn: () => fetchSalesChannelPartners(slug, filters),
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

  const data = listQuery.data
  const role = data?.role
  const roleName = role?.roleName || partner.label
  const partners = data?.partners ?? []
  const pages = data ? Math.max(1, Math.ceil(data.totalCount / data.pageSize)) : 1
  const hasSearch = Boolean(searchInput.trim())
  const activeFilterCount = (hasSearch ? 1 : 0) + (status !== 'all' ? 1 : 0)

  const invalidate = () => qc.invalidateQueries({ queryKey: ['sales-channel-partners', slug] })

  const clearFilters = () => {
    setSearchInput('')
    setSearch('')
    setStatus('all')
    setPage(1)
  }

  const openCreate = () => {
    setEditing(null)
    setForm({ ...emptyForm })
    setFormOpen(true)
  }

  const openEdit = (row: SalesChannelPartnerRow) => {
    setEditing(row)
    setForm({
      firstName: row.firstName,
      lastName: row.lastName,
      email: row.email,
      phone: row.phone,
      district: row.district,
      state: row.state,
      password: '',
    })
    setFormOpen(true)
  }

  const submitForm = async () => {
    if (!form.firstName.trim()) return toast.push('First Name is required', 'error')
    if (!form.email.trim() || !form.email.includes('@')) return toast.push('A valid email is required', 'error')
    if (!editing && form.password.trim().length < 6)
      return toast.push('Password must be at least 6 characters', 'error')

    const payload = {
      firstName: form.firstName.trim(),
      lastName: form.lastName.trim(),
      email: form.email.trim(),
      phone: form.phone.trim(),
      district: form.district.trim(),
      state: form.state.trim(),
    }

    setFormBusy(true)
    try {
      const res = editing
        ? await updateSalesChannelPartner(slug, editing.id, payload)
        : await createSalesChannelPartner(slug, { ...payload, password: form.password })
      toast.push(res.message || (editing ? `${roleName} updated` : `${roleName} added`), 'success')
      setFormOpen(false)
      await invalidate()
    } catch (e) {
      toast.push(e instanceof ApiError ? e.message : 'Save failed', 'error')
    } finally {
      setFormBusy(false)
    }
  }

  const runToggle = async (row: SalesChannelPartnerRow) => {
    setTogglingId(row.id)
    try {
      const next = row.status === 'ACTIVE' ? 'INACTIVE' : 'ACTIVE'
      const res = await toggleSalesChannelPartnerStatus(slug, row.id, next)
      toast.push(res.message || 'Status updated', 'success')
      await invalidate()
    } catch (e) {
      toast.push(e instanceof ApiError ? e.message : 'Status update failed', 'error')
    } finally {
      setTogglingId(null)
    }
  }

  const runReset = async () => {
    if (!resetTarget) return
    if (newPassword.trim().length < 6) return toast.push('Password must be at least 6 characters', 'error')
    setResetBusy(true)
    try {
      const res = await resetSalesChannelPartnerPassword(slug, resetTarget.id, newPassword.trim())
      toast.push(res.message || 'Password reset', 'success')
      setResetTarget(null)
      setNewPassword('')
    } catch (e) {
      toast.push(e instanceof ApiError ? e.message : 'Reset failed', 'error')
    } finally {
      setResetBusy(false)
    }
  }

  const Icon = partner.icon

  return (
    <div className="flex h-full min-h-0 min-w-0 max-w-full flex-col gap-3">
      <div className="flex shrink-0 flex-wrap items-end justify-between gap-3">
        <div>
          <h1 className="font-[family-name:var(--font-display)] flex items-center gap-2 text-3xl font-semibold tracking-tight">
            <Icon size={28} className="text-rose-600" />
            {roleName}
            {role?.prefix && <Badge variant="secondary">{role.prefix}</Badge>}
          </h1>
          <p className="mt-1 text-sm text-muted-foreground">
            {data?.totalCount ?? '—'} partners
            {role?.salesType ? ` · ${role.salesType}` : ''}
          </p>
        </div>
        <div className="flex flex-wrap items-center gap-2">
          <FiltersButton activeCount={activeFilterCount} onClick={() => setFiltersOpen(true)} />
          <Button variant="outline" onClick={() => listQuery.refetch()} disabled={listQuery.isFetching}>
            <RefreshCw size={16} /> Refresh
          </Button>
          <Button onClick={openCreate} disabled={!role}>
            <Plus size={16} /> Add {roleName}
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
              placeholder="User ID, name, email, phone, district…"
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
        <div className="space-y-1.5">
          <label className="text-xs font-medium text-muted-foreground">Status</label>
          <Select
            value={status}
            onValueChange={(v) => {
              setStatus(v)
              setPage(1)
            }}
          >
            <SelectTrigger className="w-full">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All</SelectItem>
              <SelectItem value="ACTIVE">Active</SelectItem>
              <SelectItem value="INACTIVE">Inactive</SelectItem>
            </SelectContent>
          </Select>
        </div>
      </FiltersDrawer>

      <Card className="flex min-h-0 min-w-0 max-w-full flex-1 flex-col overflow-hidden py-0">
        <CardContent className="flex min-h-0 flex-1 flex-col p-0">
          <div className="min-h-0 flex-1 overflow-auto">
            <Table className="w-max min-w-full">
              <TableHeader className="sticky top-0 z-10 bg-slate-900">
                <TableRow className="border-slate-800 hover:bg-slate-900">
                  {columns.map((h) => (
                    <TableHead key={h} className="text-xs font-semibold tracking-wide text-slate-200 uppercase">
                      {h}
                    </TableHead>
                  ))}
                </TableRow>
              </TableHeader>
              <TableBody>
                {listQuery.isLoading && (
                  <TableRow>
                    <TableCell colSpan={columns.length} className="py-10 text-center text-muted-foreground">
                      Loading…
                    </TableCell>
                  </TableRow>
                )}
                {listQuery.isError && (
                  <TableRow>
                    <TableCell colSpan={columns.length} className="py-10 text-center text-destructive">
                      {(listQuery.error as Error).message}
                    </TableCell>
                  </TableRow>
                )}
                {!listQuery.isLoading && !listQuery.isError && partners.length === 0 && (
                  <TableRow>
                    <TableCell colSpan={columns.length} className="py-10 text-center text-muted-foreground">
                      No {roleName.toLowerCase()} records found
                    </TableCell>
                  </TableRow>
                )}
                {partners.map((p) => isDetailed || isCoupon ? (
                  <DmpPartnerRow
                    key={p.id}
                    partner={p}
                    showCoupon={isCoupon}
                    showWallet={canRechargeWallet}
                    toggling={togglingId === p.id}
                    onEdit={() => openEdit(p)}
                    onToggle={() => void runToggle(p)}
                    onReset={() => {
                      setResetTarget(p)
                      setNewPassword('')
                    }}
                    onBill={() => p.invoiceId && openInvoiceById(p.invoiceId)}
                    onDocuments={() => setDocuments(p)}
                    onCommission={() => setReferralEmail(p.email)}
                    onDashboard={() => setDashboardEmail(p.email)}
                    onRecharge={() => setRechargeTarget({ id: p.id, email: p.email, name: p.name })}
                  />
                ) : (
                  <TableRow key={p.id}>
                    <TableCell className="font-mono font-medium">{p.userCode}</TableCell>
                    <TableCell className="font-medium">{p.name}</TableCell>
                    <TableCell className="max-w-[220px]">
                      <div className="truncate">{p.email}</div>
                    </TableCell>
                    <TableCell>{p.phone || '—'}</TableCell>
                    <TableCell>{p.district || '—'}</TableCell>
                    <TableCell>{p.state || '—'}</TableCell>
                    <TableCell>
                      <Badge variant={badgeVariantFromTone(p.statusTone)}>{p.status}</Badge>
                    </TableCell>
                    <TableCell>{p.createdAtDisplay}</TableCell>
                    <TableCell>
                      <div className="flex gap-1">
                        <Button
                          size="icon-sm"
                          variant="ghost"
                          className={ACTION_BTN}
                          title="Edit"
                          aria-label="Edit"
                          onClick={() => openEdit(p)}
                        >
                          <Pencil size={14} />
                        </Button>
                        <Button
                          size="icon-sm"
                          variant="ghost"
                          className={
                            p.status === 'ACTIVE'
                              ? `${ACTION_BTN} text-red-600 hover:bg-red-50 hover:text-red-700`
                              : `${ACTION_BTN} text-emerald-600 hover:bg-emerald-50 hover:text-emerald-700`
                          }
                          title={p.status === 'ACTIVE' ? 'Deactivate' : 'Activate'}
                          aria-label={p.status === 'ACTIVE' ? 'Deactivate' : 'Activate'}
                          disabled={togglingId === p.id}
                          onClick={() => void runToggle(p)}
                        >
                          {togglingId === p.id ? (
                            <RefreshCw size={14} className="animate-spin" />
                          ) : p.status === 'ACTIVE' ? (
                            <PowerOff size={14} />
                          ) : (
                            <Power size={14} />
                          )}
                        </Button>
                        <Button
                          size="icon-sm"
                          variant="ghost"
                          className={ACTION_BTN}
                          title="Reset password"
                          aria-label="Reset password"
                          onClick={() => {
                            setResetTarget(p)
                            setNewPassword('')
                          }}
                        >
                          <KeyRound size={14} />
                        </Button>
                        {canRechargeWallet && (
                          <Button
                            size="icon-sm"
                            variant="ghost"
                            className={`${ACTION_BTN} text-amber-600 hover:bg-amber-50 hover:text-amber-700`}
                            title="Recharge Wallet"
                            aria-label="Recharge Wallet"
                            onClick={() => setRechargeTarget({ id: p.id, email: p.email, name: p.name })}
                          >
                            <BatteryCharging size={14} />
                          </Button>
                        )}
                      </div>
                    </TableCell>
                  </TableRow>
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

      <Drawer
        open={formOpen}
        onOpenChange={(o) => {
          if (!formBusy) setFormOpen(o)
        }}
        direction="right"
      >
        <DrawerContent className="data-[vaul-drawer-direction=right]:w-full data-[vaul-drawer-direction=right]:sm:max-w-md">
          <DrawerHeader>
            <DrawerTitle>{editing ? `Edit ${roleName}` : `Add ${roleName}`}</DrawerTitle>
            <DrawerDescription>
              Role is set automatically to <strong>{roleName}</strong>
              {role?.prefix ? ` (${role.prefix})` : ''}
            </DrawerDescription>
          </DrawerHeader>
          <div className="space-y-3 px-4 pb-2">
            <Field label="Role">
              <Input value={role ? `${role.roleName} (${role.prefix})` : roleName} disabled />
            </Field>
            <div className="grid grid-cols-2 gap-3">
              <Field label="First Name *">
                <Input
                  value={form.firstName}
                  onChange={(e) => setForm((f) => ({ ...f, firstName: e.target.value }))}
                />
              </Field>
              <Field label="Last Name">
                <Input
                  value={form.lastName}
                  onChange={(e) => setForm((f) => ({ ...f, lastName: e.target.value }))}
                />
              </Field>
            </div>
            <Field label="Email *">
              <Input
                type="email"
                value={form.email}
                onChange={(e) => setForm((f) => ({ ...f, email: e.target.value }))}
              />
            </Field>
            <Field label="Mobile">
              <Input value={form.phone} onChange={(e) => setForm((f) => ({ ...f, phone: e.target.value }))} />
            </Field>
            <div className="grid grid-cols-2 gap-3">
              <Field label="District">
                <Input
                  value={form.district}
                  onChange={(e) => setForm((f) => ({ ...f, district: e.target.value }))}
                />
              </Field>
              <Field label="State">
                <Input value={form.state} onChange={(e) => setForm((f) => ({ ...f, state: e.target.value }))} />
              </Field>
            </div>
            {!editing && (
              <Field label="Password *">
                <Input
                  type="password"
                  value={form.password}
                  onChange={(e) => setForm((f) => ({ ...f, password: e.target.value }))}
                  placeholder="Minimum 6 characters"
                />
              </Field>
            )}
          </div>
          <DrawerFooter>
            <Button onClick={() => void submitForm()} disabled={formBusy}>
              {formBusy ? 'Saving…' : editing ? 'Update' : 'Create'}
            </Button>
            <DrawerClose asChild>
              <Button variant="outline" disabled={formBusy}>
                Cancel
              </Button>
            </DrawerClose>
          </DrawerFooter>
        </DrawerContent>
      </Drawer>

      <Dialog open={!!resetTarget} onOpenChange={(o) => !o && !resetBusy && setResetTarget(null)}>
        <DialogContent className="max-w-md">
          <DialogHeader>
            <DialogTitle>Reset Password</DialogTitle>
            <DialogDescription>{resetTarget?.name}</DialogDescription>
          </DialogHeader>
          <Field label="New password *">
            <Input
              type="password"
              value={newPassword}
              onChange={(e) => setNewPassword(e.target.value)}
              placeholder="Minimum 6 characters"
            />
          </Field>
          <DialogFooter>
            <Button variant="outline" onClick={() => setResetTarget(null)} disabled={resetBusy}>
              Cancel
            </Button>
            <Button onClick={() => void runReset()} disabled={resetBusy}>
              {resetBusy ? 'Saving…' : 'Reset'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <RechargeWalletDialog target={rechargeTarget} onClose={() => setRechargeTarget(null)} />

      <Dialog open={!!dashboardEmail} onOpenChange={(open) => !open && setDashboardEmail(null)}>
        <DialogContent className="max-h-[85vh] overflow-y-auto sm:max-w-4xl">
          <DialogHeader>
            <DialogTitle>Dashboard — {dashboardEmail ?? ''}</DialogTitle>
            <DialogDescription>ShareBizz profiles for this partner.</DialogDescription>
          </DialogHeader>
          {dashboardQuery.isLoading && <p className="text-muted-foreground">Loading…</p>}
          {dashboardQuery.isError && <p className="text-destructive">{(dashboardQuery.error as Error).message}</p>}
          {dashboardQuery.data && (
            dashboardQuery.data.websites.length === 0 ? (
              <p className="rounded-xl bg-sky-50 px-4 py-3 text-sky-900">No websites created yet.</p>
            ) : (
              <Table>
                <TableHeader>
                  <TableRow>
                    {['DB ID', 'Company', 'Created', 'Validity', 'Status', 'Payment'].map((heading) => (
                      <TableHead key={heading}>{heading}</TableHead>
                    ))}
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {dashboardQuery.data.websites.map((w) => (
                    <TableRow key={w.id}>
                      <TableCell>{w.id}</TableCell>
                      <TableCell>{w.companyName || 'Unnamed'}</TableCell>
                      <TableCell>{w.uploadedDate ? w.uploadedDate.slice(0, 10) : ''}</TableCell>
                      <TableCell>{w.validityDisplay}</TableCell>
                      <TableCell>{w.statusText}</TableCell>
                      <TableCell>{w.paymentLabel}</TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            )
          )}
        </DialogContent>
      </Dialog>

      <Dialog open={!!referralEmail} onOpenChange={(open) => !open && setReferralEmail(null)}>
        <DialogContent className="max-h-[85vh] overflow-y-auto sm:max-w-4xl">
          <DialogHeader>
            <DialogTitle>Commission — {referralEmail ?? ''}</DialogTitle>
            <DialogDescription>Referral earnings for this partner.</DialogDescription>
          </DialogHeader>
          {referralQuery.isLoading && <p className="text-muted-foreground">Loading…</p>}
          {referralQuery.isError && <p className="text-destructive">{(referralQuery.error as Error).message}</p>}
          {referralQuery.data && (
            <div className="space-y-3">
              <div className="text-sm text-muted-foreground">
                Total earning ₹ {Math.round(referralQuery.data.totalReferralAmount).toLocaleString('en-IN')} · Pending ₹{' '}
                {Math.round(referralQuery.data.pendingAmount).toLocaleString('en-IN')}
              </div>
              <Table>
                <TableHeader>
                  <TableRow>
                    {['User', 'Email', 'Joined', 'Amount'].map((heading) => (
                      <TableHead key={heading}>{heading}</TableHead>
                    ))}
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {referralQuery.data.referredUsers.length === 0 && (
                    <TableRow>
                      <TableCell colSpan={4} className="text-muted-foreground">No referrals found</TableCell>
                    </TableRow>
                  )}
                  {referralQuery.data.referredUsers.map((r) => (
                    <TableRow key={r.referralId}>
                      <TableCell>{r.userName || 'Unknown'}</TableCell>
                      <TableCell>{r.referredEmail}</TableCell>
                      <TableCell>{r.referralDate?.slice(0, 10)}</TableCell>
                      <TableCell>₹ {Math.round(r.amount).toLocaleString('en-IN')}</TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>
          )}
        </DialogContent>
      </Dialog>

      <Dialog open={!!documents} onOpenChange={(open) => !open && setDocuments(null)}>
        <DialogContent className="max-w-md">
          <DialogHeader>
            <DialogTitle>Documents — {documents?.name}</DialogTitle>
            <DialogDescription>{documents?.documentStatus || 'Not Submitted'}</DialogDescription>
          </DialogHeader>
          <div className="space-y-2 text-sm">
            <DocumentLink label="PAN" href={documents?.panDocumentUrl} />
            <DocumentLink label="ID front" href={documents?.frontDocumentUrl} />
            <DocumentLink label="ID back" href={documents?.backDocumentUrl} />
          </div>
        </DialogContent>
      </Dialog>
    </div>
  )
}

function DmpPartnerRow({
  partner: p,
  showCoupon = false,
  showWallet = false,
  toggling,
  onEdit,
  onToggle,
  onReset,
  onBill,
  onDocuments,
  onCommission,
  onDashboard,
  onRecharge,
}: {
  partner: SalesChannelPartnerRow
  showCoupon?: boolean
  showWallet?: boolean
  toggling: boolean
  onEdit: () => void
  onToggle: () => void
  onReset: () => void
  onBill: () => void
  onDocuments: () => void
  onCommission: () => void
  onDashboard: () => void
  onRecharge?: () => void
}) {
  const active = p.status === 'ACTIVE'
  const commission = p.commissionStatus || 'Not Eligible'
  return (
    <TableRow>
      <TableCell className="font-mono font-medium whitespace-nowrap">{p.userCode}</TableCell>
      <TableCell className="max-w-[200px] truncate">{p.email}</TableCell>
      <TableCell className="whitespace-nowrap">{p.name}</TableCell>
      <TableCell className="whitespace-nowrap">{p.phone}</TableCell>
      <TableCell className="whitespace-nowrap">{p.joinedOnDisplay}</TableCell>
      <TableCell className="whitespace-nowrap">{p.state}</TableCell>
      <TableCell className="whitespace-nowrap">{p.district}</TableCell>
      <TableCell className="whitespace-nowrap">{p.referralSource || 'Direct'}</TableCell>
      {!showCoupon && <TableCell className="whitespace-nowrap">{p.dateCreatedDisplay}</TableCell>}
      {!showCoupon && <TableCell className="whitespace-nowrap">{p.expiryDateDisplay}</TableCell>}
      <TableCell>
        <Badge variant={badgeVariantFromTone(p.statusTone)}>{p.userStatusLabel || (active ? 'Active' : 'InActive')}</Badge>
      </TableCell>
      <TableCell>
        <Badge variant={badgeVariantFromTone(paymentTone(p.userPaymentStatus || 'Unpaid'))}>
          {p.userPaymentStatus || 'Unpaid'}
        </Badge>
      </TableCell>
      <TableCell className="whitespace-nowrap">{p.joiningFeesDisplay || '₹ 0.00'}</TableCell>
      {showCoupon ? (
        <TableCell className="whitespace-nowrap">{p.couponCode}</TableCell>
      ) : (
        <TableCell className="whitespace-nowrap">{p.promoDiscountDisplay || '₹ 0.00'}</TableCell>
      )}
      <TableCell>
        <Button variant="outline" size="sm" disabled={!p.invoiceId} onClick={onBill}>
          View
        </Button>
      </TableCell>
      <TableCell className="text-center font-semibold">{p.dbCount ?? 0}</TableCell>
      <TableCell className="whitespace-nowrap">
        <span className="inline-flex items-center gap-1">
          <Badge variant={badgeVariantFromTone(p.documentStatusTone)}>{p.documentStatus || 'Not Submitted'}</Badge>
          <Button variant="link" size="sm" className="h-auto px-1" onClick={onDocuments}>
            View
          </Button>
        </span>
      </TableCell>
      <TableCell className="text-center font-semibold">{p.totalReferred ?? 0}</TableCell>
      <TableCell className="whitespace-nowrap">{p.totalReferralEarningDisplay || '₹ 0'}</TableCell>
      <TableCell className="whitespace-nowrap">
        <span className="inline-flex items-center gap-1">
          <Badge variant={badgeVariantFromTone(commissionTone(commission))}>{commission}</Badge>
          <Button variant="link" size="sm" className="h-auto px-1" onClick={onCommission}>
            [View]
          </Button>
        </span>
      </TableCell>
      <TableCell>
        <Button variant="outline" size="sm" onClick={onDashboard}>
          View
        </Button>
      </TableCell>
      <TableCell className="whitespace-nowrap">{p.refundStatus}</TableCell>
      <TableCell>
        <div className="flex gap-1">
          <Button size="icon-sm" variant="ghost" className={ACTION_BTN} title="Edit" aria-label="Edit" onClick={onEdit}>
            <Pencil size={14} />
          </Button>
          <Button
            size="icon-sm"
            variant="ghost"
            className={
              active
                ? `${ACTION_BTN} text-red-600 hover:bg-red-50 hover:text-red-700`
                : `${ACTION_BTN} text-emerald-600 hover:bg-emerald-50 hover:text-emerald-700`
            }
            title={active ? 'Deactivate' : 'Activate'}
            aria-label={active ? 'Deactivate' : 'Activate'}
            disabled={toggling}
            onClick={onToggle}
          >
            {toggling ? <RefreshCw size={14} className="animate-spin" /> : active ? <PowerOff size={14} /> : <Power size={14} />}
          </Button>
          <Button size="icon-sm" variant="ghost" className={ACTION_BTN} title="Reset password" aria-label="Reset password" onClick={onReset}>
            <KeyRound size={14} />
          </Button>
          {showWallet && (
            <Button
              size="icon-sm"
              variant="ghost"
              className={`${ACTION_BTN} text-amber-600 hover:bg-amber-50 hover:text-amber-700`}
              title="Recharge Wallet"
              aria-label="Recharge Wallet"
              onClick={onRecharge}
            >
              <BatteryCharging size={14} />
            </Button>
          )}
        </div>
      </TableCell>
    </TableRow>
  )
}

function DocumentLink({ label, href }: { label: string; href?: string | null }) {
  if (!href) return <div className="text-muted-foreground">{label}: not uploaded</div>
  return (
    <a className="block text-rose-700 underline" href={href} target="_blank" rel="noreferrer">
      View {label}
    </a>
  )
}

function Field({ label, children }: { label: string; children: ReactNode }) {
  return (
    <div className="space-y-1.5">
      <label className="text-xs font-medium text-muted-foreground">{label}</label>
      {children}
    </div>
  )
}
