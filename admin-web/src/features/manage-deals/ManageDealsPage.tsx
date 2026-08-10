import { useQuery, useQueryClient } from '@tanstack/react-query'
import { Pencil, Plus, Power, RefreshCw, Search, Trash2, X } from 'lucide-react'
import { useEffect, useMemo, useState } from 'react'
import {
  createDeal,
  deleteDeal,
  fetchManageDeals,
  fetchManageDealsMeta,
  toggleDealStatus,
  updateDeal,
} from '@/features/manage-deals/api'
import type { ManageDealRow, UpsertDealPayload } from '@/shared/types/api'
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

const emptyForm: UpsertDealPayload = {
  planName: '',
  planType: '',
  dealName: '',
  couponCode: '',
  couponType: 'Default',
  bonusAmount: 0,
  discountAmount: 0,
  discountPercentage: 0,
  renewalCommission: 0,
  validityDate: '',
  maxUsage: 0,
  dealState: '',
  status: 'Active',
}

type ConfirmState =
  | { mode: 'delete'; id: number; name: string }
  | { mode: 'toggle'; id: number; name: string; status: string }

function toDateInput(value?: string | null) {
  if (!value) return ''
  const d = new Date(value)
  if (Number.isNaN(d.getTime())) {
    // already yyyy-MM-dd
    return value.slice(0, 10)
  }
  const y = d.getFullYear()
  const m = String(d.getMonth() + 1).padStart(2, '0')
  const day = String(d.getDate()).padStart(2, '0')
  return `${y}-${m}-${day}`
}

export function ManageDealsPage() {
  const toast = useToast()
  const qc = useQueryClient()
  const [page, setPage] = useState(1)
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [planType, setPlanType] = useState('all')
  const [status, setStatus] = useState('all')
  const [filtersOpen, setFiltersOpen] = useState(false)

  const [formOpen, setFormOpen] = useState(false)
  const [editing, setEditing] = useState<ManageDealRow | null>(null)
  const [form, setForm] = useState<UpsertDealPayload>(emptyForm)
  const [formBusy, setFormBusy] = useState(false)
  const [confirm, setConfirm] = useState<ConfirmState | null>(null)
  const [confirmBusy, setConfirmBusy] = useState(false)

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
      planType: planType === 'all' ? undefined : planType,
      status: status === 'all' ? undefined : status,
    }),
    [page, search, planType, status],
  )
  const queryKey = useMemo(() => ['manage-deals', filters] as const, [filters])

  const listQuery = useQuery({
    queryKey,
    queryFn: () => fetchManageDeals(filters),
  })

  const metaQuery = useQuery({
    queryKey: ['manage-deals-meta'],
    queryFn: fetchManageDealsMeta,
    staleTime: Infinity,
  })

  const data = listQuery.data
  const deals = data?.deals ?? []
  const pages = data ? Math.max(1, Math.ceil(data.totalCount / data.pageSize)) : 1
  const hasSearch = Boolean(searchInput.trim())
  const activeFilterCount =
    (hasSearch ? 1 : 0) + (planType !== 'all' ? 1 : 0) + (status !== 'all' ? 1 : 0)
  const states = metaQuery.data?.states ?? []

  const clearFilters = () => {
    setSearchInput('')
    setSearch('')
    setPlanType('all')
    setStatus('all')
    setPage(1)
  }

  const openCreate = () => {
    setEditing(null)
    setForm({ ...emptyForm })
    setFormOpen(true)
  }

  const openEdit = (row: ManageDealRow) => {
    setEditing(row)
    const allowed = ['Default', 'Festive Discount', 'Promotional Discount'] as const
    const couponType = allowed.includes(row.couponTypeDisplay as (typeof allowed)[number])
      ? row.couponTypeDisplay
      : 'Default'
    setForm({
      planName: row.planName,
      planType: row.planType,
      dealName: row.dealName,
      couponCode: row.couponCode,
      couponType,
      bonusAmount: row.bonusAmount,
      discountAmount: row.discountAmount,
      discountPercentage: row.discountPercentage,
      renewalCommission: row.renewalCommission ?? 0,
      validityDate: toDateInput(row.validityDate),
      maxUsage: row.maxUsage,
      dealState: row.dealState || '',
      status: row.dealStatus || 'Active',
    })
    setFormOpen(true)
  }

  const submitForm = async () => {
    if (
      !form.planName ||
      !form.planType ||
      !form.dealName.trim() ||
      !form.couponCode.trim() ||
      !form.couponType ||
      !form.validityDate
    ) {
      return toast.push('Please fill all required fields', 'error')
    }
    setFormBusy(true)
    try {
      const payload: UpsertDealPayload = {
        ...form,
        dealName: form.dealName.trim(),
        couponCode: form.couponCode.trim().toUpperCase(),
        couponType: form.couponType,
        dealState: form.dealState || '',
        bonusAmount: Number(form.bonusAmount) || 0,
        discountAmount: Number(form.discountAmount) || 0,
        discountPercentage: Number(form.discountPercentage) || 0,
        renewalCommission: Number(form.renewalCommission) || 0,
        maxUsage: Number(form.maxUsage) || 0,
        status: editing ? form.status || 'Active' : undefined,
      }
      const res = editing ? await updateDeal(editing.id, payload) : await createDeal(payload)
      toast.push(res.message || (editing ? 'Deal updated' : 'Deal added'), 'success')
      setFormOpen(false)
      await qc.invalidateQueries({ queryKey: ['manage-deals'] })
    } catch (e) {
      toast.push(e instanceof ApiError ? e.message : 'Save failed', 'error')
    } finally {
      setFormBusy(false)
    }
  }

  const runConfirm = async () => {
    if (!confirm) return
    setConfirmBusy(true)
    try {
      const res =
        confirm.mode === 'delete'
          ? await deleteDeal(confirm.id)
          : await toggleDealStatus(confirm.id)
      toast.push(res.message || (confirm.mode === 'delete' ? 'Deal deleted' : 'Status updated'), 'success')
      setConfirm(null)
      await qc.invalidateQueries({ queryKey: ['manage-deals'] })
    } catch (e) {
      toast.push(e instanceof ApiError ? e.message : 'Request failed', 'error')
    } finally {
      setConfirmBusy(false)
    }
  }

  return (
    <div className="flex h-full min-h-0 min-w-0 max-w-full flex-col gap-3">
      <div className="flex shrink-0 flex-wrap items-end justify-between gap-3">
        <div>
          <h1 className="font-[family-name:var(--font-display)] text-3xl font-semibold tracking-tight">
            Manage Deals
          </h1>
          <p className="mt-1 text-sm text-muted-foreground">
            {data?.totalCount ?? '—'} deals & coupons
          </p>
        </div>
        <div className="flex flex-wrap items-center gap-2">
          <FiltersButton activeCount={activeFilterCount} onClick={() => setFiltersOpen(true)} />
          <Button variant="outline" onClick={() => listQuery.refetch()}>
            <RefreshCw size={16} /> Refresh
          </Button>
          <Button onClick={openCreate}>
            <Plus size={16} /> Add Deal
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
              placeholder="Deal name / coupon / plan…"
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
            value={planType}
            onValueChange={(v) => {
              setPlanType(v)
              setPage(1)
            }}
          >
            <SelectTrigger className="w-full">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All</SelectItem>
              <SelectItem value="MiniWebsite">Mini Website</SelectItem>
              <SelectItem value="Franchise">Franchise</SelectItem>
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
              <SelectItem value="Active">Active</SelectItem>
              <SelectItem value="Inactive">Inactive</SelectItem>
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
                  {[
                    'Plan Type',
                    'State',
                    'Deal Name',
                    'Coupon Code',
                    'Coupon Type',
                    'Date Created',
                    'Validity Date',
                    'Referrer Commission',
                    'Referee Discount',
                    'Referrer Commission (On Referee Renewal)',
                    'Usage',
                    'Status',
                    'Action',
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
                    <TableCell colSpan={13} className="py-10 text-center text-muted-foreground">
                      Loading…
                    </TableCell>
                  </TableRow>
                )}
                {listQuery.isError && (
                  <TableRow>
                    <TableCell colSpan={13} className="py-10 text-center text-destructive">
                      {(listQuery.error as Error).message}
                    </TableCell>
                  </TableRow>
                )}
                {!listQuery.isLoading && !listQuery.isError && deals.length === 0 && (
                  <TableRow>
                    <TableCell colSpan={13} className="py-10 text-center text-muted-foreground">
                      No deals found
                    </TableCell>
                  </TableRow>
                )}
                {deals.map((d) => (
                  <TableRow key={d.id}>
                    <TableCell>{d.planType}</TableCell>
                    <TableCell>
                      {d.dealState ? (
                        <Badge variant="secondary">{d.dealStateDisplay}</Badge>
                      ) : (
                        <span className="text-muted-foreground">All</span>
                      )}
                    </TableCell>
                    <TableCell className="font-medium">{d.dealName}</TableCell>
                    <TableCell>
                      <Badge variant="outline">{d.couponCode}</Badge>
                    </TableCell>
                    <TableCell>{d.couponTypeDisplay}</TableCell>
                    <TableCell className="whitespace-nowrap text-muted-foreground">{d.createdAtDisplay}</TableCell>
                    <TableCell className={d.isExpired ? 'text-rose-600' : 'text-emerald-700'}>
                      {d.validityDateDisplay}
                    </TableCell>
                    <TableCell className="whitespace-nowrap">{d.bonusAmountDisplay}</TableCell>
                    <TableCell>{d.discountDisplay}</TableCell>
                    <TableCell className="whitespace-nowrap">{d.renewalCommissionDisplay}</TableCell>
                    <TableCell>{d.usageDisplay}</TableCell>
                    <TableCell>
                      <Badge variant={badgeVariantFromTone(d.statusTone)}>{d.dealStatus}</Badge>
                    </TableCell>
                    <TableCell>
                      <div className="flex gap-1">
                        <Button size="icon-sm" variant="ghost" title="Edit" onClick={() => openEdit(d)}>
                          <Pencil size={14} />
                        </Button>
                        <Button
                          size="icon-sm"
                          variant="ghost"
                          title="Toggle status"
                          onClick={() =>
                            setConfirm({ mode: 'toggle', id: d.id, name: d.dealName, status: d.dealStatus })
                          }
                        >
                          <Power size={14} />
                        </Button>
                        <Button
                          size="icon-sm"
                          variant="ghost"
                          title="Delete"
                          onClick={() => setConfirm({ mode: 'delete', id: d.id, name: d.dealName })}
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
            <DrawerTitle>{editing ? 'Edit Deal' : 'Add New Deal'}</DrawerTitle>
            <DrawerDescription>Deals & coupons for MiniWebsite / Franchise plans</DrawerDescription>
          </DrawerHeader>
          <div className="flex flex-1 flex-col gap-3 overflow-y-auto px-4 pb-2">
            {/* Same sequence as grid columns (Plan Name kept — required DB field) */}
            <FormField label="Plan Type *">
              <Select
                value={form.planType || undefined}
                onValueChange={(v) => setForm((f) => ({ ...f, planType: v }))}
              >
                <SelectTrigger className="w-full">
                  <SelectValue placeholder="Select Plan Type" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="MiniWebsite">Mini Website</SelectItem>
                  <SelectItem value="Franchise">Franchise</SelectItem>
                </SelectContent>
              </Select>
            </FormField>

            <FormField label="Plan Name *">
              <Select
                value={form.planName || undefined}
                onValueChange={(v) => setForm((f) => ({ ...f, planName: v }))}
              >
                <SelectTrigger className="w-full">
                  <SelectValue placeholder="Select Plan Name" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="Basic">Basic Plan</SelectItem>
                  <SelectItem value="Premium">Premium Plan</SelectItem>
                  <SelectItem value="Enterprise">Enterprise Plan</SelectItem>
                </SelectContent>
              </Select>
            </FormField>

            <FormField label="State">
              <Select
                value={form.dealState || '__all__'}
                onValueChange={(v) => setForm((f) => ({ ...f, dealState: v === '__all__' ? '' : v }))}
              >
                <SelectTrigger className="w-full">
                  <SelectValue placeholder="All States" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="__all__">All States (No state restriction)</SelectItem>
                  {states.map((s) => (
                    <SelectItem key={s} value={s}>
                      {s}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </FormField>

            <FormField label="Deal Name *">
              <Input
                value={form.dealName}
                onChange={(e) => setForm((f) => ({ ...f, dealName: e.target.value }))}
                placeholder="e.g., New Year Offer"
              />
            </FormField>

            <FormField label="Coupon Code *">
              <Input
                className="uppercase"
                value={form.couponCode}
                onChange={(e) => setForm((f) => ({ ...f, couponCode: e.target.value.toUpperCase() }))}
                placeholder="e.g., NEWYEAR2024"
              />
            </FormField>

            <FormField label="Coupon Type *">
              <Select
                value={form.couponType || 'Default'}
                onValueChange={(v) => setForm((f) => ({ ...f, couponType: v }))}
              >
                <SelectTrigger className="w-full">
                  <SelectValue placeholder="Select Coupon Type" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="Default">Default</SelectItem>
                  <SelectItem value="Festive Discount">Festive Discount</SelectItem>
                  <SelectItem value="Promotional Discount">Promotional Discount</SelectItem>
                </SelectContent>
              </Select>
            </FormField>

            {editing && (
              <FormField label="Date Created">
                <Input readOnly tabIndex={-1} value={editing.createdAtDisplay || '-'} className="bg-background text-muted-foreground" />
              </FormField>
            )}

            <FormField label="Validity Date *">
              <Input
                type="date"
                value={form.validityDate}
                onChange={(e) => setForm((f) => ({ ...f, validityDate: e.target.value }))}
              />
            </FormField>

            <FormField label="Referrer Commission">
              <Input
                type="number"
                min={0}
                value={form.bonusAmount}
                onChange={(e) => setForm((f) => ({ ...f, bonusAmount: Number(e.target.value) || 0 }))}
              />
            </FormField>

            <div className="grid grid-cols-2 gap-3">
              <FormField label="Referee Discount (Amount)">
                <Input
                  type="number"
                  min={0}
                  value={form.discountAmount}
                  onChange={(e) => setForm((f) => ({ ...f, discountAmount: Number(e.target.value) || 0 }))}
                />
              </FormField>
              <FormField label="Referee Discount (%)">
                <Input
                  type="number"
                  min={0}
                  max={100}
                  value={form.discountPercentage}
                  onChange={(e) =>
                    setForm((f) => ({ ...f, discountPercentage: Number(e.target.value) || 0 }))
                  }
                />
              </FormField>
            </div>

            <FormField label="Referrer Commission (On Referee Renewal)">
              <Input
                type="number"
                min={0}
                value={form.renewalCommission}
                onChange={(e) =>
                  setForm((f) => ({ ...f, renewalCommission: Number(e.target.value) || 0 }))
                }
              />
            </FormField>

            <FormField label="Usage (Max, 0 = Unlimited)">
              <Input
                type="number"
                min={0}
                value={form.maxUsage}
                onChange={(e) => setForm((f) => ({ ...f, maxUsage: Number(e.target.value) || 0 }))}
              />
            </FormField>

            {editing && (
              <FormField label="Status">
                <Select
                  value={form.status || 'Active'}
                  onValueChange={(v) => setForm((f) => ({ ...f, status: v }))}
                >
                  <SelectTrigger className="w-full">
                    <SelectValue placeholder="Select Status" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="Active">Active</SelectItem>
                    <SelectItem value="Inactive">Inactive</SelectItem>
                  </SelectContent>
                </Select>
              </FormField>
            )}
          </div>
          <DrawerFooter>
            <DrawerClose asChild>
              <Button type="button" variant="outline" disabled={formBusy}>
                Cancel
              </Button>
            </DrawerClose>
            <Button type="button" disabled={formBusy} onClick={() => void submitForm()}>
              {formBusy ? 'Saving…' : editing ? 'Update Deal' : 'Add Deal'}
            </Button>
          </DrawerFooter>
        </DrawerContent>
      </Drawer>

      <AlertDialog open={!!confirm} onOpenChange={(o) => !o && !confirmBusy && setConfirm(null)}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>
              {confirm?.mode === 'delete' ? 'Delete deal?' : 'Toggle status?'}
            </AlertDialogTitle>
            <AlertDialogDescription>
              {confirm?.mode === 'delete' ? (
                <>
                  Delete <strong>{confirm.name}</strong>? This cannot be undone.
                </>
              ) : (
                <>
                  Switch <strong>{confirm?.name}</strong> from {confirm?.status} to{' '}
                  {confirm?.status === 'Active' ? 'Inactive' : 'Active'}?
                </>
              )}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel disabled={confirmBusy}>Cancel</AlertDialogCancel>
            <AlertDialogAction
              disabled={confirmBusy}
              variant={confirm?.mode === 'delete' ? 'destructive' : 'default'}
              onClick={(e) => {
                e.preventDefault()
                void runConfirm()
              }}
            >
              {confirmBusy ? 'Please wait…' : confirm?.mode === 'delete' ? 'Delete' : 'Toggle'}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  )
}

