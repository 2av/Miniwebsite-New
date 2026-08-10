import { useQuery, useQueryClient } from '@tanstack/react-query'
import {
  Download,
  Eye,
  KeyRound,
  Pencil,
  Plus,
  RefreshCw,
  Search,
  UsersRound,
  X,
} from 'lucide-react'
import { useEffect, useMemo, useState, type ReactNode } from 'react'
import {
  createTeamMember,
  exportTeamTrackerCsv,
  fetchManageTeams,
  fetchTeamReferrals,
  fetchTeamTracker,
  resetTeamPassword,
  toggleTeamStatus,
  updateTeamMember,
} from '@/features/manage-teams/api'
import { fetchDashboardDetails } from '@/features/manage-users/api'
import type { ManageTeamRow } from '@/shared/types/api'
import { ApiError } from '@/shared/api/client'
import { badgeVariantFromTone } from '@/shared/lib/badgeTone'
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
  name: string
  email: string
  phone: string
  district: string
  state: string
  password: string
}

const emptyForm: FormState = {
  name: '',
  email: '',
  phone: '',
  district: '',
  state: '',
  password: '',
}

function mwUrl(id: number) {
  return `${window.location.origin}/n.php?n=${id}`
}

export function ManageTeamsPage() {
  const toast = useToast()
  const qc = useQueryClient()

  const [page, setPage] = useState(1)
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [status, setStatus] = useState('all')
  const [filtersOpen, setFiltersOpen] = useState(false)

  const [formOpen, setFormOpen] = useState(false)
  const [editing, setEditing] = useState<ManageTeamRow | null>(null)
  const [form, setForm] = useState<FormState>(emptyForm)
  const [formBusy, setFormBusy] = useState(false)

  const [resetTarget, setResetTarget] = useState<ManageTeamRow | null>(null)
  const [newPassword, setNewPassword] = useState('')
  const [resetBusy, setResetBusy] = useState(false)
  const [togglingId, setTogglingId] = useState<number | null>(null)

  const [dashboardEmail, setDashboardEmail] = useState<string | null>(null)
  const [referralsId, setReferralsId] = useState<number | null>(null)
  const [trackerId, setTrackerId] = useState<number | null>(null)

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
  const queryKey = useMemo(() => ['manage-teams', filters] as const, [filters])

  const listQuery = useQuery({
    queryKey,
    queryFn: () => fetchManageTeams(filters),
  })

  const dashboardQuery = useQuery({
    queryKey: ['dashboard-details', dashboardEmail],
    queryFn: () => fetchDashboardDetails(dashboardEmail!),
    enabled: !!dashboardEmail,
  })

  const referralsQuery = useQuery({
    queryKey: ['team-referrals', referralsId],
    queryFn: () => fetchTeamReferrals(referralsId!),
    enabled: !!referralsId,
  })

  const trackerQuery = useQuery({
    queryKey: ['team-tracker', trackerId],
    queryFn: () => fetchTeamTracker(trackerId!),
    enabled: !!trackerId,
  })

  const data = listQuery.data
  const members = data?.members ?? []
  const pages = data ? Math.max(1, Math.ceil(data.totalCount / data.pageSize)) : 1
  const hasSearch = Boolean(searchInput.trim())
  const activeFilterCount = (hasSearch ? 1 : 0) + (status !== 'all' ? 1 : 0)

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

  const openEdit = (row: ManageTeamRow) => {
    setEditing(row)
    setForm({
      name: row.name,
      email: row.email,
      phone: row.phone,
      district: row.district,
      state: row.state,
      password: '',
    })
    setFormOpen(true)
  }

  const submitForm = async () => {
    if (!form.name.trim()) return toast.push('Member name is required', 'error')
    if (!form.email.trim() || !form.email.includes('@'))
      return toast.push('A valid email is required', 'error')
    if (!editing && form.password.trim().length < 6)
      return toast.push('Password must be at least 6 characters', 'error')

    setFormBusy(true)
    try {
      if (editing) {
        const res = await updateTeamMember(editing.id, {
          name: form.name.trim(),
          email: form.email.trim(),
          phone: form.phone.trim(),
          district: form.district.trim(),
          state: form.state.trim(),
        })
        toast.push(res.message || 'Team member updated', 'success')
      } else {
        const res = await createTeamMember({
          name: form.name.trim(),
          email: form.email.trim(),
          phone: form.phone.trim(),
          district: form.district.trim(),
          state: form.state.trim(),
          password: form.password,
        })
        toast.push(res.message || 'Team member added', 'success')
      }
      setFormOpen(false)
      await qc.invalidateQueries({ queryKey: ['manage-teams'] })
    } catch (e) {
      toast.push(e instanceof ApiError ? e.message : 'Save failed', 'error')
    } finally {
      setFormBusy(false)
    }
  }

  const runToggle = async (row: ManageTeamRow) => {
    setTogglingId(row.id)
    try {
      const next = row.status === 'ACTIVE' ? 'INACTIVE' : 'ACTIVE'
      const res = await toggleTeamStatus(row.id, next)
      toast.push(res.message || 'Status updated', 'success')
      await qc.invalidateQueries({ queryKey: ['manage-teams'] })
    } catch (e) {
      toast.push(e instanceof ApiError ? e.message : 'Status update failed', 'error')
    } finally {
      setTogglingId(null)
    }
  }

  const runReset = async () => {
    if (!resetTarget) return
    if (newPassword.trim().length < 6)
      return toast.push('Password must be at least 6 characters', 'error')
    setResetBusy(true)
    try {
      const res = await resetTeamPassword(resetTarget.id, newPassword.trim())
      toast.push(res.message || 'Password reset', 'success')
      setResetTarget(null)
      setNewPassword('')
    } catch (e) {
      toast.push(e instanceof ApiError ? e.message : 'Reset failed', 'error')
    } finally {
      setResetBusy(false)
    }
  }

  return (
    <div className="flex h-full min-h-0 min-w-0 max-w-full flex-col gap-3">
      <div className="flex shrink-0 flex-wrap items-end justify-between gap-3">
        <div>
          <h1 className="font-[family-name:var(--font-display)] flex items-center gap-2 text-3xl font-semibold tracking-tight">
            <UsersRound size={28} className="text-rose-600" />
            Teams Management
          </h1>
          <p className="mt-1 text-sm text-muted-foreground">{data?.totalCount ?? '—'} team members</p>
        </div>
        <div className="flex flex-wrap items-center gap-2">
          <FiltersButton activeCount={activeFilterCount} onClick={() => setFiltersOpen(true)} />
          <Button variant="outline" onClick={() => listQuery.refetch()} disabled={listQuery.isFetching}>
            <RefreshCw size={16} /> Refresh
          </Button>
          <Button onClick={openCreate}>
            <Plus size={16} /> Add Team Member
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
              placeholder="Name, email, phone, district…"
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
                  {[
                    'User ID',
                    'Member',
                    'Mobile',
                    'MW ID',
                    'Total MW',
                    'Sales',
                    'Dashboard',
                    'Referrals',
                    'Tracker',
                    'Email',
                    'District',
                    'State',
                    'Status',
                    'Created',
                    'Actions',
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
                    <TableCell colSpan={15} className="py-10 text-center text-muted-foreground">
                      Loading…
                    </TableCell>
                  </TableRow>
                )}
                {listQuery.isError && (
                  <TableRow>
                    <TableCell colSpan={15} className="py-10 text-center text-destructive">
                      {(listQuery.error as Error).message}
                    </TableCell>
                  </TableRow>
                )}
                {!listQuery.isLoading && !listQuery.isError && members.length === 0 && (
                  <TableRow>
                    <TableCell colSpan={15} className="py-10 text-center text-muted-foreground">
                      No team members found
                    </TableCell>
                  </TableRow>
                )}
                {members.map((m) => (
                  <TableRow key={m.id}>
                    <TableCell className="font-medium">{m.id}</TableCell>
                    <TableCell className="font-medium">{m.name}</TableCell>
                    <TableCell>{m.phone || '—'}</TableCell>
                    <TableCell>
                      {m.ownMwIds.length === 0 ? (
                        <span className="text-muted-foreground">—</span>
                      ) : (
                        <div className="flex flex-wrap gap-1">
                          {m.ownMwIds.map((id) => (
                            <a
                              key={id}
                              href={mwUrl(id)}
                              target="_blank"
                              rel="noreferrer"
                              className="text-rose-600 hover:underline"
                            >
                              {id}
                            </a>
                          ))}
                        </div>
                      )}
                    </TableCell>
                    <TableCell>{m.totalMwCreated}</TableCell>
                    <TableCell>{m.totalSales}</TableCell>
                    <TableCell>
                      <Button size="sm" variant="outline" onClick={() => setDashboardEmail(m.email)}>
                        <Eye size={14} /> View
                      </Button>
                    </TableCell>
                    <TableCell>
                      {m.referralCount > 0 ? (
                        <Button size="sm" variant="outline" onClick={() => setReferralsId(m.id)}>
                          <Eye size={14} /> View
                        </Button>
                      ) : (
                        <span className="text-muted-foreground">—</span>
                      )}
                    </TableCell>
                    <TableCell>
                      {m.trackerCount > 0 ? (
                        <div className="flex gap-1">
                          <Button size="sm" variant="outline" onClick={() => setTrackerId(m.id)}>
                            View
                          </Button>
                          <Button
                            size="sm"
                            variant="outline"
                            title="Export CSV"
                            onClick={() =>
                              void exportTeamTrackerCsv(m.id).catch((e) =>
                                toast.push(e instanceof ApiError ? e.message : 'Export failed', 'error'),
                              )
                            }
                          >
                            <Download size={14} />
                          </Button>
                        </div>
                      ) : (
                        <span className="text-muted-foreground">—</span>
                      )}
                    </TableCell>
                    <TableCell className="max-w-[200px]">
                      <div className="truncate">{m.email}</div>
                    </TableCell>
                    <TableCell>{m.district || '—'}</TableCell>
                    <TableCell>{m.state || '—'}</TableCell>
                    <TableCell>
                      <Badge variant={badgeVariantFromTone(m.statusTone)}>{m.status}</Badge>
                    </TableCell>
                    <TableCell>{m.createdAtDisplay}</TableCell>
                    <TableCell>
                      <div className="flex gap-1">
                        <Button size="icon-sm" variant="ghost" title="Edit" onClick={() => openEdit(m)}>
                          <Pencil size={14} />
                        </Button>
                        <Button
                          size="sm"
                          variant="outline"
                          disabled={togglingId === m.id}
                          onClick={() => void runToggle(m)}
                        >
                          {m.status === 'ACTIVE' ? 'Deactivate' : 'Activate'}
                        </Button>
                        <Button
                          size="icon-sm"
                          variant="ghost"
                          title="Reset password"
                          onClick={() => {
                            setResetTarget(m)
                            setNewPassword('')
                          }}
                        >
                          <KeyRound size={14} />
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

      <Drawer open={formOpen} onOpenChange={setFormOpen} direction="right">
        <DrawerContent className="data-[vaul-drawer-direction=right]:w-full data-[vaul-drawer-direction=right]:sm:max-w-md">
          <DrawerHeader>
            <DrawerTitle>{editing ? 'Edit Team Member' : 'Create Team Member'}</DrawerTitle>
            <DrawerDescription>Team accounts use role TEAM in user details</DrawerDescription>
          </DrawerHeader>
          <div className="space-y-3 px-4 pb-2">
            <Field label="Member Name *">
              <Input value={form.name} onChange={(e) => setForm((f) => ({ ...f, name: e.target.value }))} />
            </Field>
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
              <Button variant="outline">Cancel</Button>
            </DrawerClose>
          </DrawerFooter>
        </DrawerContent>
      </Drawer>

      <Dialog open={!!resetTarget} onOpenChange={(o) => !o && setResetTarget(null)}>
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
            <Button variant="outline" onClick={() => setResetTarget(null)}>
              Cancel
            </Button>
            <Button onClick={() => void runReset()} disabled={resetBusy}>
              {resetBusy ? 'Saving…' : 'Reset'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <Dialog open={!!dashboardEmail} onOpenChange={(o) => !o && setDashboardEmail(null)}>
        <DialogContent className="flex max-h-[85vh] max-w-4xl flex-col overflow-hidden">
          <DialogHeader>
            <DialogTitle>Dashboard — {dashboardEmail}</DialogTitle>
          </DialogHeader>
          {dashboardQuery.isLoading && <p className="text-muted-foreground">Loading…</p>}
          {dashboardQuery.isError && (
            <p className="text-destructive">{(dashboardQuery.error as Error).message}</p>
          )}
          {dashboardQuery.data && (
            <div className="min-h-0 flex-1 overflow-auto">
              {dashboardQuery.data.websites.length === 0 ? (
                <p className="text-muted-foreground">No websites found.</p>
              ) : (
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>ID</TableHead>
                      <TableHead>Company</TableHead>
                      <TableHead>Card</TableHead>
                      <TableHead>Status</TableHead>
                      <TableHead>Payment</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {dashboardQuery.data.websites.map((w) => (
                      <TableRow key={w.id}>
                        <TableCell>{w.id}</TableCell>
                        <TableCell>{w.companyName || '—'}</TableCell>
                        <TableCell>{w.cardId || '—'}</TableCell>
                        <TableCell>{w.cardStatus || '—'}</TableCell>
                        <TableCell>{w.paymentStatus || '—'}</TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              )}
            </div>
          )}
        </DialogContent>
      </Dialog>

      <Dialog open={!!referralsId} onOpenChange={(o) => !o && setReferralsId(null)}>
        <DialogContent className="flex max-h-[85vh] max-w-5xl flex-col overflow-hidden">
          <DialogHeader>
            <DialogTitle>
              Referral Details
              {referralsQuery.data ? ` — ${referralsQuery.data.memberName}` : ''}
            </DialogTitle>
            <DialogDescription>
              {referralsQuery.data
                ? `Sales: ${referralsQuery.data.totalSales} · MW Created: ${referralsQuery.data.totalMwCreated}`
                : null}
            </DialogDescription>
          </DialogHeader>
          {referralsQuery.isLoading && <p className="text-muted-foreground">Loading…</p>}
          {referralsQuery.isError && (
            <p className="text-destructive">{(referralsQuery.error as Error).message}</p>
          )}
          {referralsQuery.data && (
            <div className="min-h-0 flex-1 overflow-auto">
              {referralsQuery.data.rows.length === 0 ? (
                <p className="text-muted-foreground">No referrals found.</p>
              ) : (
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>User ID</TableHead>
                      <TableHead>Name</TableHead>
                      <TableHead>Email</TableHead>
                      <TableHead>Phone</TableHead>
                      <TableHead>Type</TableHead>
                      <TableHead>MW Payment</TableHead>
                      <TableHead>Paid On</TableHead>
                      <TableHead>Referral Date</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {referralsQuery.data.rows.map((r, i) => (
                      <TableRow key={`${r.referredEmail}-${i}`}>
                        <TableCell>{r.userId ?? '—'}</TableCell>
                        <TableCell>{r.referredName}</TableCell>
                        <TableCell>{r.referredEmail}</TableCell>
                        <TableCell>{r.phone}</TableCell>
                        <TableCell>{r.type}</TableCell>
                        <TableCell>
                          <Badge variant={badgeVariantFromTone(r.mwPaymentStatusTone)}>
                            {r.mwPaymentStatus}
                          </Badge>
                        </TableCell>
                        <TableCell>{r.paidOnDisplay}</TableCell>
                        <TableCell>{r.referralDateDisplay}</TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              )}
            </div>
          )}
        </DialogContent>
      </Dialog>

      <Dialog open={!!trackerId} onOpenChange={(o) => !o && setTrackerId(null)}>
        <DialogContent className="flex max-h-[85vh] max-w-5xl flex-col overflow-hidden">
          <DialogHeader>
            <DialogTitle>
              Customer Tracker
              {trackerQuery.data ? ` — ${trackerQuery.data.memberName}` : ''}
            </DialogTitle>
          </DialogHeader>
          {trackerQuery.isLoading && <p className="text-muted-foreground">Loading…</p>}
          {trackerQuery.isError && (
            <p className="text-destructive">{(trackerQuery.error as Error).message}</p>
          )}
          {trackerQuery.data && (
            <div className="min-h-0 flex-1 overflow-auto">
              {trackerQuery.data.rows.length === 0 ? (
                <p className="text-muted-foreground">No customer visits recorded.</p>
              ) : (
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>Shop/Person</TableHead>
                      <TableHead>Contact</TableHead>
                      <TableHead>Approached For</TableHead>
                      <TableHead>Address</TableHead>
                      <TableHead>Date Visited</TableHead>
                      <TableHead>Status</TableHead>
                      <TableHead>Last Updated</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {trackerQuery.data.rows.map((r) => (
                      <TableRow key={r.id}>
                        <TableCell>{r.shopName}</TableCell>
                        <TableCell>{r.contactNumber}</TableCell>
                        <TableCell>{r.approachedFor}</TableCell>
                        <TableCell className="max-w-[200px]">
                          <div className="line-clamp-2">{r.address}</div>
                        </TableCell>
                        <TableCell>{r.dateVisitedDisplay}</TableCell>
                        <TableCell>
                          <Badge variant={badgeVariantFromTone(r.finalStatusTone)}>{r.finalStatus}</Badge>
                        </TableCell>
                        <TableCell>{r.lastUpdatedDisplay}</TableCell>
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

function Field({ label, children }: { label: string; children: ReactNode }) {
  return (
    <div className="space-y-1.5">
      <label className="text-xs font-medium text-muted-foreground">{label}</label>
      {children}
    </div>
  )
}
