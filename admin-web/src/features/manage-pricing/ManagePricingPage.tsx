import { useQuery, useQueryClient } from '@tanstack/react-query'
import { BadgeIndianRupee, Pencil, Plus, Power, RefreshCw, Search, Trash2 } from 'lucide-react'
import { useEffect, useMemo, useState } from 'react'
import {
  createPricing,
  deletePricing,
  fetchManagePricing,
  fetchManagePricingMeta,
  togglePricingStatus,
  updatePricing,
} from '@/features/manage-pricing/api'
import type { ManagePricingRow, UpsertPricingPayload } from '@/shared/types/api'
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

const GST_RATE = 1.18

type PricingFormState = {
  planType: string
  planName: string
  /** Keep as string so the user can clear the field while typing */
  originalPrice: string
  offeredPrice: string
  dealId: number | null
  status: string
}

const emptyForm: PricingFormState = {
  planType: '',
  planName: '',
  originalPrice: '',
  offeredPrice: '',
  dealId: null,
  status: 'Active',
}

type ConfirmState =
  | { mode: 'delete'; id: number; name: string }
  | { mode: 'toggle'; id: number; name: string; status: string }

function withGst(amount: number) {
  const n = Number(amount)
  if (!Number.isFinite(n) || n < 0) return 0
  // Round to nearest whole rupee (no paise / decimals)
  return Math.round(n * GST_RATE)
}

function moneyLabel(raw: string) {
  if (raw.trim() === '') return '₹0'
  const n = Number(raw)
  if (!Number.isFinite(n) || n < 0) return '₹0'
  return `₹${withGst(n).toLocaleString('en-IN', { maximumFractionDigits: 0 })}`
}

function parsePriceInput(raw: string): number {
  const n = Number(raw)
  if (!Number.isFinite(n) || n < 0) return 0
  return n
}

/** Allow digits and one decimal point while typing; empty string is allowed. */
function sanitizePriceInput(raw: string) {
  const cleaned = raw.replace(/[^\d.]/g, '')
  const parts = cleaned.split('.')
  if (parts.length <= 1) return cleaned
  return `${parts[0]}.${parts.slice(1).join('').slice(0, 2)}`
}

export function ManagePricingPage() {
  const toast = useToast()
  const qc = useQueryClient()
  const [page, setPage] = useState(1)
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [planType, setPlanType] = useState('all')
  const [status, setStatus] = useState('all')
  const [filtersOpen, setFiltersOpen] = useState(false)

  const [formOpen, setFormOpen] = useState(false)
  const [editing, setEditing] = useState<ManagePricingRow | null>(null)
  const [form, setForm] = useState<PricingFormState>(emptyForm)
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
  const queryKey = useMemo(() => ['manage-pricing', filters] as const, [filters])

  const listQuery = useQuery({
    queryKey,
    queryFn: () => fetchManagePricing(filters),
  })

  const metaQuery = useQuery({
    queryKey: ['manage-pricing-meta'],
    queryFn: fetchManagePricingMeta,
    staleTime: Infinity,
  })

  const data = listQuery.data
  const items = data?.items ?? []
  const pages = data ? Math.max(1, Math.ceil(data.totalCount / data.pageSize)) : 1
  const hasSearch = Boolean(searchInput.trim())
  const activeFilterCount =
    (hasSearch ? 1 : 0) + (planType !== 'all' ? 1 : 0) + (status !== 'all' ? 1 : 0)

  const catalog = metaQuery.data?.planCatalog ?? []
  const dealOptions = metaQuery.data?.deals ?? []
  const existingPlans = metaQuery.data?.existingPlans ?? []
  const planNamesForType = useMemo(() => {
    const entry = catalog.find((c) => c.key === form.planType)
    const names = entry?.planNames ?? []
    const taken = new Set(
      existingPlans
        .filter((p) => p.planType === form.planType && p.id !== editing?.id)
        .map((p) => p.planName),
    )
    return names.filter((n) => !taken.has(n))
  }, [catalog, existingPlans, form.planType, editing?.id])

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

  const openEdit = (row: ManagePricingRow) => {
    setEditing(row)
    setForm({
      planType: row.planType,
      planName: row.planName,
      originalPrice: String(row.originalPrice ?? ''),
      offeredPrice: String(row.offeredPrice ?? ''),
      dealId: row.dealId ?? null,
      status: row.status || 'Active',
    })
    setFormOpen(true)
  }

  const submitForm = async () => {
    if (!form.planType || !form.planName) {
      return toast.push('Please select plan type and plan name', 'error')
    }
    const duplicate = existingPlans.some(
      (p) =>
        p.planType === form.planType &&
        p.planName === form.planName &&
        p.id !== editing?.id,
    )
    if (duplicate) {
      return toast.push('This plan type and plan name combination already exists', 'error')
    }
    if (!planNamesForType.includes(form.planName)) {
      return toast.push('This plan name is already used for the selected plan type', 'error')
    }
    const originalPrice = parsePriceInput(form.originalPrice)
    const offeredPrice = parsePriceInput(form.offeredPrice)
    if (form.originalPrice.trim() === '' || form.offeredPrice.trim() === '') {
      return toast.push('Please enter original and offered prices', 'error')
    }
    if (originalPrice < 0 || offeredPrice < 0) {
      return toast.push('Prices cannot be negative', 'error')
    }
    setFormBusy(true)
    try {
      const payload: UpsertPricingPayload = {
        planType: form.planType,
        planName: form.planName,
        originalPrice,
        offeredPrice,
        dealId: form.dealId && form.dealId > 0 ? form.dealId : null,
        status: editing ? form.status || 'Active' : undefined,
      }
      const res = editing
        ? await updatePricing(editing.id, payload)
        : await createPricing(payload)
      toast.push(res.message || (editing ? 'Pricing updated' : 'Pricing added'), 'success')
      setFormOpen(false)
      await qc.invalidateQueries({ queryKey: ['manage-pricing'] })
      await qc.invalidateQueries({ queryKey: ['manage-pricing-meta'] })
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
      if (confirm.mode === 'delete') {
        const res = await deletePricing(confirm.id)
        toast.push(res.message || 'Deleted', 'success')
      } else {
        const res = await togglePricingStatus(confirm.id)
        toast.push(res.message || 'Status updated', 'success')
      }
      setConfirm(null)
      await qc.invalidateQueries({ queryKey: ['manage-pricing'] })
      await qc.invalidateQueries({ queryKey: ['manage-pricing-meta'] })
    } catch (e) {
      toast.push(e instanceof ApiError ? e.message : 'Action failed', 'error')
    } finally {
      setConfirmBusy(false)
    }
  }

  return (
    <div className="flex h-full min-h-0 min-w-0 max-w-full flex-col gap-3">
      <div className="flex shrink-0 flex-wrap items-end justify-between gap-3">
        <div>
          <h1 className="font-[family-name:var(--font-display)] flex items-center gap-2 text-3xl font-semibold tracking-tight">
            <BadgeIndianRupee size={28} className="text-emerald-700" />
            Manage Pricing
          </h1>
          <p className="mt-1 text-sm text-muted-foreground">{data?.totalCount ?? '—'} pricing plans</p>
        </div>
        <div className="flex flex-wrap items-center gap-2">
          <FiltersButton activeCount={activeFilterCount} onClick={() => setFiltersOpen(true)} />
          <Button variant="outline" size="sm" onClick={() => listQuery.refetch()} disabled={listQuery.isFetching}>
            <RefreshCw size={16} className={listQuery.isFetching ? 'animate-spin' : ''} />
            Refresh
          </Button>
          <Button size="sm" onClick={openCreate}>
            <Plus size={16} />
            Add Pricing
          </Button>
        </div>
      </div>

      <Card className="flex min-h-0 min-w-0 max-w-full flex-1 flex-col overflow-hidden py-0">
        <CardContent className="flex min-h-0 flex-1 flex-col p-0">
          {listQuery.isError && (
            <div className="m-4 rounded-md border border-destructive/30 bg-destructive/5 px-3 py-2 text-sm text-destructive">
              {(listQuery.error as Error)?.message || 'Failed to load'}
            </div>
          )}

          <div className="min-h-0 flex-1 overflow-auto">
            <Table className="w-max min-w-full">
              <TableHeader className="sticky top-0 z-10 bg-slate-900">
                <TableRow className="border-slate-800 hover:bg-slate-900">
                  {[
                    'Plan Type',
                    'Plan Name',
                    'Original (w/o GST)',
                    'Original (w/ GST)',
                    'Offered (w/o GST)',
                    'Offered (w/ GST)',
                    'Deal / Coupon',
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
                    <TableCell colSpan={9} className="py-10 text-center text-muted-foreground">
                      Loading…
                    </TableCell>
                  </TableRow>
                )}
                {!listQuery.isLoading && items.length === 0 && (
                  <TableRow>
                    <TableCell colSpan={9} className="py-10 text-center text-muted-foreground">
                      No pricing plans found.
                    </TableCell>
                  </TableRow>
                )}
                {items.map((row) => (
                    <TableRow key={row.id}>
                      <TableCell className="max-w-[10rem] whitespace-normal text-sm">{row.planType}</TableCell>
                      <TableCell className="max-w-[14rem] whitespace-normal font-medium">{row.planName}</TableCell>
                      <TableCell>{row.originalPriceDisplay}</TableCell>
                      <TableCell>{row.originalPriceGstDisplay}</TableCell>
                      <TableCell>{row.offeredPriceDisplay}</TableCell>
                      <TableCell>{row.offeredPriceGstDisplay}</TableCell>
                      <TableCell className="max-w-[12rem] whitespace-normal text-sm">{row.dealDisplay}</TableCell>
                      <TableCell>
                        <Badge variant={badgeVariantFromTone(row.statusTone)}>{row.status}</Badge>
                      </TableCell>
                      <TableCell>
                        <div className="flex gap-1">
                          <Button size="icon-sm" variant="ghost" title="Edit" onClick={() => openEdit(row)}>
                            <Pencil size={14} />
                          </Button>
                          <Button
                            size="icon-sm"
                            variant="ghost"
                            title="Toggle status"
                            onClick={() =>
                              setConfirm({
                                mode: 'toggle',
                                id: row.id,
                                name: row.planName,
                                status: row.status,
                              })
                            }
                          >
                            <Power size={14} />
                          </Button>
                          <Button
                            size="icon-sm"
                            variant="ghost"
                            title="Delete"
                            onClick={() => setConfirm({ mode: 'delete', id: row.id, name: row.planName })}
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

      <FiltersDrawer
        open={filtersOpen}
        onOpenChange={setFiltersOpen}
        title="Filter pricing"
        onClear={activeFilterCount > 0 ? clearFilters : undefined}
      >
        <div className="space-y-4">
          <div className="space-y-2">
            <label className="text-sm font-medium">Search</label>
            <div className="relative">
              <Search size={16} className="absolute top-2.5 left-2.5 text-muted-foreground" />
              <Input
                className="pl-8"
                value={searchInput}
                onChange={(e) => setSearchInput(e.target.value)}
                placeholder="Plan, deal, coupon…"
              />
            </div>
          </div>
          <div className="space-y-2">
            <label className="text-sm font-medium">Plan type</label>
            <Select
              value={planType}
              onValueChange={(v) => {
                setPlanType(v)
                setPage(1)
              }}
            >
              <SelectTrigger>
                <SelectValue placeholder="All" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">All</SelectItem>
                {catalog.map((c) => (
                  <SelectItem key={c.key} value={c.key}>
                    {c.key}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
          <div className="space-y-2">
            <label className="text-sm font-medium">Status</label>
            <Select
              value={status}
              onValueChange={(v) => {
                setStatus(v)
                setPage(1)
              }}
            >
              <SelectTrigger>
                <SelectValue placeholder="All" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">All</SelectItem>
                <SelectItem value="Active">Active</SelectItem>
                <SelectItem value="Inactive">Inactive</SelectItem>
              </SelectContent>
            </Select>
          </div>
        </div>
      </FiltersDrawer>

      <Drawer open={formOpen} onOpenChange={setFormOpen} direction="right">
        <DrawerContent className="data-[vaul-drawer-direction=right]:sm:max-w-md">
          <DrawerHeader>
            <DrawerTitle>{editing ? 'Edit Pricing' : 'Add Pricing'}</DrawerTitle>
            <DrawerDescription>
              GST is auto-calculated at 18%. Plan type and name are fixed catalog options.
            </DrawerDescription>
          </DrawerHeader>
          <div className="flex flex-1 flex-col gap-3 overflow-y-auto px-4 pb-2">
            <FormField label="Plan Type *">
              <Select
                value={form.planType || undefined}
                onValueChange={(v) => setForm((f) => ({ ...f, planType: v, planName: '' }))}
              >
                <SelectTrigger className="w-full">
                  <SelectValue placeholder="Select Plan Type" />
                </SelectTrigger>
                <SelectContent>
                  {catalog.map((c) => (
                    <SelectItem key={c.key} value={c.key}>
                      {c.key}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </FormField>

            <FormField label="Plan Name *">
              <Select
                value={form.planName || undefined}
                onValueChange={(v) => setForm((f) => ({ ...f, planName: v }))}
                disabled={!form.planType}
              >
                <SelectTrigger className="w-full">
                  <SelectValue placeholder="Select Plan Name" />
                </SelectTrigger>
                <SelectContent>
                  {planNamesForType.map((n) => (
                    <SelectItem key={n} value={n}>
                      {n}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </FormField>

            <div className="rounded-lg border bg-muted/30 p-3">
              <p className="mb-2 text-xs font-semibold tracking-wide text-foreground uppercase">
                Original Price
              </p>
              <div className="grid grid-cols-2 gap-3">
                <FormField label="Without GST *">
                  <Input
                    type="text"
                    inputMode="decimal"
                    className="w-full"
                    placeholder="0"
                    value={form.originalPrice}
                    onChange={(e) =>
                      setForm((f) => ({ ...f, originalPrice: sanitizePriceInput(e.target.value) }))
                    }
                  />
                </FormField>
                <FormField label="With 18% GST">
                  <Input
                    readOnly
                    tabIndex={-1}
                    value={moneyLabel(form.originalPrice)}
                    className="w-full bg-background text-muted-foreground"
                  />
                </FormField>
              </div>
            </div>

            <div className="rounded-lg border bg-muted/30 p-3">
              <p className="mb-2 text-xs font-semibold tracking-wide text-foreground uppercase">
                Offered Price
              </p>
              <div className="grid grid-cols-2 gap-3">
                <FormField label="Without GST *">
                  <Input
                    type="text"
                    inputMode="decimal"
                    className="w-full"
                    placeholder="0"
                    value={form.offeredPrice}
                    onChange={(e) =>
                      setForm((f) => ({ ...f, offeredPrice: sanitizePriceInput(e.target.value) }))
                    }
                  />
                </FormField>
                <FormField label="With 18% GST">
                  <Input
                    readOnly
                    tabIndex={-1}
                    value={moneyLabel(form.offeredPrice)}
                    className="w-full bg-background text-muted-foreground"
                  />
                </FormField>
              </div>
            </div>

            <FormField label="Manage Deals (Coupon Code)">
              <Select
                value={form.dealId && form.dealId > 0 ? String(form.dealId) : 'none'}
                onValueChange={(v) =>
                  setForm((f) => ({ ...f, dealId: v === 'none' ? null : Number(v) }))
                }
              >
                <SelectTrigger className="w-full">
                  <SelectValue placeholder="None" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="none">None</SelectItem>
                  {dealOptions.map((d) => (
                    <SelectItem key={d.id} value={String(d.id)}>
                      {d.dealName}
                      {d.couponCode ? ` (${d.couponCode})` : ''}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </FormField>

            {editing && (
              <FormField label="Status">
                <Select
                  value={form.status || 'Active'}
                  onValueChange={(v) => setForm((f) => ({ ...f, status: v }))}
                >
                  <SelectTrigger className="w-full">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="Active">Active</SelectItem>
                    <SelectItem value="Inactive">Inactive</SelectItem>
                  </SelectContent>
                </Select>
              </FormField>
            )}
          </div>
          <DrawerFooter className="flex-row gap-2">
            <DrawerClose asChild>
              <Button variant="outline" className="flex-1" disabled={formBusy}>
                Cancel
              </Button>
            </DrawerClose>
            <Button className="flex-1" onClick={submitForm} disabled={formBusy}>
              {formBusy ? 'Saving…' : editing ? 'Update' : 'Create'}
            </Button>
          </DrawerFooter>
        </DrawerContent>
      </Drawer>

      <AlertDialog open={!!confirm} onOpenChange={(o) => !o && setConfirm(null)}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>
              {confirm?.mode === 'delete' ? 'Delete pricing plan?' : 'Toggle status?'}
            </AlertDialogTitle>
            <AlertDialogDescription>
              {confirm?.mode === 'delete'
                ? `Delete “${confirm.name}”? This cannot be undone.`
                : `Set “${confirm?.name}” to ${
                    confirm?.status?.toLowerCase() === 'active' ? 'Inactive' : 'Active'
                  }?`}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel disabled={confirmBusy}>Cancel</AlertDialogCancel>
            <AlertDialogAction onClick={runConfirm} disabled={confirmBusy}>
              {confirmBusy ? 'Please wait…' : 'Confirm'}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  )
}

