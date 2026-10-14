import { useQuery, useQueryClient } from '@tanstack/react-query'
import { Dices, Pencil, PenLine, Plus, RefreshCw, Search, Trash2, X } from 'lucide-react'
import { useEffect, useMemo, useRef, useState } from 'react'
import {
  createCoupon,
  deleteCoupon,
  fetchCoupons,
  fetchCouponsMeta,
  generateCouponCode,
  updateCoupon,
} from '@/features/coupons/api'
import type { CouponRow, PromoDiscountType, UpsertCouponPayload } from '@/shared/types/api'
import { ApiError } from '@/shared/api/client'
import { badgeVariantFromTone } from '@/shared/lib/badgeTone'
import { FiltersButton, FiltersDrawer } from '@/shared/ui/FiltersDrawer'
import { FormField } from '@/shared/ui/FormField'
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

const COUPON_TYPES = ['Festive Discount', 'Promotional Discount']
const FORM_STATUSES = ['Active', 'InActive']
const COUPON_CODE_PATTERN = /^[A-Z0-9]{4,20}$/
const COLUMNS = [
  'Plan Type',
  'Coupon Name',
  'Coupon Code',
  'Coupon Type',
  'Date Created',
  'Validity Date',
  'Promo Discount',
  'Usage Limit Per Coupon',
  'Usage Limit Per User',
  'Status',
  'Action',
]

type CouponForm = {
  roleId: string
  couponName: string
  couponCode: string
  couponType: string
  validityDate: string
  promoDiscountType: PromoDiscountType
  promoDiscountValue: string
  usageLimitPerCoupon: string
  usageLimitPerUser: string
  status: string
  referrerCommission: string
  renewalCommission: string
}

type CodeMode = 'custom' | 'random'

const emptyForm: CouponForm = {
  roleId: '',
  couponName: '',
  couponCode: '',
  couponType: '',
  validityDate: '',
  promoDiscountType: 'Amount',
  promoDiscountValue: '0',
  usageLimitPerCoupon: '0',
  usageLimitPerUser: '0',
  status: 'Active',
  referrerCommission: '0',
  renewalCommission: '0',
}

function toDateInput(value?: string | null) {
  if (!value) return ''
  return value.slice(0, 10)
}

function todayInput() {
  const d = new Date()
  const m = String(d.getMonth() + 1).padStart(2, '0')
  const day = String(d.getDate()).padStart(2, '0')
  return `${d.getFullYear()}-${m}-${day}`
}

function toNumber(value: string) {
  const n = Number(value)
  return Number.isFinite(n) ? n : NaN
}

export function CouponsPage() {
  const toast = useToast()
  const qc = useQueryClient()
  const [page, setPage] = useState(1)
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [roleFilter, setRoleFilter] = useState('all')
  const [status, setStatus] = useState('all')
  const [filtersOpen, setFiltersOpen] = useState(false)

  const [formOpen, setFormOpen] = useState(false)
  const [editing, setEditing] = useState<CouponRow | null>(null)
  const [form, setForm] = useState<CouponForm>(emptyForm)
  const [codeMode, setCodeMode] = useState<CodeMode>('custom')
  const [generating, setGenerating] = useState(false)
  const [formBusy, setFormBusy] = useState(false)
  const [confirmDelete, setConfirmDelete] = useState<{ id: number; name: string } | null>(null)
  const [confirmBusy, setConfirmBusy] = useState(false)
  const codeInputRef = useRef<HTMLInputElement>(null)

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
      roleId: roleFilter === 'all' ? undefined : Number(roleFilter),
      status: status === 'all' ? undefined : status,
    }),
    [page, search, roleFilter, status],
  )
  const queryKey = useMemo(() => ['coupons', filters] as const, [filters])

  const listQuery = useQuery({
    queryKey,
    queryFn: () => fetchCoupons(filters),
  })

  const metaQuery = useQuery({
    queryKey: ['coupons-meta'],
    queryFn: fetchCouponsMeta,
    staleTime: 5 * 60 * 1000,
  })

  const data = listQuery.data
  const coupons = data?.coupons ?? []
  const pages = data ? Math.max(1, Math.ceil(data.totalCount / data.pageSize)) : 1
  const hasSearch = Boolean(searchInput.trim())
  const activeFilterCount =
    (hasSearch ? 1 : 0) + (roleFilter !== 'all' ? 1 : 0) + (status !== 'all' ? 1 : 0)
  const roles = metaQuery.data?.roles ?? []
  const couponTypes = metaQuery.data?.couponTypes?.length ? metaQuery.data.couponTypes : COUPON_TYPES
  const statuses = metaQuery.data?.statuses?.length ? metaQuery.data.statuses : [...FORM_STATUSES, 'Expired']

  const clearFilters = () => {
    setSearchInput('')
    setSearch('')
    setRoleFilter('all')
    setStatus('all')
    setPage(1)
  }

  const openCreate = () => {
    setEditing(null)
    setForm({ ...emptyForm })
    setCodeMode('custom')
    setFormOpen(true)
  }

  const openEdit = (row: CouponRow) => {
    setEditing(row)
    setForm({
      roleId: row.roleId ? String(row.roleId) : '',
      couponName: row.couponName,
      couponCode: row.couponCode,
      couponType: COUPON_TYPES.includes(row.couponType) ? row.couponType : '',
      validityDate: toDateInput(row.validityDate),
      promoDiscountType: row.promoDiscountType === 'Percentage' ? 'Percentage' : 'Amount',
      promoDiscountValue: String(row.promoDiscountValue ?? 0),
      usageLimitPerCoupon: String(row.usageLimitPerCoupon ?? 0),
      usageLimitPerUser: String(row.usageLimitPerUser ?? 0),
      status: row.status === 'Active' ? 'Active' : 'InActive',
      referrerCommission: String(row.referrerCommission ?? 0),
      renewalCommission: String(row.renewalCommission ?? 0),
    })
    setCodeMode('custom')
    setFormOpen(true)
  }

  const chooseCustomCode = () => {
    if (codeMode === 'random') setForm((f) => ({ ...f, couponCode: '' }))
    setCodeMode('custom')
    window.setTimeout(() => codeInputRef.current?.focus(), 0)
  }

  const chooseRandomCode = async () => {
    setGenerating(true)
    try {
      const res = await generateCouponCode()
      setForm((f) => ({ ...f, couponCode: res.code }))
      setCodeMode('random')
    } catch (e) {
      toast.push(e instanceof ApiError ? e.message : 'Unable to generate a code', 'error')
    } finally {
      setGenerating(false)
    }
  }

  const validate = (): string | null => {
    if (!form.roleId) return 'Please select a plan type'
    if (!form.couponName.trim()) return 'Please enter a coupon name'
    if (!COUPON_CODE_PATTERN.test(form.couponCode)) return 'Coupon code must be 4 to 20 letters or numbers'
    if (!form.couponType) return 'Please select a coupon type'
    if (!form.validityDate) return 'Please select a validity date'
    const discount = toNumber(form.promoDiscountValue)
    if (Number.isNaN(discount) || discount < 0) return 'Promo discount cannot be negative'
    if (form.promoDiscountType === 'Percentage' && discount > 100)
      return 'Promo discount percentage cannot be more than 100'
    const perCoupon = toNumber(form.usageLimitPerCoupon)
    const perUser = toNumber(form.usageLimitPerUser)
    if (!Number.isInteger(perCoupon) || perCoupon < 0) return 'Usage limit per coupon must be 0 or more'
    if (!Number.isInteger(perUser) || perUser < 0) return 'Usage limit per user must be 0 or more'
    if (perCoupon > 0 && perUser > perCoupon)
      return 'Usage limit per user cannot be more than the usage limit per coupon'
    const referrer = toNumber(form.referrerCommission)
    const renewal = toNumber(form.renewalCommission)
    if (Number.isNaN(referrer) || referrer < 0) return 'Referrer commission cannot be negative'
    if (Number.isNaN(renewal) || renewal < 0) return 'Renewal commission cannot be negative'
    return null
  }

  const submitForm = async () => {
    const error = validate()
    if (error) return toast.push(error, 'error')
    setFormBusy(true)
    try {
      const payload: UpsertCouponPayload = {
        roleId: Number(form.roleId),
        couponName: form.couponName.trim(),
        couponCode: form.couponCode,
        couponType: form.couponType,
        validityDate: form.validityDate,
        promoDiscountType: form.promoDiscountType,
        promoDiscountValue: toNumber(form.promoDiscountValue),
        usageLimitPerCoupon: toNumber(form.usageLimitPerCoupon),
        usageLimitPerUser: toNumber(form.usageLimitPerUser),
        status: form.status,
        referrerCommission: toNumber(form.referrerCommission),
        renewalCommission: toNumber(form.renewalCommission),
      }
      const res = editing ? await updateCoupon(editing.id, payload) : await createCoupon(payload)
      toast.push(res.message || (editing ? 'Coupon updated' : 'Coupon added'), 'success')
      setFormOpen(false)
      await qc.invalidateQueries({ queryKey: ['coupons'] })
    } catch (e) {
      toast.push(e instanceof ApiError ? e.message : 'Save failed', 'error')
    } finally {
      setFormBusy(false)
    }
  }

  const runDelete = async () => {
    if (!confirmDelete) return
    setConfirmBusy(true)
    try {
      const res = await deleteCoupon(confirmDelete.id)
      toast.push(res.message || 'Coupon deleted', 'success')
      setConfirmDelete(null)
      await qc.invalidateQueries({ queryKey: ['coupons'] })
    } catch (e) {
      toast.push(e instanceof ApiError ? e.message : 'Delete failed', 'error')
    } finally {
      setConfirmBusy(false)
    }
  }

  return (
    <div className="flex h-full min-h-0 min-w-0 max-w-full flex-col gap-3">
      <div className="flex shrink-0 flex-wrap items-end justify-between gap-3">
        <div>
          <h1 className="font-[family-name:var(--font-display)] text-3xl font-semibold tracking-tight">
            Coupons
          </h1>
          <p className="mt-1 text-sm text-muted-foreground">{data?.totalCount ?? '—'} coupons</p>
        </div>
        <div className="flex flex-wrap items-center gap-2">
          <FiltersButton activeCount={activeFilterCount} onClick={() => setFiltersOpen(true)} />
          <Button variant="outline" onClick={() => listQuery.refetch()}>
            <RefreshCw size={16} /> Refresh
          </Button>
          <Button onClick={openCreate}>
            <Plus size={16} /> Add Coupon
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
              placeholder="Coupon name / code / plan type…"
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
          <label className="text-xs font-medium text-muted-foreground">Plan type</label>
          <Select
            value={roleFilter}
            onValueChange={(v) => {
              setRoleFilter(v)
              setPage(1)
            }}
          >
            <SelectTrigger className="w-full">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All</SelectItem>
              {roles.map((r) => (
                <SelectItem key={r.roleId} value={String(r.roleId)}>
                  {r.roleName}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
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
              {statuses.map((s) => (
                <SelectItem key={s} value={s}>
                  {s}
                </SelectItem>
              ))}
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
                  {COLUMNS.map((h) => (
                    <TableHead key={h} className="text-xs font-semibold tracking-wide text-slate-200 uppercase">
                      {h}
                    </TableHead>
                  ))}
                </TableRow>
              </TableHeader>
              <TableBody>
                {listQuery.isLoading && (
                  <TableRow>
                    <TableCell colSpan={COLUMNS.length} className="py-10 text-center text-muted-foreground">
                      Loading…
                    </TableCell>
                  </TableRow>
                )}
                {listQuery.isError && (
                  <TableRow>
                    <TableCell colSpan={COLUMNS.length} className="py-10 text-center text-destructive">
                      {(listQuery.error as Error).message}
                    </TableCell>
                  </TableRow>
                )}
                {!listQuery.isLoading && !listQuery.isError && coupons.length === 0 && (
                  <TableRow>
                    <TableCell colSpan={COLUMNS.length} className="py-10 text-center text-muted-foreground">
                      No coupons found
                    </TableCell>
                  </TableRow>
                )}
                {coupons.map((c) => (
                  <TableRow key={c.id}>
                    <TableCell>{c.planType}</TableCell>
                    <TableCell className="font-medium">{c.couponName}</TableCell>
                    <TableCell>
                      <Badge variant="outline" className="font-mono">
                        {c.couponCode}
                      </Badge>
                    </TableCell>
                    <TableCell>{c.couponType}</TableCell>
                    <TableCell className="whitespace-nowrap text-muted-foreground">{c.createdAtDisplay}</TableCell>
                    <TableCell
                      className={`whitespace-nowrap ${c.statusDisplay === 'Expired' ? 'text-rose-600' : ''}`}
                    >
                      {c.validityDateDisplay}
                    </TableCell>
                    <TableCell className="whitespace-nowrap">{c.promoDiscountDisplay}</TableCell>
                    <TableCell>{c.usageLimitPerCouponDisplay}</TableCell>
                    <TableCell>{c.usageLimitPerUserDisplay}</TableCell>
                    <TableCell>
                      <Badge variant={badgeVariantFromTone(c.statusTone)}>{c.statusDisplay}</Badge>
                    </TableCell>
                    <TableCell>
                      <div className="flex gap-1">
                        <Button size="icon-sm" variant="ghost" title="Edit" onClick={() => openEdit(c)}>
                          <Pencil size={14} />
                        </Button>
                        <Button
                          size="icon-sm"
                          variant="ghost"
                          title="Delete"
                          onClick={() => setConfirmDelete({ id: c.id, name: c.couponName })}
                        >
                          <Trash2 size={14} />
                        </Button>
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
        <DrawerContent className="data-[vaul-drawer-direction=right]:sm:max-w-md">
          <DrawerHeader>
            <DrawerTitle>{editing ? 'Edit Coupon' : 'Add New Coupon'}</DrawerTitle>
            <DrawerDescription>Coupons are applied per plan type (role).</DrawerDescription>
          </DrawerHeader>
          <div className="flex flex-1 flex-col gap-3 overflow-y-auto px-4 pb-2">
            <FormField label="Plan Type *">
              <Select
                value={form.roleId || undefined}
                onValueChange={(v) => setForm((f) => ({ ...f, roleId: v }))}
              >
                <SelectTrigger className="w-full">
                  <SelectValue placeholder="Select Plan Type" />
                </SelectTrigger>
                <SelectContent>
                  {roles.map((r) => (
                    <SelectItem key={r.roleId} value={String(r.roleId)}>
                      {r.roleName}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </FormField>

            <FormField label="Coupon Name *">
              <Input
                value={form.couponName}
                maxLength={200}
                onChange={(e) => setForm((f) => ({ ...f, couponName: e.target.value }))}
                placeholder="e.g., Diwali Offer"
              />
            </FormField>

            <FormField label="Coupon Code *">
              <div className="space-y-2">
                <div className="grid grid-cols-2 gap-2">
                  <Button
                    type="button"
                    variant={codeMode === 'custom' ? 'default' : 'outline'}
                    onClick={chooseCustomCode}
                  >
                    <PenLine size={14} /> Add Custom
                  </Button>
                  <Button
                    type="button"
                    variant={codeMode === 'random' ? 'default' : 'outline'}
                    disabled={generating}
                    onClick={() => void chooseRandomCode()}
                  >
                    <Dices size={14} /> {generating ? 'Generating…' : 'Generate Random'}
                  </Button>
                </div>
                <Input
                  ref={codeInputRef}
                  className="font-mono uppercase"
                  value={form.couponCode}
                  maxLength={20}
                  readOnly={codeMode === 'random'}
                  onChange={(e) =>
                    setForm((f) => ({ ...f, couponCode: e.target.value.toUpperCase().replace(/[^A-Z0-9]/g, '') }))
                  }
                  placeholder={codeMode === 'random' ? 'Click Generate Random' : 'e.g., DIWALI2026'}
                />
                <p className="text-xs text-muted-foreground">
                  {codeMode === 'random'
                    ? 'Random code: 4 letters + 4 numbers. Click Generate Random again for a new one.'
                    : '4 to 20 letters or numbers.'}
                </p>
              </div>
            </FormField>

            <FormField label="Coupon Type *">
              <Select
                value={form.couponType || undefined}
                onValueChange={(v) => setForm((f) => ({ ...f, couponType: v }))}
              >
                <SelectTrigger className="w-full">
                  <SelectValue placeholder="Select Coupon Type" />
                </SelectTrigger>
                <SelectContent>
                  {couponTypes.map((t) => (
                    <SelectItem key={t} value={t}>
                      {t}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </FormField>

            {editing && (
              <FormField label="Date Created">
                <Input
                  readOnly
                  tabIndex={-1}
                  value={editing.createdAtDisplay || '-'}
                  className="bg-background text-muted-foreground"
                />
              </FormField>
            )}

            <FormField label="Validity Date *">
              <Input
                type="date"
                min={editing ? undefined : todayInput()}
                value={form.validityDate}
                onChange={(e) => setForm((f) => ({ ...f, validityDate: e.target.value }))}
              />
            </FormField>

            <FormField label="Promo Discount">
              <div className="flex gap-2">
                <Select
                  value={form.promoDiscountType}
                  onValueChange={(v) => setForm((f) => ({ ...f, promoDiscountType: v as PromoDiscountType }))}
                >
                  <SelectTrigger className="w-28 shrink-0">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="Amount">₹ Amount</SelectItem>
                    <SelectItem value="Percentage">% Percent</SelectItem>
                  </SelectContent>
                </Select>
                <Input
                  type="number"
                  min={0}
                  max={form.promoDiscountType === 'Percentage' ? 100 : undefined}
                  step="0.01"
                  value={form.promoDiscountValue}
                  onChange={(e) => setForm((f) => ({ ...f, promoDiscountValue: e.target.value }))}
                />
              </div>
            </FormField>

            <div className="grid grid-cols-2 gap-3">
              <FormField label="Usage Limit Per Coupon">
                <Input
                  type="number"
                  min={0}
                  step={1}
                  value={form.usageLimitPerCoupon}
                  onChange={(e) => setForm((f) => ({ ...f, usageLimitPerCoupon: e.target.value }))}
                />
              </FormField>
              <FormField label="Usage Limit Per User">
                <Input
                  type="number"
                  min={0}
                  step={1}
                  value={form.usageLimitPerUser}
                  onChange={(e) => setForm((f) => ({ ...f, usageLimitPerUser: e.target.value }))}
                />
              </FormField>
            </div>
            <p className="-mt-1 text-xs text-muted-foreground">0 = Unlimited</p>

            <FormField label="Status">
              <Select value={form.status} onValueChange={(v) => setForm((f) => ({ ...f, status: v }))}>
                <SelectTrigger className="w-full">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {FORM_STATUSES.map((s) => (
                    <SelectItem key={s} value={s}>
                      {s}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </FormField>
            {editing?.statusDisplay === 'Expired' && (
              <p className="-mt-1 text-xs text-rose-600">
                This coupon shows as Expired because its validity date has passed. Extend the validity date to use it
                again.
              </p>
            )}

            <div className="mt-2 border-t pt-3">
              <p className="mb-2 text-xs font-semibold tracking-wide text-muted-foreground uppercase">
                Referral commission
              </p>
              <div className="grid grid-cols-2 gap-3">
                <FormField label="Referrer Commission (₹)">
                  <Input
                    type="number"
                    min={0}
                    value={form.referrerCommission}
                    onChange={(e) => setForm((f) => ({ ...f, referrerCommission: e.target.value }))}
                  />
                </FormField>
                <FormField label="On Referee Renewal (₹)">
                  <Input
                    type="number"
                    min={0}
                    value={form.renewalCommission}
                    onChange={(e) => setForm((f) => ({ ...f, renewalCommission: e.target.value }))}
                  />
                </FormField>
              </div>
            </div>
          </div>
          <DrawerFooter>
            <DrawerClose asChild>
              <Button type="button" variant="outline" disabled={formBusy}>
                Cancel
              </Button>
            </DrawerClose>
            <Button type="button" disabled={formBusy} onClick={() => void submitForm()}>
              {formBusy ? 'Saving…' : editing ? 'Update Coupon' : 'Add Coupon'}
            </Button>
          </DrawerFooter>
        </DrawerContent>
      </Drawer>

      <AlertDialog open={!!confirmDelete} onOpenChange={(o) => !o && !confirmBusy && setConfirmDelete(null)}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Delete coupon?</AlertDialogTitle>
            <AlertDialogDescription>
              Delete <strong>{confirmDelete?.name}</strong>? This cannot be undone.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel disabled={confirmBusy}>Cancel</AlertDialogCancel>
            <AlertDialogAction
              disabled={confirmBusy}
              variant="destructive"
              onClick={(e) => {
                e.preventDefault()
                void runDelete()
              }}
            >
              {confirmBusy ? 'Please wait…' : 'Delete'}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  )
}
