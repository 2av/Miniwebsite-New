import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Download, ExternalLink, Eye, Pencil, RefreshCw, Search, Share2, X } from 'lucide-react'
import { useEffect, useMemo, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { fetchManageCards, setComplimentary } from '@/features/manage-cards/api'
import type { ManageCardRow } from '@/shared/types/api'
import { ApiError } from '@/shared/api/client'
import { openInvoiceByCardId } from '@/shared/lib/invoiceDownload'
import { badgeVariantFromTone } from '@/shared/lib/badgeTone'
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
import { Input } from '@/components/ui/input'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { Switch } from '@/components/ui/switch'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'

type PaymentFilter = 'all' | 'paid' | 'unpaid' | 'trial'
type Filters = {
  page: number
  pageSize: number
  search?: string
  paymentFilter?: PaymentFilter
}

const emptyFilters: Filters = { page: 1, pageSize: 10, search: '', paymentFilter: 'all' }

function formatDate(value?: string | null) {
  if (!value) return '-'
  const d = new Date(value)
  if (Number.isNaN(d.getTime())) return '-'
  return d.toLocaleDateString('en-IN', { day: '2-digit', month: 'short', year: 'numeric' })
}

function shareCard(url: string) {
  const text = encodeURIComponent(url)
  const isMobile = /Android|iPhone|iPad|iPod|Windows Phone/i.test(navigator.userAgent)
  if (isMobile) window.location.href = `whatsapp://send?text=${text}`
  else window.open(`https://wa.me/?text=${text}`, '_blank')
}

type ComplimentaryConfirm = {
  id: number
  enable: boolean
}

export function ManageCardsPage() {
  const toast = useToast()
  const qc = useQueryClient()
  const [searchParams] = useSearchParams()
  const initialSearch = searchParams.get('search')?.trim() || ''
  const [filters, setFilters] = useState<Filters>(() => ({
    ...emptyFilters,
    search: initialSearch || undefined,
  }))
  const [searchInput, setSearchInput] = useState(initialSearch)
  const [paymentDraft, setPaymentDraft] = useState<PaymentFilter>('all')
  const [complimentaryConfirm, setComplimentaryConfirm] = useState<ComplimentaryConfirm | null>(null)
  const [filtersOpen, setFiltersOpen] = useState(false)

  useEffect(() => {
    const t = window.setTimeout(() => {
      const next = searchInput.trim()
      setFilters((prev) => {
        if ((prev.search || '') === next) return prev
        return { ...prev, search: next || undefined, page: 1 }
      })
    }, 350)
    return () => window.clearTimeout(t)
  }, [searchInput])

  const queryKey = useMemo(() => ['manage-cards', filters] as const, [filters])

  const listQuery = useQuery({
    queryKey,
    queryFn: () =>
      fetchManageCards({
        page: filters.page,
        pageSize: filters.pageSize,
        search: filters.search || undefined,
        paymentFilter: filters.paymentFilter && filters.paymentFilter !== 'all' ? filters.paymentFilter : undefined,
      }),
  })

  const complimentaryMut = useMutation({
    mutationFn: ({ id, status }: { id: number; status: 'Yes' | 'No' }) => setComplimentary(id, status),
    onSuccess: async (res) => {
      toast.push(res.message || 'Complimentary updated', 'success')
      await qc.invalidateQueries({ queryKey: ['manage-cards'] })
    },
    onError: (e: Error) => toast.push(e instanceof ApiError ? e.message : e.message, 'error'),
    onSettled: () => setComplimentaryConfirm(null),
  })

  const data = listQuery.data
  const cards = data?.cards ?? []
  const pages = data ? Math.max(1, Math.ceil(data.totalCount / data.pageSize)) : 1
  const hasSearch = Boolean(searchInput.trim())
  const activeFilterCount = (hasSearch ? 1 : 0) + (paymentDraft !== 'all' ? 1 : 0)

  const clearFilters = () => {
    setSearchInput('')
    setPaymentDraft('all')
    setFilters({ ...emptyFilters })
  }

  const confirmComplimentary = () => {
    if (!complimentaryConfirm) return
    complimentaryMut.mutate({
      id: complimentaryConfirm.id,
      status: complimentaryConfirm.enable ? 'Yes' : 'No',
    })
  }

  return (
    <div className="flex h-full min-h-0 min-w-0 max-w-full flex-col gap-3">
      <div className="flex shrink-0 flex-wrap items-end justify-between gap-3">
        <div>
          <h1 className="font-[family-name:var(--font-display)] text-3xl font-semibold text-slate-900">
            Miniwebsite Details
          </h1>
          <p className="mt-1 text-sm text-slate-500">{data?.totalCount ?? '—'} websites</p>
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
          <label className="text-xs font-medium text-muted-foreground">Payment</label>
          <Select
            value={paymentDraft}
            onValueChange={(value) => {
              const v = value as PaymentFilter
              setPaymentDraft(v)
              setFilters((f) => ({ ...f, paymentFilter: v, page: 1 }))
            }}
          >
            <SelectTrigger className="w-full">
              <SelectValue placeholder="All payments" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All payments</SelectItem>
              <SelectItem value="paid">Payment Done</SelectItem>
              <SelectItem value="unpaid">Payment Not Done</SelectItem>
              <SelectItem value="trial">Trial Cards</SelectItem>
            </SelectContent>
          </Select>
        </div>
        <div className="space-y-1.5">
          <label className="text-xs font-medium text-muted-foreground">Search</label>
          <div className="relative">
            <Search size={16} className="pointer-events-none absolute top-2.5 left-3 text-muted-foreground" />
            <Input
              className="pr-9 pl-9"
              placeholder="ID / company / email…"
              value={searchInput}
              onChange={(e) => setSearchInput(e.target.value)}
            />
            {hasSearch && (
              <Button
                type="button"
                variant="ghost"
                size="icon-xs"
                className="absolute top-1.5 right-2 text-muted-foreground"
                onClick={() => setSearchInput('')}
                aria-label="Clear search"
              >
                <X size={14} />
              </Button>
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
                  'User ID',
                  'MW ID',
                  'User Email',
                  'User Name',
                  'Phone',
                  'Referral',
                  'Company',
                  'Created',
                  'Validity',
                  'MW Status',
                  'View / Edit / Share',
                  'Payment',
                  'Order ₹',
                  'Invoice',
                  'Complimentary',
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
                  <TableCell colSpan={15} className="px-4 py-10 text-center text-muted-foreground">
                    Loading websites…
                  </TableCell>
                </TableRow>
              )}
              {listQuery.isError && (
                <TableRow>
                  <TableCell colSpan={15} className="px-4 py-10 text-center text-destructive">
                    {(listQuery.error as Error).message}
                  </TableCell>
                </TableRow>
              )}
              {!listQuery.isLoading && !listQuery.isError && cards.length === 0 && (
                <TableRow>
                  <TableCell colSpan={15} className="px-4 py-10 text-center text-muted-foreground">
                    No websites found
                  </TableCell>
                </TableRow>
              )}
              {cards.map((c) => (
                <CardRow
                  key={c.id}
                  card={c}
                  complimentaryBusy={
                    complimentaryMut.isPending && complimentaryConfirm?.id === c.id
                  }
                  onComplimentary={(enable) => setComplimentaryConfirm({ id: c.id, enable })}
                />
              ))}
              </TableBody>
            </Table>
          </div>

          <div className="flex shrink-0 items-center justify-between border-t px-4 py-3 text-sm">
          <div className="text-muted-foreground">
            Page {data?.page ?? 1} of {pages} · {data?.totalCount ?? 0} websites
          </div>
          <div className="flex gap-2">
            <Button
              variant="outline"
              size="sm"
              disabled={(filters.page ?? 1) <= 1 || listQuery.isFetching}
              onClick={() => setFilters((f) => ({ ...f, page: Math.max(1, f.page - 1) }))}
            >
              Prev
            </Button>
            <Button
              variant="outline"
              size="sm"
              disabled={(filters.page ?? 1) >= pages || listQuery.isFetching}
              onClick={() => setFilters((f) => ({ ...f, page: f.page + 1 }))}
            >
              Next
            </Button>
          </div>
          </div>
        </CardContent>
      </Card>

      <AlertDialog
        open={!!complimentaryConfirm}
        onOpenChange={(open) => {
          if (!open && !complimentaryMut.isPending) setComplimentaryConfirm(null)
        }}
      >
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Confirm complimentary change</AlertDialogTitle>
            <AlertDialogDescription>
              {complimentaryConfirm?.enable ? 'Enable' : 'Disable'} complimentary access for MW #
              {complimentaryConfirm?.id}?
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel disabled={complimentaryMut.isPending}>Cancel</AlertDialogCancel>
            <AlertDialogAction
              disabled={complimentaryMut.isPending}
              onClick={(event) => {
                event.preventDefault()
                confirmComplimentary()
              }}
            >
              {complimentaryMut.isPending ? 'Please wait…' : 'Confirm'}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  )
}

function CardRow({
  card: c,
  onComplimentary,
  complimentaryBusy,
}: {
  card: ManageCardRow
  onComplimentary: (yes: boolean) => void
  complimentaryBusy: boolean
}) {
  return (
    <TableRow className="hover:bg-rose-50/30">
      <TableCell className="px-3 py-3">{c.userId ?? '-'}</TableCell>
      <TableCell className="px-3 py-3 font-medium">{c.id}</TableCell>
      <TableCell className="max-w-[180px] truncate px-3 py-3">{c.userEmail || '-'}</TableCell>
      <TableCell className="px-3 py-3">{c.userName || '-'}</TableCell>
      <TableCell className="px-3 py-3">{c.userPhone || '-'}</TableCell>
      <TableCell className="px-3 py-3">{c.referralSourceDisplay}</TableCell>
      <TableCell className="max-w-[160px] truncate px-3 py-3">{c.companyName || '-'}</TableCell>
      <TableCell className="px-3 py-3 text-muted-foreground">{formatDate(c.uploadedDate)}</TableCell>
      <TableCell className="px-3 py-3">
        <span className={c.validityTone === 'danger' ? 'text-rose-600 font-medium' : undefined}>
          {c.validityDisplay}
        </span>
      </TableCell>
      <TableCell className="px-3 py-3">
        <Badge variant={badgeVariantFromTone(c.statusTone)}>{c.statusText}</Badge>
      </TableCell>
      <TableCell className="px-3 py-3">
        <div className="flex items-center gap-1">
          <Button variant="ghost" size="icon-sm" asChild>
            <a href={c.publicUrl} target="_blank" rel="noreferrer" title="View" aria-label="View">
              <Eye size={16} />
            </a>
          </Button>
          <Button variant="ghost" size="icon-sm" asChild>
            <a href={c.editUrl} target="_blank" rel="noreferrer" title="Edit (PHP)" aria-label="Edit">
              <Pencil size={16} />
            </a>
          </Button>
          <Button
            type="button"
            variant="ghost"
            size="icon-sm"
            title="Share WhatsApp"
            aria-label="Share on WhatsApp"
            onClick={() => shareCard(c.publicUrl)}
          >
            <Share2 size={16} />
          </Button>
          <Button variant="ghost" size="icon-sm" className="text-slate-400" asChild>
            <a href={c.publicUrl} target="_blank" rel="noreferrer" title="Open" aria-label="Open">
              <ExternalLink size={14} />
            </a>
          </Button>
        </div>
      </TableCell>
      <TableCell className="px-3 py-3">
        <Badge variant={badgeVariantFromTone(c.paymentLabel.startsWith('Paid') ? 'ok' : 'neutral')}>
          {c.paymentLabel}
        </Badge>
      </TableCell>
      <TableCell className="px-3 py-3">{c.orderAmount ? `₹${c.orderAmount}` : '-'}</TableCell>
      <TableCell className="px-3 py-3">
        {c.hasInvoice ? (
          <Button
            type="button"
            variant="ghost"
            size="icon-sm"
            className="text-rose-600"
            title="Download invoice"
            aria-label="Download invoice"
            onClick={() => openInvoiceByCardId(c.id)}
          >
            <Download size={16} />
          </Button>
        ) : (
          <span className="text-slate-300">-</span>
        )}
      </TableCell>
      <TableCell className="px-3 py-3 text-center">
        <Switch
          checked={(c.complimentaryEnabled || 'No') === 'Yes'}
          disabled={!c.canToggleComplimentary || complimentaryBusy}
          onCheckedChange={(next) => {
            if (!c.canToggleComplimentary) return
            onComplimentary(next)
          }}
          aria-label={`Toggle complimentary for MW #${c.id}`}
        />
      </TableCell>
    </TableRow>
  )
}