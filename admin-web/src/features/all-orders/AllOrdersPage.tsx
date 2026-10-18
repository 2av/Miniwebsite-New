import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Pencil, RefreshCw, Search, X } from 'lucide-react'
import { useEffect, useMemo, useState } from 'react'
import { fetchAllOrders, updateOrderComment } from '@/features/all-orders/api'
import { fetchDashboardDetails } from '@/features/manage-users/api'
import type { AllOrderRow } from '@/shared/types/api'
import { openInvoiceById } from '@/shared/lib/invoiceDownload'
import { FiltersButton, FiltersDrawer } from '@/shared/ui/FiltersDrawer'
import { useToast } from '@/shared/ui/Toast'
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
import { Input } from '@/components/ui/input'
import { Textarea } from '@/components/ui/textarea'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'

const COLUMNS = [
  'Order ID',
  'Order Date',
  'User ID',
  'User Email',
  'DB ID',
  'Order Description',
  'Product Price',
  'Promo Discount',
  'Net Price',
  'CGST @ 9%',
  'SGST @ 9%',
  'IGST @ 18%',
  'Order Amount',
  'Invoice No.',
  'User Dashboard',
  'Referral Source',
  'Any Comment',
]

export function AllOrdersPage() {
  const toast = useToast()
  const qc = useQueryClient()
  const [page, setPage] = useState(1)
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [filtersOpen, setFiltersOpen] = useState(false)
  const [dashboardEmail, setDashboardEmail] = useState<string | null>(null)
  const [commentTarget, setCommentTarget] = useState<AllOrderRow | null>(null)
  const [commentDraft, setCommentDraft] = useState('')

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
  const queryKey = useMemo(() => ['all-orders', filters] as const, [filters])

  const listQuery = useQuery({
    queryKey,
    queryFn: () => fetchAllOrders(filters),
  })

  const dashboardQuery = useQuery({
    queryKey: ['dashboard-details', dashboardEmail],
    queryFn: () => fetchDashboardDetails(dashboardEmail!),
    enabled: !!dashboardEmail,
  })

  const commentMut = useMutation({
    mutationFn: ({ id, comment }: { id: number; comment: string }) => updateOrderComment(id, comment),
    onSuccess: async (res) => {
      toast.push(res.message || 'Comment saved', 'success')
      setCommentTarget(null)
      await qc.invalidateQueries({ queryKey: ['all-orders'] })
    },
    onError: (e: Error) => toast.push(e.message, 'error'),
  })

  const data = listQuery.data
  const orders = data?.orders ?? []
  const pages = data ? Math.max(1, Math.ceil(data.totalCount / data.pageSize)) : 1
  const hasSearch = Boolean(searchInput.trim())
  const activeFilterCount = hasSearch ? 1 : 0

  const clearFilters = () => {
    setSearchInput('')
    setSearch('')
    setPage(1)
  }

  const openComment = (order: AllOrderRow) => {
    setCommentDraft(order.comment || '')
    setCommentTarget(order)
  }

  return (
    <div className="flex h-full min-h-0 min-w-0 max-w-full flex-col gap-3">
      <div className="flex shrink-0 flex-wrap items-end justify-between gap-3">
        <div>
          <h1 className="font-[family-name:var(--font-display)] text-3xl font-semibold tracking-tight">
            All Orders
          </h1>
          <p className="mt-1 text-sm text-muted-foreground">{data?.totalCount ?? '—'} orders</p>
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
        onClear={activeFilterCount > 0 ? clearFilters : undefined}
      >
        <div className="space-y-1.5">
          <label className="text-xs font-medium text-muted-foreground">Search</label>
          <div className="relative">
            <Search size={16} className="pointer-events-none absolute top-2.5 left-3 text-muted-foreground" />
            <Input
              className="pr-9 pl-9"
              placeholder="Order ID / User ID / email / invoice no…"
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
          <div className="min-h-0 flex-1 overflow-auto">
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
                {!listQuery.isLoading && !listQuery.isError && orders.length === 0 && (
                  <TableRow>
                    <TableCell colSpan={COLUMNS.length} className="py-10 text-center text-muted-foreground">
                      No orders found
                    </TableCell>
                  </TableRow>
                )}
                {orders.map((o) => (
                  <OrderRow
                    key={o.invoiceId}
                    order={o}
                    onDashboard={() => o.userEmail && setDashboardEmail(o.userEmail)}
                    onComment={() => openComment(o)}
                  />
                ))}
              </TableBody>
            </Table>
          </div>

          <div className="flex shrink-0 items-center justify-between border-t px-4 py-3 text-sm">
            <div className="text-muted-foreground">
              Page {data?.page ?? 1} of {pages} · {data?.totalCount ?? 0} orders · 10 per page
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

      <Dialog open={!!dashboardEmail} onOpenChange={(open) => !open && setDashboardEmail(null)}>
        <DialogContent className="max-h-[85vh] overflow-y-auto sm:max-w-4xl">
          <DialogHeader>
            <DialogTitle>Dashboard — {dashboardEmail ?? ''}</DialogTitle>
            <DialogDescription>ShareBizz profiles for this user.</DialogDescription>
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

      <Dialog open={!!commentTarget} onOpenChange={(open) => !open && !commentMut.isPending && setCommentTarget(null)}>
        <DialogContent className="max-w-md">
          <DialogHeader>
            <DialogTitle>Comment — {commentTarget?.orderIdDisplay}</DialogTitle>
            <DialogDescription>Internal note for this order.</DialogDescription>
          </DialogHeader>
          <Textarea
            rows={4}
            maxLength={1000}
            value={commentDraft}
            onChange={(e) => setCommentDraft(e.target.value)}
            placeholder="Add a comment…"
          />
          <DialogFooter>
            <Button variant="outline" disabled={commentMut.isPending} onClick={() => setCommentTarget(null)}>
              Cancel
            </Button>
            <Button
              disabled={commentMut.isPending || !commentTarget}
              onClick={() => commentTarget && commentMut.mutate({ id: commentTarget.invoiceId, comment: commentDraft })}
            >
              {commentMut.isPending ? 'Saving…' : 'Save'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}

function OrderRow({
  order: o,
  onDashboard,
  onComment,
}: {
  order: AllOrderRow
  onDashboard: () => void
  onComment: () => void
}) {
  return (
    <TableRow>
      <TableCell className="px-3 font-medium whitespace-nowrap">{o.orderIdDisplay}</TableCell>
      <TableCell className="px-3 whitespace-nowrap">{o.orderDateDisplay}</TableCell>
      <TableCell className="px-3 whitespace-nowrap">{o.userIdDisplay}</TableCell>
      <TableCell className="max-w-[200px] truncate px-3">{o.userEmail}</TableCell>
      <TableCell className="px-3 whitespace-nowrap">{o.dbIdDisplay}</TableCell>
      <TableCell className="px-3 whitespace-nowrap">{o.orderDescription}</TableCell>
      <TableCell className="px-3 whitespace-nowrap">{o.listPriceDisplay}</TableCell>
      <TableCell className="px-3 whitespace-nowrap">{o.promoDiscountDisplay}</TableCell>
      <TableCell className="px-3 whitespace-nowrap">{o.netPriceDisplay}</TableCell>
      <TableCell className="px-3 whitespace-nowrap">{o.cgstDisplay || '₹ 0'}</TableCell>
      <TableCell className="px-3 whitespace-nowrap">{o.sgstDisplay || '₹ 0'}</TableCell>
      <TableCell className="px-3 whitespace-nowrap">{o.igstDisplay || '₹ 0'}</TableCell>
      <TableCell className="px-3 font-semibold whitespace-nowrap">{o.totalAmountDisplay}</TableCell>
      <TableCell className="px-3 whitespace-nowrap">
        {o.invoiceNumber ? (
          <Button variant="link" size="sm" className="h-auto px-0" onClick={() => openInvoiceById(o.invoiceId)}>
            {o.invoiceNumber}
          </Button>
        ) : null}
      </TableCell>
      <TableCell className="px-3">
        <Button variant="outline" size="sm" disabled={!o.userEmail} onClick={onDashboard}>
          View
        </Button>
      </TableCell>
      <TableCell className="px-3 whitespace-nowrap">{o.referralSource || 'Direct'}</TableCell>
      <TableCell className="px-3">
        <div className="flex max-w-[240px] items-center gap-1">
          <span className="truncate" title={o.comment}>{o.comment}</span>
          <Button variant="ghost" size="icon-sm" title="Edit comment" aria-label="Edit comment" onClick={onComment}>
            <Pencil size={14} />
          </Button>
        </div>
      </TableCell>
    </TableRow>
  )
}
