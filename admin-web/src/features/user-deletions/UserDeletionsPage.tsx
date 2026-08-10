import { useQuery, useQueryClient } from '@tanstack/react-query'
import { RefreshCw, RotateCcw, Search, Trash2, X } from 'lucide-react'
import { useEffect, useMemo, useState } from 'react'
import { fetchUserDeletions, restoreUsers, softDeleteUsers } from '@/features/user-deletions/api'
import type { UserDeletionRow } from '@/shared/types/api'
import { ApiError } from '@/shared/api/client'
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
import { Checkbox } from '@/components/ui/checkbox'
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

function formatDate(value?: string | null) {
  if (!value) return '-'
  const d = new Date(value)
  if (Number.isNaN(d.getTime())) return '-'
  return d.toLocaleDateString('en-IN', { day: '2-digit', month: 'short', year: 'numeric' })
}

type LockedRole = 'CUSTOMER' | 'FRANCHISEE' | 'TEAM'

type ConfirmState = {
  mode: 'soft-delete' | 'restore'
  ids: number[]
  label: string
}

const ROLE_TITLES: Record<LockedRole, string> = {
  CUSTOMER: 'Customer',
  FRANCHISEE: 'Franchisee',
  TEAM: 'Team',
}

export function UserDeletionsPage({ lockedRole }: { lockedRole: LockedRole }) {
  const toast = useToast()
  const qc = useQueryClient()
  const [page, setPage] = useState(1)
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [status, setStatus] = useState('all')
  const [filtersOpen, setFiltersOpen] = useState(false)
  const [selected, setSelected] = useState<number[]>([])
  const [confirm, setConfirm] = useState<ConfirmState | null>(null)
  const [busy, setBusy] = useState(false)

  useEffect(() => {
    setPage(1)
    setSearchInput('')
    setSearch('')
    setStatus('all')
    setSelected([])
  }, [lockedRole])

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
      role: lockedRole,
      status: status === 'all' ? undefined : status,
    }),
    [page, search, lockedRole, status],
  )
  const queryKey = useMemo(() => ['user-deletions', filters] as const, [filters])

  const listQuery = useQuery({
    queryKey,
    queryFn: () => fetchUserDeletions(filters),
  })

  const data = listQuery.data
  const users = data?.users ?? []
  const pages = data ? Math.max(1, Math.ceil(data.totalCount / data.pageSize)) : 1
  const hasSearch = Boolean(searchInput.trim())
  const activeFilterCount = (hasSearch ? 1 : 0) + (status !== 'all' ? 1 : 0)
  const allSelected = users.length > 0 && users.every((u) => selected.includes(u.id))
  const roleLabel = ROLE_TITLES[lockedRole]

  const clearFilters = () => {
    setSearchInput('')
    setSearch('')
    setStatus('all')
    setPage(1)
  }

  useEffect(() => {
    setSelected([])
  }, [page, search, lockedRole, status])

  const invalidate = () => qc.invalidateQueries({ queryKey: ['user-deletions'] })

  const askSoftDelete = (ids: number[], label: string) => {
    setConfirm({ mode: 'soft-delete', ids, label })
  }

  const askRestore = (ids: number[], label: string) => {
    setConfirm({ mode: 'restore', ids, label })
  }

  const runConfirm = async () => {
    if (!confirm || confirm.ids.length === 0) return
    setBusy(true)
    try {
      const res =
        confirm.mode === 'soft-delete'
          ? await softDeleteUsers(confirm.ids)
          : await restoreUsers(confirm.ids)
      toast.push(
        res?.message || (confirm.mode === 'soft-delete' ? 'Users marked as deleted' : 'Users restored'),
        'success',
      )
      setSelected([])
      setConfirm(null)
      await invalidate()
    } catch (e) {
      toast.push(e instanceof ApiError ? e.message : 'Request failed', 'error')
    } finally {
      setBusy(false)
    }
  }

  const toggleAll = (checked: boolean) => {
    if (checked) setSelected(users.map((u) => u.id))
    else setSelected([])
  }

  const toggleOne = (id: number, checked: boolean) => {
    setSelected((prev) => (checked ? [...prev, id] : prev.filter((x) => x !== id)))
  }

  return (
    <div className="flex h-full min-h-0 min-w-0 max-w-full flex-col gap-3">
      <div className="flex shrink-0 flex-wrap items-end justify-between gap-3">
        <div>
          <h1 className="font-[family-name:var(--font-display)] text-3xl font-semibold tracking-tight">
            User Deletion — {roleLabel}
          </h1>
          <p className="mt-1 text-sm text-muted-foreground">
            {data?.totalCount ?? '—'} · {data?.activeCount ?? '—'} active · {data?.deletedCount ?? '—'} deleted
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
        description={`Filter ${roleLabel.toLowerCase()} accounts`}
        onClear={activeFilterCount > 0 ? clearFilters : undefined}
      >
        <div className="grid grid-cols-3 gap-2 rounded-lg border bg-muted/40 p-3 text-center">
          <div>
            <div className="text-xs text-muted-foreground">Total</div>
            <div className="text-lg font-semibold">{data?.totalCount ?? '—'}</div>
          </div>
          <div>
            <div className="text-xs text-muted-foreground">Active</div>
            <div className="text-lg font-semibold text-emerald-700">{data?.activeCount ?? '—'}</div>
          </div>
          <div>
            <div className="text-xs text-muted-foreground">Deleted</div>
            <div className="text-lg font-semibold text-rose-700">{data?.deletedCount ?? '—'}</div>
          </div>
        </div>
        <div className="space-y-1.5">
          <label className="text-xs font-medium text-muted-foreground">Search</label>
          <div className="relative">
            <Search size={16} className="pointer-events-none absolute top-2.5 left-3 text-muted-foreground" />
            <Input
              className="pr-9 pl-9"
              placeholder="Name or email…"
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
          <label className="text-xs font-medium text-muted-foreground">Deletion status</label>
          <Select
            value={status}
            onValueChange={(v) => {
              setStatus(v)
              setPage(1)
            }}
          >
            <SelectTrigger className="w-full">
              <SelectValue placeholder="All" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All</SelectItem>
              <SelectItem value="active">Active (not deleted)</SelectItem>
              <SelectItem value="deleted">Deleted</SelectItem>
            </SelectContent>
          </Select>
        </div>
      </FiltersDrawer>

      {selected.length > 0 && (
        <Card className="shrink-0 border-sky-200 bg-sky-50">
          <CardContent className="flex flex-wrap items-center justify-between gap-3 py-3">
            <div className="text-sm font-semibold text-sky-900">{selected.length} selected</div>
            <div className="flex gap-2">
              <Button
                variant="outline"
                onClick={() => askSoftDelete(selected, `${selected.length} selected user(s)`)}
              >
                <Trash2 size={16} /> Soft Delete
              </Button>
              <Button onClick={() => askRestore(selected, `${selected.length} selected user(s)`)}>
                <RotateCcw size={16} /> Restore
              </Button>
            </div>
          </CardContent>
        </Card>
      )}

      <Card className="flex min-h-0 min-w-0 max-w-full flex-1 flex-col overflow-hidden py-0">
        <CardContent className="flex min-h-0 flex-1 flex-col p-0">
          <div className="min-h-0 flex-1 overflow-auto">
            <Table>
              <TableHeader className="sticky top-0 z-10 bg-slate-900">
                <TableRow className="border-slate-800 hover:bg-slate-900">
                  <TableHead className="w-10 text-slate-200">
                    <Checkbox
                      checked={allSelected}
                      onCheckedChange={(v) => toggleAll(v === true)}
                      aria-label="Select all"
                      className="border-slate-400"
                    />
                  </TableHead>
                  {['ID', 'Name', 'Email', 'Role', 'Status', 'Deleted', 'Created', 'Updated', 'Actions'].map(
                    (h) => (
                      <TableHead key={h} className="text-xs font-semibold tracking-wide text-slate-200 uppercase">
                        {h}
                      </TableHead>
                    ),
                  )}
                </TableRow>
              </TableHeader>
              <TableBody>
                {listQuery.isLoading && (
                  <TableRow>
                    <TableCell colSpan={10} className="py-10 text-center text-muted-foreground">
                      Loading…
                    </TableCell>
                  </TableRow>
                )}
                {listQuery.isError && (
                  <TableRow>
                    <TableCell colSpan={10} className="py-10 text-center text-destructive">
                      {(listQuery.error as Error).message}
                    </TableCell>
                  </TableRow>
                )}
                {!listQuery.isLoading && !listQuery.isError && users.length === 0 && (
                  <TableRow>
                    <TableCell colSpan={10} className="py-10 text-center text-muted-foreground">
                      No users found
                    </TableCell>
                  </TableRow>
                )}
                {users.map((u) => (
                  <Row
                    key={u.id}
                    user={u}
                    checked={selected.includes(u.id)}
                    onToggle={(checked) => toggleOne(u.id, checked)}
                    onSoftDelete={() => askSoftDelete([u.id], u.email)}
                    onRestore={() => askRestore([u.id], u.email)}
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

      <AlertDialog open={!!confirm} onOpenChange={(open) => !open && !busy && setConfirm(null)}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>
              {confirm?.mode === 'soft-delete' ? 'Confirm soft delete' : 'Confirm restore'}
            </AlertDialogTitle>
            <AlertDialogDescription>
              {confirm?.mode === 'soft-delete' ? (
                <>
                  Soft-delete <strong>{confirm.label}</strong>? The account will be marked as deleted and
                  hidden from normal listings. You can restore it later.
                </>
              ) : (
                <>
                  Restore <strong>{confirm?.label}</strong>? The account will become active again.
                </>
              )}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel disabled={busy}>Cancel</AlertDialogCancel>
            <AlertDialogAction
              disabled={busy}
              variant={confirm?.mode === 'soft-delete' ? 'destructive' : 'default'}
              onClick={(e) => {
                e.preventDefault()
                void runConfirm()
              }}
            >
              {busy
                ? 'Please wait…'
                : confirm?.mode === 'soft-delete'
                  ? 'Soft Delete'
                  : 'Restore'}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  )
}

function Row({
  user: u,
  checked,
  onToggle,
  onSoftDelete,
  onRestore,
}: {
  user: UserDeletionRow
  checked: boolean
  onToggle: (checked: boolean) => void
  onSoftDelete: () => void
  onRestore: () => void
}) {
  return (
    <TableRow className={checked ? 'bg-sky-50/70' : undefined}>
      <TableCell>
        <Checkbox
          checked={checked}
          onCheckedChange={(v) => onToggle(v === true)}
          aria-label={`Select ${u.email}`}
        />
      </TableCell>
      <TableCell className="font-medium">{u.id}</TableCell>
      <TableCell>{u.name}</TableCell>
      <TableCell className="max-w-[200px] truncate">{u.email}</TableCell>
      <TableCell>
        <Badge variant="secondary">{u.role}</Badge>
      </TableCell>
      <TableCell>{u.status}</TableCell>
      <TableCell>
        <Badge variant={u.isDeleted ? 'destructive' : 'default'}>
          {u.isDeleted ? 'Deleted' : 'Active'}
        </Badge>
      </TableCell>
      <TableCell className="whitespace-nowrap text-muted-foreground">{formatDate(u.createdAt)}</TableCell>
      <TableCell className="whitespace-nowrap text-muted-foreground">{formatDate(u.updatedAt)}</TableCell>
      <TableCell>
        {u.isDeleted ? (
          <Button variant="outline" size="sm" onClick={onRestore}>
            <RotateCcw size={14} /> Restore
          </Button>
        ) : (
          <Button variant="outline" size="sm" onClick={onSoftDelete}>
            <Trash2 size={14} /> Soft Delete
          </Button>
        )}
      </TableCell>
    </TableRow>
  )
}
