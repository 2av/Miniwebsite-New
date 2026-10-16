import { useQuery, useQueryClient } from '@tanstack/react-query'
import { HandCoins, Pencil, Plus, Power, RefreshCw, Search, Trash2 } from 'lucide-react'
import { useEffect, useMemo, useState } from 'react'
import {
  createPlanCommissions,
  deletePlanCommission,
  fetchPlanCommissions,
  fetchPlanCommissionsMeta,
  togglePlanCommissionStatus,
  updatePlanCommission,
} from '@/features/plan-commissions/api'
import type { PlanCommissionRow, UpsertPlanCommissionPayload } from '@/shared/types/api'
import { ApiError } from '@/shared/api/client'
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
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'
import { Checkbox } from '@/components/ui/checkbox'
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

const FIXED = 'Fixed'

type FormState = {
  roleIds: number[]
  pricingPlanId: string
  discountType: string
  /** Keep amounts as strings so the user can clear the field while typing */
  discountAmount: string
  earningType: string
  earningAmount: string
  /** Empty means no renewal commission % */
  renewalEarningPercent: string
  status: string
}

const emptyForm: FormState = {
  roleIds: [],
  pricingPlanId: '',
  discountType: FIXED,
  discountAmount: '',
  earningType: FIXED,
  earningAmount: '',
  renewalEarningPercent: '',
  status: 'Active',
}

const GRID_HEADERS = [
  'Commission Name',
  'Original Price',
  'List Price',
  'Discount',
  'Commissions',
  'Renewal (%)',
  'Action',
] as const

type ConfirmState =
  | { mode: 'delete'; id: number; name: string }
  | { mode: 'toggle'; id: number; name: string; status: string }

function toAmount(raw: string) {
  const n = Number(raw)
  return Number.isFinite(n) && n >= 0 ? n : 0
}

/** Allow digits and one decimal point while typing; empty string is allowed. */
function sanitizeAmount(raw: string) {
  const cleaned = raw.replace(/[^\d.]/g, '')
  const parts = cleaned.split('.')
  if (parts.length <= 1) return cleaned
  return `${parts[0]}.${parts.slice(1).join('').slice(0, 2)}`
}

function money(n: number | null | undefined) {
  if (n == null || !Number.isFinite(n)) return '—'
  return `₹${n.toLocaleString('en-IN', { maximumFractionDigits: 2 })}`
}

/** Prefer API display string; fall back to numeric amount (camelCase or PascalCase / string). */
function priceLabel(
  row: Record<string, unknown>,
  displayKeys: string[],
  amountKeys: string[],
) {
  for (const k of displayKeys) {
    const v = row[k]
    if (typeof v === 'string' && v.trim() !== '' && !/^[—\-]?$/.test(v.trim())) return v
  }
  for (const k of amountKeys) {
    const v = row[k]
    if (typeof v === 'number' && Number.isFinite(v)) return money(v)
    if (typeof v === 'string' && v.trim() !== '') {
      const n = Number(v)
      if (Number.isFinite(n)) return money(n)
    }
  }
  return '—'
}

export function PlanCommissionsPage() {
  const toast = useToast()
  const qc = useQueryClient()
  const [page, setPage] = useState(1)
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [roleFilter, setRoleFilter] = useState('all')
  const [planFilter, setPlanFilter] = useState('all')
  const [status, setStatus] = useState('all')
  const [filtersOpen, setFiltersOpen] = useState(false)

  const [formOpen, setFormOpen] = useState(false)
  const [editing, setEditing] = useState<PlanCommissionRow | null>(null)
  const [form, setForm] = useState<FormState>(emptyForm)
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
      roleId: roleFilter === 'all' ? undefined : Number(roleFilter),
      pricingPlanId: planFilter === 'all' ? undefined : Number(planFilter),
      status: status === 'all' ? undefined : status,
    }),
    [page, search, roleFilter, planFilter, status],
  )

  const listQuery = useQuery({
    queryKey: ['plan-commissions', filters],
    queryFn: () => fetchPlanCommissions(filters),
  })

  const metaQuery = useQuery({
    queryKey: ['plan-commissions-meta'],
    queryFn: fetchPlanCommissionsMeta,
  })

  const data = listQuery.data
  const items = data?.items ?? []
  const pages = data ? Math.max(1, Math.ceil(data.totalCount / data.pageSize)) : 1
  const activeFilterCount =
    (searchInput.trim() ? 1 : 0) + (planFilter !== 'all' ? 1 : 0) + (status !== 'all' ? 1 : 0)

  const resetSecondaryFilters = () => {
    setSearchInput('')
    setSearch('')
    setPlanFilter('all')
    setStatus('all')
    setPage(1)
  }

  const onRoleFilterChange = (value: string) => {
    setRoleFilter(value)
    resetSecondaryFilters()
  }

  const roles = metaQuery.data?.roles ?? []
  const plans = metaQuery.data?.plans ?? []
  const discountTypes = metaQuery.data?.discountTypes ?? [FIXED]
  const earningTypes = metaQuery.data?.earningTypes ?? [FIXED]

  const selectedPlan = plans.find((p) => String(p.id) === form.pricingPlanId)

  const clearFilters = () => {
    resetSecondaryFilters()
  }

  const openCreate = () => {
    setEditing(null)
    setForm({ ...emptyForm })
    setFormOpen(true)
  }

  const openEdit = (row: PlanCommissionRow) => {
    setEditing(row)
    setForm({
      roleIds: [row.roleId],
      pricingPlanId: String(row.pricingPlanId),
      discountType: row.discountType,
      discountAmount: row.discountType === FIXED ? String(row.discountAmount) : '',
      earningType: row.earningType,
      earningAmount: row.earningType === FIXED ? String(row.earningAmount) : '',
      renewalEarningPercent: row.renewalEarningPercent == null ? '' : String(row.renewalEarningPercent),
      status: row.status,
    })
    setFormOpen(true)
  }

  const toggleRole = (roleId: number, checked: boolean) =>
    setForm((f) => ({
      ...f,
      roleIds: checked ? [...f.roleIds, roleId] : f.roleIds.filter((id) => id !== roleId),
    }))

  const validate = (): string | null => {
    if (form.roleIds.length === 0) return 'Please select a role'
    if (!selectedPlan) return 'Please select a commission name (plan)'
    if (form.discountType === FIXED && form.discountAmount.trim() === '') return 'Please enter the discount'
    if (form.earningType === FIXED && form.earningAmount.trim() === '') return 'Please enter the commission'
    if (form.renewalEarningPercent.trim() !== '') {
      const pct = Number(form.renewalEarningPercent)
      if (!Number.isFinite(pct) || pct <= 0 || pct > 100) return 'Renewal % must be between 0 and 100'
    }
    const discount = form.discountType === FIXED ? toAmount(form.discountAmount) : 0
    const commission = form.earningType === FIXED ? toAmount(form.earningAmount) : 0
    if (discount + commission > selectedPlan.offeredPrice)
      return 'Discount plus commission cannot be more than the list price'
    return null
  }

  const submitForm = async () => {
    const error = validate()
    if (error) return toast.push(error, 'error')
    setFormBusy(true)
    try {
      const payload: UpsertPlanCommissionPayload = {
        roleIds: form.roleIds,
        pricingPlanId: Number(form.pricingPlanId),
        discountType: form.discountType,
        discountAmount: form.discountType === FIXED ? toAmount(form.discountAmount) : 0,
        earningType: form.earningType,
        earningAmount: form.earningType === FIXED ? toAmount(form.earningAmount) : 0,
        renewalPrice: null,
        renewalEarningPercent:
          form.renewalEarningPercent.trim() === '' ? null : Number(form.renewalEarningPercent),
        status: editing ? form.status : undefined,
      }
      const res = editing
        ? await updatePlanCommission(editing.id, payload)
        : await createPlanCommissions(payload)
      toast.push(res.message || (editing ? 'Commission updated' : 'Commission added'), 'success')
      setFormOpen(false)
      await qc.invalidateQueries({ queryKey: ['plan-commissions'] })
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
          ? await deletePlanCommission(confirm.id)
          : await togglePlanCommissionStatus(confirm.id)
      toast.push(res.message || 'Done', 'success')
      setConfirm(null)
      await qc.invalidateQueries({ queryKey: ['plan-commissions'] })
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
            <HandCoins size={28} className="text-emerald-700" />
            Plan Commissions
          </h1>
          <p className="mt-1 text-sm text-muted-foreground">
            {data?.totalCount ?? '—'} role terms · discount and partner earning per plan
          </p>
        </div>
        <div className="flex flex-wrap items-center gap-2">
          <FiltersButton activeCount={activeFilterCount} onClick={() => setFiltersOpen(true)} />
          <Button variant="outline" size="sm" onClick={() => listQuery.refetch()} disabled={listQuery.isFetching}>
            <RefreshCw size={16} className={listQuery.isFetching ? 'animate-spin' : ''} />
            Refresh
          </Button>
          <Button size="sm" onClick={openCreate}>
            <Plus size={16} />
            Add Commission
          </Button>
        </div>
      </div>

      <Card className="flex min-h-0 min-w-0 max-w-full flex-1 flex-col overflow-hidden py-0">
        <CardContent className="flex min-h-0 flex-1 flex-col p-0">
          <div className="flex shrink-0 flex-wrap items-center gap-2 border-b px-4 py-3">
            <label className="text-sm font-medium text-muted-foreground">Role</label>
            <Select value={roleFilter} onValueChange={onRoleFilterChange}>
              <SelectTrigger className="w-[220px]">
                <SelectValue placeholder="All roles" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">All roles</SelectItem>
                {roles.map((r) => (
                  <SelectItem key={r.roleId} value={String(r.roleId)}>
                    {r.roleName}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          {listQuery.isError && (
            <div className="m-4 rounded-md border border-destructive/30 bg-destructive/5 px-3 py-2 text-sm text-destructive">
              {(listQuery.error as Error)?.message || 'Failed to load'}
            </div>
          )}

          <div className="min-h-0 flex-1 overflow-auto">
            <Table className="w-max min-w-full">
              <TableHeader className="sticky top-0 z-10 bg-slate-900">
                <TableRow className="border-slate-800 hover:bg-slate-900">
                  {GRID_HEADERS.map((h) => (
                    <TableHead key={h} className="text-xs font-semibold tracking-wide text-slate-200 uppercase">
                      {h}
                    </TableHead>
                  ))}
                </TableRow>
              </TableHeader>
              <TableBody>
                {listQuery.isLoading && (
                  <TableRow>
                    <TableCell colSpan={GRID_HEADERS.length} className="py-10 text-center text-muted-foreground">
                      Loading…
                    </TableCell>
                  </TableRow>
                )}
                {!listQuery.isLoading && items.length === 0 && (
                  <TableRow>
                    <TableCell colSpan={GRID_HEADERS.length} className="py-10 text-center text-muted-foreground">
                      No plan commissions found.
                    </TableCell>
                  </TableRow>
                )}
                {items.map((row) => {
                  const r = row as PlanCommissionRow & Record<string, unknown>
                  return (
                  <TableRow key={row.id}>
                    <TableCell className="max-w-[16rem] whitespace-normal font-medium">
                      {row.planName || String(r.PlanName ?? '')}
                    </TableCell>
                    <TableCell>
                      {priceLabel(r, ['originalPriceDisplay', 'OriginalPriceDisplay'], [
                        'originalPrice',
                        'OriginalPrice',
                      ])}
                    </TableCell>
                    <TableCell>
                      {priceLabel(r, ['offeredPriceDisplay', 'OfferedPriceDisplay'], [
                        'offeredPrice',
                        'OfferedPrice',
                      ])}
                    </TableCell>
                    <TableCell>{row.discountDisplay || String(r.DiscountDisplay ?? '—')}</TableCell>
                    <TableCell>{row.earningDisplay || String(r.EarningDisplay ?? '—')}</TableCell>
                    <TableCell>
                      {row.renewalEarningPercent == null && r.RenewalEarningPercent == null
                        ? '—'
                        : `${row.renewalEarningPercent ?? r.RenewalEarningPercent}%`}
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
                              name: `${row.roleName} · ${row.planName}`,
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
                          onClick={() =>
                            setConfirm({ mode: 'delete', id: row.id, name: `${row.roleName} · ${row.planName}` })
                          }
                        >
                          <Trash2 size={14} />
                        </Button>
                      </div>
                    </TableCell>
                  </TableRow>
                  )
                })}
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
        title="Filter plan commissions"
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
                placeholder="Plan name…"
              />
            </div>
          </div>
          <div className="space-y-2">
            <label className="text-sm font-medium">Plan</label>
            <Select
              value={planFilter}
              onValueChange={(v) => {
                setPlanFilter(v)
                setPage(1)
              }}
            >
              <SelectTrigger>
                <SelectValue placeholder="All" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">All</SelectItem>
                {plans.map((p) => (
                  <SelectItem key={p.id} value={String(p.id)}>
                    {p.planName}
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
                <SelectItem value="InActive">InActive</SelectItem>
              </SelectContent>
            </Select>
          </div>
        </div>
      </FiltersDrawer>

      <Drawer open={formOpen} onOpenChange={setFormOpen} direction="right">
        <DrawerContent className="data-[vaul-drawer-direction=right]:sm:max-w-lg">
          <DrawerHeader>
            <DrawerTitle>{editing ? 'Edit Commission' : 'Add Commission'}</DrawerTitle>
            <DrawerDescription>
              Original Price and List Price come from Plan Pricing when you pick a commission name.
            </DrawerDescription>
          </DrawerHeader>
          <div className="flex flex-1 flex-col gap-3 overflow-y-auto px-4 pb-2">
            {editing ? (
              <FormField label="Role *">
                <Select
                  value={form.roleIds[0] ? String(form.roleIds[0]) : undefined}
                  onValueChange={(v) => setForm((f) => ({ ...f, roleIds: [Number(v)] }))}
                >
                  <SelectTrigger className="w-full">
                    <SelectValue placeholder="Select Role" />
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
            ) : (
              <FormField label="Roles *">
                <div className="grid grid-cols-2 gap-2 rounded-md border p-3">
                  {roles.map((r) => (
                    <label key={r.roleId} className="flex cursor-pointer items-center gap-2 text-sm">
                      <Checkbox
                        checked={form.roleIds.includes(r.roleId)}
                        onCheckedChange={(c) => toggleRole(r.roleId, c === true)}
                      />
                      {r.roleName}
                    </label>
                  ))}
                </div>
                <p className="mt-1 text-xs text-muted-foreground">
                  Roles with the same terms can be added together; one row is saved per role.
                </p>
              </FormField>
            )}

            <FormField label="Commission Name *">
              <Select
                value={form.pricingPlanId || undefined}
                onValueChange={(v) => setForm((f) => ({ ...f, pricingPlanId: v }))}
              >
                <SelectTrigger className="w-full">
                  <SelectValue placeholder="Select from Plan Pricing" />
                </SelectTrigger>
                <SelectContent>
                  {plans.map((p) => (
                    <SelectItem key={p.id} value={String(p.id)}>
                      {p.planName}
                      {p.status !== 'Active' ? ' (Inactive)' : ''}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </FormField>

            <div className="grid grid-cols-2 gap-3">
              <FormField label="Original Price">
                <Input
                  readOnly
                  tabIndex={-1}
                  value={
                    selectedPlan
                      ? priceLabel(selectedPlan as unknown as Record<string, unknown>, [], [
                          'originalPrice',
                          'OriginalPrice',
                        ])
                      : '—'
                  }
                  className="bg-muted/40 text-muted-foreground"
                />
              </FormField>
              <FormField label="List Price">
                <Input
                  readOnly
                  tabIndex={-1}
                  value={
                    selectedPlan
                      ? priceLabel(selectedPlan as unknown as Record<string, unknown>, [], [
                          'offeredPrice',
                          'OfferedPrice',
                        ])
                      : '—'
                  }
                  className="bg-muted/40 text-muted-foreground"
                />
              </FormField>
            </div>

            <div className="grid grid-cols-2 gap-3">
              <FormField label="Discount *">
                <Select
                  value={form.discountType}
                  onValueChange={(v) => setForm((f) => ({ ...f, discountType: v }))}
                >
                  <SelectTrigger className="w-full">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {discountTypes.map((t) => (
                      <SelectItem key={t} value={t}>
                        {t}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </FormField>
              <FormField label="Discount (₹)">
                <Input
                  inputMode="decimal"
                  placeholder={form.discountType === FIXED ? '0' : 'Not fixed'}
                  disabled={form.discountType !== FIXED}
                  value={form.discountType === FIXED ? form.discountAmount : ''}
                  onChange={(e) => setForm((f) => ({ ...f, discountAmount: sanitizeAmount(e.target.value) }))}
                />
              </FormField>
            </div>

            <div className="grid grid-cols-2 gap-3">
              <FormField label="Commissions *">
                <Select
                  value={form.earningType}
                  onValueChange={(v) => setForm((f) => ({ ...f, earningType: v }))}
                >
                  <SelectTrigger className="w-full">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {earningTypes.map((t) => (
                      <SelectItem key={t} value={t}>
                        {t}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </FormField>
              <FormField label="Commission (₹)">
                <Input
                  inputMode="decimal"
                  placeholder={form.earningType === FIXED ? '0' : 'Not fixed'}
                  disabled={form.earningType !== FIXED}
                  value={form.earningType === FIXED ? form.earningAmount : ''}
                  onChange={(e) => setForm((f) => ({ ...f, earningAmount: sanitizeAmount(e.target.value) }))}
                />
              </FormField>
            </div>

            <FormField label="Renewal (%)">
              <Input
                inputMode="decimal"
                placeholder="Leave empty for no renewal commission"
                value={form.renewalEarningPercent}
                onChange={(e) =>
                  setForm((f) => ({ ...f, renewalEarningPercent: sanitizeAmount(e.target.value) }))
                }
              />
              <p className="mt-1 text-xs text-muted-foreground">Percent of list price from Plan Pricing.</p>
            </FormField>

            {editing && (
              <FormField label="Status">
                <Select value={form.status} onValueChange={(v) => setForm((f) => ({ ...f, status: v }))}>
                  <SelectTrigger className="w-full">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="Active">Active</SelectItem>
                    <SelectItem value="InActive">InActive</SelectItem>
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
              {confirm?.mode === 'delete' ? 'Delete plan commission?' : 'Toggle status?'}
            </AlertDialogTitle>
            <AlertDialogDescription>
              {confirm?.mode === 'delete'
                ? `Delete “${confirm.name}”? This cannot be undone.`
                : `Set “${confirm?.name}” to ${confirm?.status === 'Active' ? 'InActive' : 'Active'}?`}
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
