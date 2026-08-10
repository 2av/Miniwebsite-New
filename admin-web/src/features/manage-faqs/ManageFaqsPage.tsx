import { useQuery, useQueryClient } from '@tanstack/react-query'
import { CircleHelp, Pencil, Plus, RefreshCw, Search, Trash2, X } from 'lucide-react'
import { useEffect, useMemo, useState } from 'react'
import {
  createFaq,
  deleteFaq,
  fetchManageFaqs,
  fetchManageFaqsMeta,
  updateFaq,
} from '@/features/manage-faqs/api'
import type { ManageFaqRow, UpsertFaqPayload } from '@/shared/types/api'
import { ApiError } from '@/shared/api/client'
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

const emptyForm: UpsertFaqPayload = {
  pageType: '',
  question: '',
  answer: '',
  sortOrder: 1,
  status: 'active',
}

export function ManageFaqsPage() {
  const toast = useToast()
  const qc = useQueryClient()
  const [page, setPage] = useState(1)
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [pageType, setPageType] = useState('all')
  const [status, setStatus] = useState('all')
  const [filtersOpen, setFiltersOpen] = useState(false)

  const [formOpen, setFormOpen] = useState(false)
  const [editing, setEditing] = useState<ManageFaqRow | null>(null)
  const [form, setForm] = useState<UpsertFaqPayload>(emptyForm)
  const [formBusy, setFormBusy] = useState(false)
  const [deleteTarget, setDeleteTarget] = useState<ManageFaqRow | null>(null)
  const [deleteBusy, setDeleteBusy] = useState(false)

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
      pageType: pageType === 'all' ? undefined : pageType,
      status: status === 'all' ? undefined : status,
    }),
    [page, search, pageType, status],
  )
  const queryKey = useMemo(() => ['manage-faqs', filters] as const, [filters])

  const listQuery = useQuery({
    queryKey,
    queryFn: () => fetchManageFaqs(filters),
  })

  const metaQuery = useQuery({
    queryKey: ['manage-faqs-meta'],
    queryFn: fetchManageFaqsMeta,
    staleTime: Infinity,
  })

  const data = listQuery.data
  const faqs = data?.faqs ?? []
  const pages = data ? Math.max(1, Math.ceil(data.totalCount / data.pageSize)) : 1
  const hasSearch = Boolean(searchInput.trim())
  const activeFilterCount =
    (hasSearch ? 1 : 0) + (pageType !== 'all' ? 1 : 0) + (status !== 'all' ? 1 : 0)
  const pageTypes = metaQuery.data?.pageTypes ?? []

  const clearFilters = () => {
    setSearchInput('')
    setSearch('')
    setPageType('all')
    setStatus('all')
    setPage(1)
  }

  const openCreate = () => {
    setEditing(null)
    setForm({ ...emptyForm })
    setFormOpen(true)
  }

  const openEdit = (row: ManageFaqRow) => {
    setEditing(row)
    setForm({
      pageType: row.pageType,
      question: row.question,
      answer: row.answer,
      sortOrder: row.sortOrder,
      status: row.status,
    })
    setFormOpen(true)
  }

  const submitForm = async () => {
    if (!form.pageType || !form.question.trim() || !form.answer.trim()) {
      return toast.push('Please fill all required fields', 'error')
    }
    setFormBusy(true)
    try {
      const payload: UpsertFaqPayload = {
        pageType: form.pageType,
        question: form.question.trim(),
        answer: form.answer.trim(),
        sortOrder: Number(form.sortOrder) || 1,
        status: editing ? form.status || 'active' : undefined,
      }
      const res = editing ? await updateFaq(editing.id, payload) : await createFaq(payload)
      toast.push(res.message || (editing ? 'FAQ updated' : 'FAQ added'), 'success')
      setFormOpen(false)
      await qc.invalidateQueries({ queryKey: ['manage-faqs'] })
    } catch (e) {
      toast.push(e instanceof ApiError ? e.message : 'Save failed', 'error')
    } finally {
      setFormBusy(false)
    }
  }

  const runDelete = async () => {
    if (!deleteTarget) return
    setDeleteBusy(true)
    try {
      const res = await deleteFaq(deleteTarget.id)
      toast.push(res.message || 'FAQ deleted', 'success')
      setDeleteTarget(null)
      await qc.invalidateQueries({ queryKey: ['manage-faqs'] })
    } catch (e) {
      toast.push(e instanceof ApiError ? e.message : 'Delete failed', 'error')
    } finally {
      setDeleteBusy(false)
    }
  }

  return (
    <div className="flex h-full min-h-0 min-w-0 max-w-full flex-col gap-3">
      <div className="flex shrink-0 flex-wrap items-end justify-between gap-3">
        <div>
          <h1 className="font-[family-name:var(--font-display)] flex items-center gap-2 text-3xl font-semibold tracking-tight">
            <CircleHelp size={28} className="text-rose-600" />
            FAQ Management
          </h1>
          <p className="mt-1 text-sm text-muted-foreground">{data?.totalCount ?? '—'} FAQs</p>
        </div>
        <div className="flex flex-wrap items-center gap-2">
          <FiltersButton activeCount={activeFilterCount} onClick={() => setFiltersOpen(true)} />
          <Button variant="outline" onClick={() => listQuery.refetch()}>
            <RefreshCw size={16} /> Refresh
          </Button>
          <Button onClick={openCreate}>
            <Plus size={16} /> Add FAQ
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
              placeholder="Question or answer…"
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
          <label className="text-xs font-medium text-muted-foreground">Page type</label>
          <Select
            value={pageType}
            onValueChange={(v) => {
              setPageType(v)
              setPage(1)
            }}
          >
            <SelectTrigger className="w-full">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All</SelectItem>
              {pageTypes.map((p) => (
                <SelectItem key={p.value} value={p.value}>
                  {p.label}
                </SelectItem>
              ))}
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
              <SelectItem value="active">Active</SelectItem>
              <SelectItem value="inactive">Inactive</SelectItem>
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
                  {['ID', 'Page Type', 'Question', 'Answer', 'Sort', 'Status', 'Action'].map((h) => (
                    <TableHead key={h} className="text-xs font-semibold tracking-wide text-slate-200 uppercase">
                      {h}
                    </TableHead>
                  ))}
                </TableRow>
              </TableHeader>
              <TableBody>
                {listQuery.isLoading && (
                  <TableRow>
                    <TableCell colSpan={7} className="py-10 text-center text-muted-foreground">
                      Loading…
                    </TableCell>
                  </TableRow>
                )}
                {listQuery.isError && (
                  <TableRow>
                    <TableCell colSpan={7} className="py-10 text-center text-destructive">
                      {(listQuery.error as Error).message}
                    </TableCell>
                  </TableRow>
                )}
                {!listQuery.isLoading && !listQuery.isError && faqs.length === 0 && (
                  <TableRow>
                    <TableCell colSpan={7} className="py-10 text-center text-muted-foreground">
                      No FAQs found
                    </TableCell>
                  </TableRow>
                )}
                {faqs.map((f) => (
                  <TableRow key={f.id}>
                    <TableCell className="font-medium">{f.id}</TableCell>
                    <TableCell>
                      <Badge variant="secondary">{f.pageTypeDisplay}</Badge>
                    </TableCell>
                    <TableCell className="max-w-[240px]">
                      <div className="line-clamp-2 font-medium">{f.question}</div>
                    </TableCell>
                    <TableCell className="max-w-[280px] text-muted-foreground">
                      <div className="line-clamp-2">{f.answerPreview}</div>
                    </TableCell>
                    <TableCell>{f.sortOrder}</TableCell>
                    <TableCell>
                      <Badge variant={badgeVariantFromTone(f.statusTone)}>{f.status}</Badge>
                    </TableCell>
                    <TableCell>
                      <div className="flex gap-1">
                        <Button size="icon-sm" variant="ghost" title="Edit" onClick={() => openEdit(f)}>
                          <Pencil size={14} />
                        </Button>
                        <Button
                          size="icon-sm"
                          variant="ghost"
                          title="Delete"
                          onClick={() => setDeleteTarget(f)}
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
        <DrawerContent className="data-[vaul-drawer-direction=right]:sm:max-w-lg">
          <DrawerHeader>
            <DrawerTitle>{editing ? 'Edit FAQ' : 'Add FAQ'}</DrawerTitle>
            <DrawerDescription>Questions shown on home, refer & earn, and franchise pages</DrawerDescription>
          </DrawerHeader>
          <div className="flex flex-1 flex-col gap-3 overflow-y-auto px-4 pb-2">
            <Field label="Page Type *">
              <Select value={form.pageType} onValueChange={(v) => setForm((f) => ({ ...f, pageType: v }))}>
                <SelectTrigger className="w-full">
                  <SelectValue placeholder="Select Page Type" />
                </SelectTrigger>
                <SelectContent>
                  {pageTypes.map((p) => (
                    <SelectItem key={p.value} value={p.value}>
                      {p.label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </Field>
            <Field label="Question *">
              <Input
                value={form.question}
                onChange={(e) => setForm((f) => ({ ...f, question: e.target.value }))}
                placeholder="Enter question"
              />
            </Field>
            <Field label="Answer *">
              <Textarea
                value={form.answer}
                onChange={(e) => setForm((f) => ({ ...f, answer: e.target.value }))}
                rows={8}
                placeholder="Enter answer (HTML allowed)"
              />
            </Field>
            <Field label="Sort Order">
              <Input
                type="number"
                min={1}
                value={form.sortOrder}
                onChange={(e) => setForm((f) => ({ ...f, sortOrder: Number(e.target.value) || 1 }))}
              />
            </Field>
            {editing && (
              <Field label="Status">
                <Select
                  value={form.status || 'active'}
                  onValueChange={(v) => setForm((f) => ({ ...f, status: v }))}
                >
                  <SelectTrigger className="w-full">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="active">Active</SelectItem>
                    <SelectItem value="inactive">Inactive</SelectItem>
                  </SelectContent>
                </Select>
              </Field>
            )}
          </div>
          <DrawerFooter>
            <DrawerClose asChild>
              <Button type="button" variant="outline" disabled={formBusy}>
                Cancel
              </Button>
            </DrawerClose>
            <Button type="button" disabled={formBusy} onClick={() => void submitForm()}>
              {formBusy ? 'Saving…' : editing ? 'Update FAQ' : 'Add FAQ'}
            </Button>
          </DrawerFooter>
        </DrawerContent>
      </Drawer>

      <AlertDialog open={!!deleteTarget} onOpenChange={(o) => !o && !deleteBusy && setDeleteTarget(null)}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Delete FAQ?</AlertDialogTitle>
            <AlertDialogDescription>
              Delete <strong>{deleteTarget?.question}</strong>? This cannot be undone.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel disabled={deleteBusy}>Cancel</AlertDialogCancel>
            <AlertDialogAction
              disabled={deleteBusy}
              variant="destructive"
              onClick={(e) => {
                e.preventDefault()
                void runDelete()
              }}
            >
              {deleteBusy ? 'Deleting…' : 'Delete'}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
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
