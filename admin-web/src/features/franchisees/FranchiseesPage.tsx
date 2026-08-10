import { useQuery, useQueryClient } from '@tanstack/react-query'
import { Download, ExternalLink, Eye, Pencil, Plus, RefreshCw, Search, Share2, X } from 'lucide-react'
import { useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import {
  activateFranchisee,
  createFranchisee,
  fetchFranchiseeDashboard,
  fetchFranchisees,
} from '@/features/franchisees/api'
import { resetPassword } from '@/features/manage-users/api'
import type { FranchiseeRow } from '@/shared/types/api'
import { ApiError } from '@/shared/api/client'
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
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog'
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
import { useToast } from '@/shared/ui/Toast'

function formatDate(value?: string | null) {
  if (!value) return '-'
  const d = new Date(value)
  if (Number.isNaN(d.getTime())) return '-'
  return d.toLocaleDateString('en-IN', { day: '2-digit', month: 'short', year: 'numeric' })
}

function shareUrl(url: string) {
  const text = encodeURIComponent(url)
  const isMobile = /Android|iPhone|iPad|iPod|Windows Phone/i.test(navigator.userAgent)
  if (isMobile) window.location.href = `whatsapp://send?text=${text}`
  else window.open(`https://wa.me/?text=${text}`, '_blank')
}

export function FranchiseesPage() {
  const toast = useToast()
  const qc = useQueryClient()
  const [page, setPage] = useState(1)
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [showCreate, setShowCreate] = useState(false)
  const [createName, setCreateName] = useState('')
  const [createEmail, setCreateEmail] = useState('')
  const [createPhone, setCreatePhone] = useState('')
  const [createPassword, setCreatePassword] = useState('')
  const [dashboardEmail, setDashboardEmail] = useState<string | null>(null)
  const [resetTarget, setResetTarget] = useState<string | null>(null)
  const [resetPw, setResetPw] = useState('')
  const [resetConfirm, setResetConfirm] = useState('')
  const [activateTarget, setActivateTarget] = useState<Pick<FranchiseeRow, 'id' | 'email'> | null>(null)
  const [showResetConfirm, setShowResetConfirm] = useState(false)
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
  const queryKey = useMemo(() => ['franchisees', filters] as const, [filters])

  const listQuery = useQuery({
    queryKey,
    queryFn: () => fetchFranchisees(filters),
  })

  const dashboardQuery = useQuery({
    queryKey: ['franchisee-dashboard', dashboardEmail],
    queryFn: () => fetchFranchiseeDashboard(dashboardEmail!),
    enabled: !!dashboardEmail,
  })

  const invalidate = () => qc.invalidateQueries({ queryKey: ['franchisees'] })

  const run = async (fn: () => Promise<unknown>, ok: string) => {
    try {
      const res = (await fn()) as { message?: string | null }
      toast.push(res?.message || ok, 'success')
      await invalidate()
      return true
    } catch (e) {
      toast.push(e instanceof ApiError ? e.message : 'Request failed', 'error')
      return false
    }
  }

  const data = listQuery.data
  const rows = data?.franchisees ?? []
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
            Franchisee Details
          </h1>
          <p className="mt-1 text-sm text-slate-500">{data?.totalCount ?? '—'} franchisees</p>
        </div>
        <div className="flex flex-wrap items-center gap-2">
          <FiltersButton activeCount={activeFilterCount} onClick={() => setFiltersOpen(true)} />
          <Button variant="secondary" onClick={() => listQuery.refetch()}>
            <RefreshCw size={16} /> Refresh
          </Button>
          <Button onClick={() => setShowCreate(true)}>
            <Plus size={16} /> Add Franchisee
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
              placeholder="Email / name / phone…"
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
                    'FR ID',
                    'Email',
                    'Name',
                    'Phone',
                    'Joined',
                    'Referral',
                    'Company',
                    'FR Status',
                    'View / Edit / Share',
                    'Payment',
                    'Franchise Fee',
                    'Invoice',
                    'MW',
                    'Documents',
                    'Wallet',
                    'Dashboard',
                    'Password',
                  ].map((h) => (
                    <TableHead
                      key={h}
                      className="px-3 text-xs font-semibold tracking-wide text-slate-200 uppercase"
                    >
                      {h}
                    </TableHead>
                  ))}
                </TableRow>
              </TableHeader>
              <TableBody>
                {listQuery.isLoading && (
                  <TableRow>
                    <TableCell colSpan={17} className="px-4 py-10 text-center text-muted-foreground">
                      Loading…
                    </TableCell>
                  </TableRow>
                )}
                {listQuery.isError && (
                  <TableRow>
                    <TableCell colSpan={17} className="px-4 py-10 text-center text-destructive">
                      {(listQuery.error as Error).message}
                    </TableCell>
                  </TableRow>
                )}
                {!listQuery.isLoading && !listQuery.isError && rows.length === 0 && (
                  <TableRow>
                    <TableCell colSpan={17} className="px-4 py-10 text-center text-muted-foreground">
                      No franchisees found
                    </TableCell>
                  </TableRow>
                )}
                {rows.map((f) => (
                  <Row
                    key={f.id}
                    franchisee={f}
                    onActivate={() => setActivateTarget({ id: f.id, email: f.email })}
                    onDashboard={() => setDashboardEmail(f.email)}
                    onReset={() => {
                      setResetTarget(f.email)
                      setResetPw('')
                      setResetConfirm('')
                    }}
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

      <Dialog open={showCreate} onOpenChange={setShowCreate}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Add Franchisee</DialogTitle>
          </DialogHeader>
          <div className="space-y-3">
            <Input
              placeholder="Full name *"
              value={createName}
              onChange={(e) => setCreateName(e.target.value)}
            />
            <Input
              type="email"
              placeholder="Login email *"
              value={createEmail}
              onChange={(e) => setCreateEmail(e.target.value)}
            />
            <Input
              placeholder="Mobile (10 digits) *"
              value={createPhone}
              onChange={(e) => setCreatePhone(e.target.value.replace(/\D/g, '').slice(0, 10))}
            />
            <Input
              type="password"
              placeholder="Password *"
              value={createPassword}
              onChange={(e) => setCreatePassword(e.target.value)}
            />
            <Button
              onClick={async () => {
                if (!createName.trim() || !createEmail.trim() || createPhone.length !== 10 || !createPassword) {
                  toast.push('Please fill all required fields correctly', 'error')
                  return
                }
                const ok = await run(
                  () =>
                    createFranchisee({
                      name: createName.trim(),
                      email: createEmail.trim(),
                      phone: createPhone,
                      password: createPassword,
                    }),
                  'Franchisee created',
                )
                if (ok) {
                  setShowCreate(false)
                  setCreateName('')
                  setCreateEmail('')
                  setCreatePhone('')
                  setCreatePassword('')
                }
              }}
            >
              Create ID & Password
            </Button>
          </div>
        </DialogContent>
      </Dialog>

      <Dialog open={!!dashboardEmail} onOpenChange={(open) => !open && setDashboardEmail(null)}>
        <DialogContent className="max-h-[85vh] overflow-y-auto sm:max-w-4xl">
          <DialogHeader>
            <DialogTitle>Dashboard — {dashboardEmail ?? ''}</DialogTitle>
          </DialogHeader>
          {dashboardQuery.isLoading && <p className="text-muted-foreground">Loading…</p>}
          {dashboardQuery.isError && (
            <p className="text-rose-600">{(dashboardQuery.error as Error).message}</p>
          )}
          {dashboardQuery.data && (
            <div className="overflow-x-auto">
              {dashboardQuery.data.websites.length === 0 ? (
                <p className="rounded-xl bg-sky-50 px-4 py-3 text-sky-900">No websites created yet.</p>
              ) : (
                <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>MW ID</TableHead>
                    <TableHead>Company</TableHead>
                    <TableHead>Created</TableHead>
                    <TableHead>Validity</TableHead>
                    <TableHead>Status</TableHead>
                    <TableHead>Payment</TableHead>
                    <TableHead>Open</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {dashboardQuery.data.websites.map((w) => (
                    <TableRow key={w.id}>
                      <TableCell>{w.id}</TableCell>
                      <TableCell>{w.companyName || 'Unnamed'}</TableCell>
                      <TableCell>{formatDate(w.uploadedDate)}</TableCell>
                      <TableCell>{formatDate(w.validityDate)}</TableCell>
                      <TableCell>
                        <Badge variant={badgeVariantFromTone(w.statusText === 'Active' ? 'ok' : 'warn')}>
                          {w.statusText}
                        </Badge>
                      </TableCell>
                      <TableCell>{w.paymentLabel}</TableCell>
                      <TableCell>
                        <a href={w.publicUrl} target="_blank" rel="noreferrer" className="text-rose-600 hover:underline">
                          View
                        </a>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
                </Table>
              )}
            </div>
          )}
        </DialogContent>
      </Dialog>

      <Dialog open={!!resetTarget} onOpenChange={(open) => !open && setResetTarget(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Reset password</DialogTitle>
          </DialogHeader>
          <p className="mb-3 text-sm text-slate-500">
            Updating password for <strong>{resetTarget}</strong>
          </p>
          <div className="space-y-3">
            <Input
              type="password"
              placeholder="New password (min 6)"
              value={resetPw}
              onChange={(e) => setResetPw(e.target.value)}
            />
            <Input
              type="password"
              placeholder="Confirm password"
              value={resetConfirm}
              onChange={(e) => setResetConfirm(e.target.value)}
            />
            <Button
              variant="destructive"
              onClick={() => {
                if (!resetTarget) return
                if (resetPw.length < 6) return toast.push('Password must be at least 6 characters', 'error')
                if (resetPw !== resetConfirm) return toast.push('Passwords do not match', 'error')
                setShowResetConfirm(true)
              }}
            >
              Update Password
            </Button>
          </div>
        </DialogContent>
      </Dialog>

      <AlertDialog open={!!activateTarget} onOpenChange={(open) => !open && setActivateTarget(null)}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Activate franchisee?</AlertDialogTitle>
            <AlertDialogDescription>
              Activate <strong>{activateTarget?.email}</strong>? This account will be able to use its
              franchisee access.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction
              onClick={() => {
                if (!activateTarget) return
                void run(() => activateFranchisee(activateTarget.id), 'Franchisee activated')
                setActivateTarget(null)
              }}
            >
              Activate
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>

      <AlertDialog open={showResetConfirm} onOpenChange={setShowResetConfirm}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Reset password?</AlertDialogTitle>
            <AlertDialogDescription>
              Reset the password for <strong>{resetTarget}</strong>? Their current password will stop
              working.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction
              variant="destructive"
              onClick={async () => {
                if (!resetTarget) return
                const ok = await run(
                  () => resetPassword(resetTarget, resetPw, 'FRANCHISEE'),
                  'Password updated',
                )
                if (ok) setResetTarget(null)
              }}
            >
              Reset Password
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  )
}

function Row({
  franchisee: f,
  onActivate,
  onDashboard,
  onReset,
}: {
  franchisee: FranchiseeRow
  onActivate: () => void
  onDashboard: () => void
  onReset: () => void
}) {
  return (
    <TableRow className="hover:bg-rose-50/30">
      <TableCell className="px-3 py-3 font-medium">{f.id}</TableCell>
      <TableCell className="max-w-[180px] truncate px-3 py-3">{f.email}</TableCell>
      <TableCell className="px-3 py-3">{f.name}</TableCell>
      <TableCell className="px-3 py-3">{f.phone || '-'}</TableCell>
      <TableCell className="px-3 py-3 text-muted-foreground">{formatDate(f.createdAt)}</TableCell>
      <TableCell className="px-3 py-3">{f.referralSourceDisplay}</TableCell>
      <TableCell className="max-w-[140px] truncate px-3 py-3">{f.companyName}</TableCell>
      <TableCell className="px-3 py-3">
        <div className="flex flex-col items-start gap-1">
          <Badge variant={badgeVariantFromTone(f.isActive ? 'ok' : 'neutral')}>{f.status}</Badge>
          {!f.isActive && (
            <Button variant="outline" size="xs" onClick={onActivate}>
              Activate
            </Button>
          )}
        </div>
      </TableCell>
      <TableCell className="px-3 py-3">
        {f.firstCardId ? (
          <div className="flex items-center gap-1">
            <a href={f.publicUrl} target="_blank" rel="noreferrer" className="rounded-lg p-1.5 text-slate-500 hover:bg-slate-100" title="View">
              <Eye size={14} />
            </a>
            <a href={f.editUrl} target="_blank" rel="noreferrer" className="rounded-lg p-1.5 text-slate-500 hover:bg-slate-100" title="Edit">
              <Pencil size={14} />
            </a>
            <button
              type="button"
              className="rounded-lg p-1.5 text-slate-500 hover:bg-slate-100"
              title="Share"
              onClick={() => shareUrl(f.publicUrl)}
            >
              <Share2 size={14} />
            </button>
            <a href={f.publicUrl} target="_blank" rel="noreferrer" className="rounded-lg p-1.5 text-slate-400 hover:bg-slate-100" title="Open">
              <ExternalLink size={14} />
            </a>
          </div>
        ) : (
          <span className="text-slate-300">-</span>
        )}
      </TableCell>
      <TableCell className="px-3 py-3">
        <div className="flex flex-col gap-0.5">
          <Badge variant={badgeVariantFromTone(f.paymentStatusTone)}>{f.paymentStatusLabel}</Badge>
          {f.paidOnDisplay && <span className="text-xs text-slate-500">on {f.paidOnDisplay}</span>}
        </div>
      </TableCell>
      <TableCell className="px-3 py-3">{f.franchiseFeeDisplay}</TableCell>
      <TableCell className="px-3 py-3">
        {f.franchiseInvoiceId ? (
          <button
            type="button"
            className="inline-flex text-rose-600 hover:underline"
            title="Download invoice"
            onClick={() => openInvoiceById(f.franchiseInvoiceId!)}
          >
            <Download size={16} />
          </button>
        ) : (
          <span className="text-slate-300">-</span>
        )}
      </TableCell>
      <TableCell className="px-3 py-3 text-center font-semibold">{f.websiteCount}</TableCell>
      <TableCell className="px-3 py-3">
        <Badge variant={badgeVariantFromTone(f.documentStatusTone)}>{f.documentStatus}</Badge>
      </TableCell>
      <TableCell className="px-3 py-3">
        <Link
          to={`/recharge-wallet?email=${encodeURIComponent(f.email)}&id=${f.id}`}
          className="font-medium text-rose-600 hover:underline"
          title="Recharge wallet"
        >
          {f.walletBalanceDisplay}
        </Link>
      </TableCell>
      <TableCell className="px-3 py-3">
        {f.websiteCount > 0 ? (
          <Button variant="ghost" onClick={onDashboard}>
            View
          </Button>
        ) : (
          <span className="text-slate-300">-</span>
        )}
      </TableCell>
      <TableCell className="px-3 py-3">
        <Button variant="ghost" onClick={onReset}>
          Reset
        </Button>
      </TableCell>
    </TableRow>
  )
}
