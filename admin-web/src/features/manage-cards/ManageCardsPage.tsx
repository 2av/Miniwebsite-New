import { useQuery } from '@tanstack/react-query'
import { Eye, Pencil, RefreshCw, Search, Share2, X } from 'lucide-react'
import { useEffect, useMemo, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { fetchManageCards } from '@/features/manage-cards/api'
import type { ManageCardRow } from '@/shared/types/api'
import { openInvoiceByCardId } from '@/shared/lib/invoiceDownload'
import { badgeVariantFromTone } from '@/shared/lib/badgeTone'
import { FiltersButton, FiltersDrawer } from '@/shared/ui/FiltersDrawer'
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

const COLUMNS = [
  'User ID',
  'DB ID',
  'User Email',
  'User Name',
  'User Number',
  'Company Name',
  'Referral Source',
  'Date Created',
  'Expiry Date',
  'DB Profile Status',
  'Profile Payment Status',
  'Order Value',
  'Product Invoice Details',
  'Actions',
]

function shareCard(url: string) {
  const text = encodeURIComponent(url)
  const isMobile = /Android|iPhone|iPad|iPod|Windows Phone/i.test(navigator.userAgent)
  if (isMobile) window.location.href = `whatsapp://send?text=${text}`
  else window.open(`https://wa.me/?text=${text}`, '_blank')
}

export function ManageCardsPage() {
  const [searchParams] = useSearchParams()
  const initialSearch = searchParams.get('search')?.trim() || ''
  const [filters, setFilters] = useState<Filters>(() => ({
    ...emptyFilters,
    search: initialSearch || undefined,
  }))
  const [searchInput, setSearchInput] = useState(initialSearch)
  const [paymentDraft, setPaymentDraft] = useState<PaymentFilter>('all')
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

  return (
    <div className="flex h-full min-h-0 min-w-0 max-w-full flex-col gap-3">
      <div className="flex shrink-0 flex-wrap items-end justify-between gap-3">
        <div>
          <h1 className="font-[family-name:var(--font-display)] text-3xl font-semibold text-slate-900">
            DB Profile Details
          </h1>
          <p className="mt-1 text-sm text-slate-500">{data?.totalCount ?? '—'} profiles</p>
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
              placeholder="User ID / DB ID / company / email…"
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
                {COLUMNS.map((h) => (
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
                  <TableCell colSpan={COLUMNS.length} className="px-4 py-10 text-center text-muted-foreground">
                    Loading profiles…
                  </TableCell>
                </TableRow>
              )}
              {listQuery.isError && (
                <TableRow>
                  <TableCell colSpan={COLUMNS.length} className="px-4 py-10 text-center text-destructive">
                    {(listQuery.error as Error).message}
                  </TableCell>
                </TableRow>
              )}
              {!listQuery.isLoading && !listQuery.isError && cards.length === 0 && (
                <TableRow>
                  <TableCell colSpan={COLUMNS.length} className="px-4 py-10 text-center text-muted-foreground">
                    No DB profiles found
                  </TableCell>
                </TableRow>
              )}
              {cards.map((c) => (
                <CardRow key={c.id} card={c} />
              ))}
              </TableBody>
            </Table>
          </div>

          <div className="flex shrink-0 items-center justify-between border-t px-4 py-3 text-sm">
          <div className="text-muted-foreground">
            Page {data?.page ?? 1} of {pages} · {data?.totalCount ?? 0} profiles
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

    </div>
  )
}

function paymentTone(label: string): 'neutral' | 'ok' | 'danger' | 'warn' {
  if (label.startsWith('Paid')) return 'ok'
  if (label === 'Rejected') return 'danger'
  if (label === 'Pending') return 'warn'
  return 'neutral'
}

function CardRow({ card: c }: { card: ManageCardRow }) {
  const raw = c.paymentLabel || 'Pending'
  const payment = raw === 'Unpaid' ? 'Pending' : raw
  return (
    <TableRow className="hover:bg-rose-50/30">
      <TableCell className="px-3 py-3 whitespace-nowrap">{c.userIdDisplay}</TableCell>
      <TableCell className="px-3 py-3 font-medium whitespace-nowrap">{c.dbIdDisplay || c.cardId}</TableCell>
      <TableCell className="max-w-[180px] truncate px-3 py-3">{c.userEmail}</TableCell>
      <TableCell className="px-3 py-3 whitespace-nowrap">{c.userName}</TableCell>
      <TableCell className="px-3 py-3 whitespace-nowrap">{c.userPhone}</TableCell>
      <TableCell className="max-w-[160px] truncate px-3 py-3">{c.companyName}</TableCell>
      <TableCell className="px-3 py-3 whitespace-nowrap">{c.referralSourceDisplay || 'Direct'}</TableCell>
      <TableCell className="px-3 py-3 whitespace-nowrap">{c.dateCreatedDisplay}</TableCell>
      <TableCell className="px-3 py-3 whitespace-nowrap">{c.expiryDateDisplay}</TableCell>
      <TableCell className="px-3 py-3 whitespace-nowrap">
        <Badge variant={badgeVariantFromTone(c.statusTone)}>{c.statusText}</Badge>
      </TableCell>
      <TableCell className="px-3 py-3 whitespace-nowrap">
        <Badge variant={badgeVariantFromTone(paymentTone(payment))}>{payment}</Badge>
      </TableCell>
      <TableCell className="px-3 py-3 whitespace-nowrap">{c.orderValueDisplay || '₹ 0'}</TableCell>
      <TableCell className="px-3 py-3">
        <Button variant="outline" size="sm" disabled={!c.hasInvoice} onClick={() => openInvoiceByCardId(c.id)}>
          View
        </Button>
      </TableCell>
      <TableCell className="px-3 py-3">
        <div className="flex items-center gap-1">
          <Button variant="ghost" size="icon-sm" asChild>
            <a href={c.publicUrl} target="_blank" rel="noreferrer" title="View" aria-label="View">
              <Eye size={16} />
            </a>
          </Button>
          <Button variant="ghost" size="icon-sm" asChild>
            <a href={c.editUrl} target="_blank" rel="noreferrer" title="Edit" aria-label="Edit">
              <Pencil size={16} />
            </a>
          </Button>
          <Button
            type="button"
            variant="ghost"
            size="icon-sm"
            title="Share"
            aria-label="Share"
            onClick={() => shareCard(c.publicUrl)}
          >
            <Share2 size={16} />
          </Button>
        </div>
      </TableCell>
    </TableRow>
  )
}