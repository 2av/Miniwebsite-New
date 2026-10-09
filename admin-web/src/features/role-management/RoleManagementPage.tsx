import { useQuery, useQueryClient } from '@tanstack/react-query'
import { Pencil, Plus, RefreshCw, Search, Shield, X } from 'lucide-react'
import { useEffect, useMemo, useState } from 'react'
import {
  createRole,
  fetchManageRoles,
  fetchManageRolesMeta,
  updateRole,
} from '@/features/role-management/api'
import type { ManageRoleRow, UpsertRolePayload } from '@/shared/types/api'
import { ApiError } from '@/shared/api/client'
import { FiltersButton, FiltersDrawer } from '@/shared/ui/FiltersDrawer'
import { useToast } from '@/shared/ui/Toast'
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
import { Textarea } from '@/components/ui/textarea'

const emptyForm: UpsertRolePayload = {
  roleName: '',
  roleDescription: '',
  prefix: '',
  salesType: '',
}

export function RoleManagementPage() {
  const toast = useToast()
  const qc = useQueryClient()
  const [page, setPage] = useState(1)
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [salesType, setSalesType] = useState('all')
  const [filtersOpen, setFiltersOpen] = useState(false)

  const [formOpen, setFormOpen] = useState(false)
  const [editing, setEditing] = useState<ManageRoleRow | null>(null)
  const [form, setForm] = useState<UpsertRolePayload>(emptyForm)
  const [formBusy, setFormBusy] = useState(false)
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
      salesType: salesType === 'all' ? undefined : salesType,
    }),
    [page, search, salesType],
  )
  const queryKey = useMemo(() => ['role-management', filters] as const, [filters])

  const listQuery = useQuery({
    queryKey,
    queryFn: () => fetchManageRoles(filters),
  })

  const metaQuery = useQuery({
    queryKey: ['role-management-meta'],
    queryFn: fetchManageRolesMeta,
    staleTime: Infinity,
  })

  const data = listQuery.data
  const roles = data?.roles ?? []
  const pages = data ? Math.max(1, Math.ceil(data.totalCount / data.pageSize)) : 1
  const hasSearch = Boolean(searchInput.trim())
  const activeFilterCount = (hasSearch ? 1 : 0) + (salesType !== 'all' ? 1 : 0)
  const salesTypes = metaQuery.data?.salesTypes ?? []

  const clearFilters = () => {
    setSearchInput('')
    setSearch('')
    setSalesType('all')
    setPage(1)
  }

  const openCreate = () => {
    setEditing(null)
    setForm({ ...emptyForm })
    setFormOpen(true)
  }

  const openEdit = (row: ManageRoleRow) => {
    setEditing(row)
    setForm({
      roleName: row.roleName,
      roleDescription: row.roleDescription,
      prefix: row.prefix,
      salesType: row.salesType,
    })
    setFormOpen(true)
  }

  const submitForm = async () => {
    if (!form.roleName.trim()) {
      return toast.push('Role name is required', 'error')
    }
    setFormBusy(true)
    try {
      const payload: UpsertRolePayload = {
        roleName: form.roleName.trim(),
        roleDescription: form.roleDescription?.trim() || '',
        prefix: form.prefix?.trim() || '',
        salesType: form.salesType?.trim() || '',
      }
      const res = editing
        ? await updateRole(editing.roleId, payload)
        : await createRole(payload)
      toast.push(res.message || (editing ? 'Role updated' : 'Role added'), 'success')
      setFormOpen(false)
      await qc.invalidateQueries({ queryKey: ['role-management'] })
    } catch (e) {
      toast.push(e instanceof ApiError ? e.message : 'Save failed', 'error')
    } finally {
      setFormBusy(false)
    }
  }

  return (
    <div className="flex h-full min-h-0 min-w-0 max-w-full flex-col gap-3">
      <div className="flex shrink-0 flex-wrap items-end justify-between gap-3">
        <div>
          <h1 className="font-[family-name:var(--font-display)] flex items-center gap-2 text-3xl font-semibold tracking-tight">
            <Shield size={28} className="text-rose-600" />
            Role Management
          </h1>
          <p className="mt-1 text-sm text-muted-foreground">{data?.totalCount ?? '—'} roles</p>
        </div>
        <div className="flex flex-wrap items-center gap-2">
          <FiltersButton activeCount={activeFilterCount} onClick={() => setFiltersOpen(true)} />
          <Button variant="outline" onClick={() => listQuery.refetch()}>
            <RefreshCw size={16} /> Refresh
          </Button>
          <Button onClick={openCreate}>
            <Plus size={16} /> Add Role
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
              placeholder="Role name, description, or prefix…"
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
          <label className="text-xs font-medium text-muted-foreground">Sales type</label>
          <Select
            value={salesType}
            onValueChange={(v) => {
              setSalesType(v)
              setPage(1)
            }}
          >
            <SelectTrigger className="w-full">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All</SelectItem>
              {salesTypes.map((p) => (
                <SelectItem key={p.value} value={p.value}>
                  {p.label}
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
                  {['RoleId', 'RoleName', 'RoleDescription', 'Prefix', 'SalesType', 'Action'].map((h) => (
                    <TableHead key={h} className="text-xs font-semibold tracking-wide text-slate-200 uppercase">
                      {h}
                    </TableHead>
                  ))}
                </TableRow>
              </TableHeader>
              <TableBody>
                {listQuery.isLoading && (
                  <TableRow>
                    <TableCell colSpan={6} className="py-10 text-center text-muted-foreground">
                      Loading…
                    </TableCell>
                  </TableRow>
                )}
                {listQuery.isError && (
                  <TableRow>
                    <TableCell colSpan={6} className="py-10 text-center text-destructive">
                      {(listQuery.error as Error).message}
                    </TableCell>
                  </TableRow>
                )}
                {!listQuery.isLoading && !listQuery.isError && roles.length === 0 && (
                  <TableRow>
                    <TableCell colSpan={6} className="py-10 text-center text-muted-foreground">
                      No roles found
                    </TableCell>
                  </TableRow>
                )}
                {roles.map((r) => (
                  <TableRow key={r.roleId}>
                    <TableCell className="font-medium">{r.roleId}</TableCell>
                    <TableCell className="font-medium">{r.roleName}</TableCell>
                    <TableCell className="max-w-[320px] text-muted-foreground">
                      <div className="line-clamp-2">{r.roleDescription || '—'}</div>
                    </TableCell>
                    <TableCell>
                      {r.prefix ? <Badge variant="secondary">{r.prefix}</Badge> : '—'}
                    </TableCell>
                    <TableCell>{r.salesType || '—'}</TableCell>
                    <TableCell>
                      <Button size="icon-sm" variant="ghost" title="Edit" onClick={() => openEdit(r)}>
                        <Pencil size={14} />
                      </Button>
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
        <DrawerContent className="data-[vaul-drawer-direction=right]:sm:max-w-lg">
          <DrawerHeader>
            <DrawerTitle>{editing ? 'Edit Role' : 'Add Role'}</DrawerTitle>
            <DrawerDescription>Define a role name, prefix, and sales type</DrawerDescription>
          </DrawerHeader>
          <div className="flex flex-1 flex-col gap-3 overflow-y-auto px-4 pb-2">
            <Field label="Role name *">
              <Input
                value={form.roleName}
                onChange={(e) => setForm((f) => ({ ...f, roleName: e.target.value }))}
                placeholder="e.g. Franchise Partner"
              />
            </Field>
            <Field label="Role description">
              <Textarea
                value={form.roleDescription ?? ''}
                onChange={(e) => setForm((f) => ({ ...f, roleDescription: e.target.value }))}
                rows={4}
                placeholder="Short description of this role"
              />
            </Field>
            <Field label="Prefix">
              <Input
                value={form.prefix ?? ''}
                onChange={(e) => setForm((f) => ({ ...f, prefix: e.target.value }))}
                placeholder="e.g. FR"
              />
            </Field>
            <Field label="Sales type">
              <Select
                value={form.salesType || undefined}
                onValueChange={(v) => setForm((f) => ({ ...f, salesType: v }))}
              >
                <SelectTrigger className="w-full">
                  <SelectValue placeholder="Select sales type" />
                </SelectTrigger>
                <SelectContent>
                  {salesTypes.map((p) => (
                    <SelectItem key={p.value} value={p.value}>
                      {p.label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </Field>
          </div>
          <DrawerFooter>
            <DrawerClose asChild>
              <Button type="button" variant="outline" disabled={formBusy}>
                Cancel
              </Button>
            </DrawerClose>
            <Button type="button" disabled={formBusy} onClick={() => void submitForm()}>
              {formBusy ? 'Saving…' : editing ? 'Update Role' : 'Add Role'}
            </Button>
          </DrawerFooter>
        </DrawerContent>
      </Drawer>
    </div>
  )
}

function Field({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div className="space-y-1.5">
      <label className="text-xs font-medium text-muted-foreground">{label}</label>
      {children}
    </div>
  )
}
