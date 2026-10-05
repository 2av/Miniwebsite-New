import { useQuery, useQueryClient } from '@tanstack/react-query'
import { Eye, Mail, Pencil, Plus, RefreshCw, Search, Trash2, X } from 'lucide-react'
import { useEffect, useMemo, useState } from 'react'
import {
  createEmailTemplate,
  deleteEmailTemplate,
  fetchEmailTemplates,
  fetchEmailTemplatesMeta,
  previewEmailTemplate,
  toggleEmailTemplateStatus,
  updateEmailTemplate,
} from '@/features/email-templates/api'
import type { EmailTemplateRow, UpsertEmailTemplatePayload } from '@/shared/types/api'
import { ApiError } from '@/shared/api/client'
import { badgeVariantFromTone } from '@/shared/lib/badgeTone'
import { HtmlRichEditor } from '@/shared/ui/HtmlRichEditor'
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
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'

const emptyForm: UpsertEmailTemplatePayload = {
  templateKey: '',
  name: '',
  category: 'general',
  subject: '',
  bodyHtml: '',
  availableVariables: '',
  description: '',
  status: 'active',
}

function sampleVarsFromList(vars?: string | null): Record<string, string> {
  const out: Record<string, string> = {}
  for (const raw of (vars || '').split(/[,;\s]+/).filter(Boolean)) {
    const key = raw.replace(/[{}]/g, '')
    out[key] = `sample_${key}`
  }
  if (!out.name) out.name = 'John Doe'
  if (!out.email) out.email = 'john@example.com'
  if (!out.host) out.host = 'miniwebsite.in'
  if (!out.otp) out.otp = '123456'
  if (!out.password) out.password = 'TempPass123'
  return out
}

export function ManageEmailTemplatesPage() {
  const toast = useToast()
  const qc = useQueryClient()
  const [page, setPage] = useState(1)
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [category, setCategory] = useState('all')
  const [status, setStatus] = useState('all')
  const [filtersOpen, setFiltersOpen] = useState(false)

  const [formOpen, setFormOpen] = useState(false)
  const [editing, setEditing] = useState<EmailTemplateRow | null>(null)
  const [form, setForm] = useState<UpsertEmailTemplatePayload>(emptyForm)
  const [formBusy, setFormBusy] = useState(false)
  const [deleteTarget, setDeleteTarget] = useState<EmailTemplateRow | null>(null)
  const [deleteBusy, setDeleteBusy] = useState(false)
  const [previewHtml, setPreviewHtml] = useState<{ subject: string; bodyHtml: string } | null>(null)
  const [previewBusy, setPreviewBusy] = useState(false)

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
      pageSize: 50,
      search: search || undefined,
      category: category === 'all' ? undefined : category,
      status: status === 'all' ? undefined : status,
    }),
    [page, search, category, status],
  )
  const queryKey = useMemo(() => ['email-templates', filters] as const, [filters])

  const listQuery = useQuery({
    queryKey,
    queryFn: () => fetchEmailTemplates(filters),
  })

  const metaQuery = useQuery({
    queryKey: ['email-templates-meta'],
    queryFn: fetchEmailTemplatesMeta,
    staleTime: Infinity,
  })

  const data = listQuery.data
  const templates = data?.templates ?? []
  const totalCount = data?.totalCount ?? 0
  const pageSize = data?.pageSize || 50
  const pages = Math.max(1, Math.ceil(totalCount / pageSize) || 1)
  const hasSearch = Boolean(searchInput.trim())
  const activeFilterCount =
    (category !== 'all' ? 1 : 0) + (status !== 'all' ? 1 : 0) + (hasSearch ? 1 : 0)

  function openCreate() {
    setEditing(null)
    setForm(emptyForm)
    setFormOpen(true)
  }

  function openEdit(row: EmailTemplateRow) {
    setEditing(row)
    setForm({
      templateKey: row.templateKey,
      name: row.name,
      category: row.category,
      subject: row.subject,
      bodyHtml: row.bodyHtml,
      availableVariables: row.availableVariables || '',
      description: row.description || '',
      status: row.status,
    })
    setFormOpen(true)
  }

  async function submitForm() {
    setFormBusy(true)
    try {
      const payload: UpsertEmailTemplatePayload = {
        ...form,
        updatedBy: 'admin',
      }
      if (editing) {
        await updateEmailTemplate(editing.id, payload)
        toast.push('Email template updated', 'success')
      } else {
        await createEmailTemplate(payload)
        toast.push('Email template created', 'success')
      }
      setFormOpen(false)
      await qc.invalidateQueries({ queryKey: ['email-templates'] })
    } catch (e) {
      toast.push(e instanceof ApiError ? e.message : 'Save failed', 'error')
    } finally {
      setFormBusy(false)
    }
  }

  async function onToggle(row: EmailTemplateRow) {
    try {
      await toggleEmailTemplateStatus(row.id)
      toast.push(row.status === 'active' ? 'Template deactivated' : 'Template activated', 'success')
      await qc.invalidateQueries({ queryKey: ['email-templates'] })
    } catch (e) {
      toast.push(e instanceof ApiError ? e.message : 'Status update failed', 'error')
    }
  }

  async function onDelete() {
    if (!deleteTarget) return
    setDeleteBusy(true)
    try {
      await deleteEmailTemplate(deleteTarget.id)
      toast.push('Email template deleted', 'success')
      setDeleteTarget(null)
      await qc.invalidateQueries({ queryKey: ['email-templates'] })
    } catch (e) {
      toast.push(e instanceof ApiError ? e.message : 'Delete failed', 'error')
    } finally {
      setDeleteBusy(false)
    }
  }

  async function onPreview(fromForm = false) {
    setPreviewBusy(true)
    try {
      const subject = fromForm ? form.subject : editing?.subject || form.subject
      const bodyHtml = fromForm ? form.bodyHtml : editing?.bodyHtml || form.bodyHtml
      const vars = sampleVarsFromList(fromForm ? form.availableVariables : editing?.availableVariables)
      const result = await previewEmailTemplate({ subject, bodyHtml, sampleVariables: vars })
      setPreviewHtml(result)
    } catch (e) {
      toast.push(e instanceof ApiError ? e.message : 'Preview failed', 'error')
    } finally {
      setPreviewBusy(false)
    }
  }

  return (
    <div className="flex h-full min-h-0 min-w-0 max-w-full flex-col gap-3">
      <div className="flex shrink-0 flex-wrap items-start justify-between gap-3">
        <div>
          <h1 className="flex items-center gap-2 text-2xl font-semibold text-slate-900">
            <Mail className="h-6 w-6 text-slate-500" />
            Email Templates
          </h1>
          <p className="mt-1 text-sm text-slate-500">
            {totalCount} templates · Manage subjects and HTML bodies. Placeholders like{' '}
            <code className="rounded bg-slate-100 px-1">{'{{name}}'}</code>.
          </p>
        </div>
        <div className="flex flex-wrap gap-2">
          <Button variant="outline" size="sm" onClick={() => listQuery.refetch()} disabled={listQuery.isFetching}>
            <RefreshCw className={`mr-1.5 h-4 w-4 ${listQuery.isFetching ? 'animate-spin' : ''}`} />
            Refresh
          </Button>
          <Button size="sm" onClick={openCreate}>
            <Plus className="mr-1.5 h-4 w-4" />
            Add template
          </Button>
        </div>
      </div>

      <Card className="shrink-0">
        <CardContent className="flex flex-wrap items-center gap-2 p-4">
          <div className="relative min-w-[220px] flex-1">
            <Search className="pointer-events-none absolute left-2.5 top-2.5 h-4 w-4 text-slate-400" />
            <Input
              className="pl-8"
              placeholder="Search name, key, subject…"
              value={searchInput}
              onChange={(e) => setSearchInput(e.target.value)}
            />
          </div>
          <FiltersButton activeCount={activeFilterCount} onClick={() => setFiltersOpen(true)} />
        </CardContent>
      </Card>

      <Card className="flex min-h-0 min-w-0 max-w-full flex-1 flex-col overflow-hidden py-0">
        <CardContent className="flex min-h-0 flex-1 flex-col p-0">
          <div className="sidebar-nav-scroll min-h-0 flex-1 overflow-auto overscroll-contain">
            <Table className="min-w-full">
              <TableHeader className="sticky top-0 z-10 bg-background">
                <TableRow>
                  <TableHead>Name / Key</TableHead>
                  <TableHead>Category</TableHead>
                  <TableHead>Subject</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead className="w-[200px] text-right">Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {listQuery.isLoading ? (
                  <TableRow>
                    <TableCell colSpan={5} className="py-10 text-center text-slate-500">
                      Loading…
                    </TableCell>
                  </TableRow>
                ) : templates.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={5} className="py-10 text-center text-slate-500">
                      No email templates found.
                    </TableCell>
                  </TableRow>
                ) : (
                  templates.map((row) => (
                    <TableRow key={row.id}>
                      <TableCell>
                        <div className="font-medium text-slate-900">{row.name}</div>
                        <div className="font-mono text-xs text-slate-500">{row.templateKey}</div>
                        {row.isSystem ? (
                          <Badge variant="secondary" className="mt-1">
                            System
                          </Badge>
                        ) : null}
                      </TableCell>
                      <TableCell>{row.categoryDisplay}</TableCell>
                      <TableCell>
                        <div className="max-w-[280px] truncate text-sm">{row.subject}</div>
                        <div className="mt-0.5 max-w-[280px] truncate text-xs text-slate-500">{row.bodyPreview}</div>
                      </TableCell>
                      <TableCell>
                        <Badge variant={badgeVariantFromTone(row.statusTone)}>{row.status}</Badge>
                      </TableCell>
                      <TableCell className="text-right">
                        <div className="flex justify-end gap-1">
                          <Button
                            size="icon"
                            variant="ghost"
                            title="Preview"
                            onClick={() => {
                              void previewEmailTemplate({
                                subject: row.subject,
                                bodyHtml: row.bodyHtml,
                                sampleVariables: sampleVarsFromList(row.availableVariables),
                              })
                                .then(setPreviewHtml)
                                .catch((e) => toast.push(e instanceof ApiError ? e.message : 'Preview failed', 'error'))
                            }}
                          >
                            <Eye className="h-4 w-4" />
                          </Button>
                          <Button size="icon" variant="ghost" title="Edit" onClick={() => openEdit(row)}>
                            <Pencil className="h-4 w-4" />
                          </Button>
                          <Button size="sm" variant="outline" onClick={() => void onToggle(row)}>
                            {row.status === 'active' ? 'Disable' : 'Enable'}
                          </Button>
                          {!row.isSystem ? (
                            <Button size="icon" variant="ghost" title="Delete" onClick={() => setDeleteTarget(row)}>
                              <Trash2 className="h-4 w-4 text-red-600" />
                            </Button>
                          ) : null}
                        </div>
                      </TableCell>
                    </TableRow>
                  ))
                )}
              </TableBody>
            </Table>
          </div>
          <div className="flex shrink-0 items-center justify-between border-t px-4 py-3 text-sm">
            <span className="text-slate-500">
              Showing {templates.length} of {totalCount} · Page {data?.page ?? page} of {pages} · {pageSize} per page
            </span>
            <div className="flex gap-2">
              <Button
                size="sm"
                variant="outline"
                disabled={page <= 1 || listQuery.isFetching}
                onClick={() => setPage((p) => Math.max(1, p - 1))}
              >
                Previous
              </Button>
              <Button
                size="sm"
                variant="outline"
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
        onClear={() => {
          setCategory('all')
          setStatus('all')
          setPage(1)
        }}
      >
        <div className="space-y-4">
          <div>
            <label className="mb-1.5 block text-sm font-medium">Category</label>
            <Select
              value={category}
              onValueChange={(v) => {
                setCategory(v)
                setPage(1)
              }}
            >
              <SelectTrigger>
                <SelectValue placeholder="All categories" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">All categories</SelectItem>
                {(metaQuery.data?.categories ?? []).map((c) => (
                  <SelectItem key={c.value} value={c.value}>
                    {c.label}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
          <div>
            <label className="mb-1.5 block text-sm font-medium">Status</label>
            <Select
              value={status}
              onValueChange={(v) => {
                setStatus(v)
                setPage(1)
              }}
            >
              <SelectTrigger>
                <SelectValue placeholder="All statuses" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">All</SelectItem>
                <SelectItem value="active">Active</SelectItem>
                <SelectItem value="inactive">Inactive</SelectItem>
              </SelectContent>
            </Select>
          </div>
        </div>
      </FiltersDrawer>

      <Drawer open={formOpen} onOpenChange={setFormOpen} direction="right">
        <DrawerContent className="data-[vaul-drawer-direction=right]:w-full data-[vaul-drawer-direction=right]:sm:max-w-4xl">
          <DrawerHeader className="shrink-0">
            <DrawerTitle>{editing ? 'Edit email template' : 'Add email template'}</DrawerTitle>
            <DrawerDescription>
              Placeholders use double curly braces, e.g. {'{{name}}'}, {'{{otp}}'}. Type them in the editor as plain text.
            </DrawerDescription>
          </DrawerHeader>
          <div className="min-h-0 flex-1 space-y-3 overflow-y-auto px-4 pb-2">
            <div>
              <label className="mb-1 block text-sm font-medium">Template key</label>
              <Input
                value={form.templateKey}
                disabled={Boolean(editing?.isSystem)}
                onChange={(e) => setForm((f) => ({ ...f, templateKey: e.target.value }))}
                placeholder="registration_otp"
              />
            </div>
            <div>
              <label className="mb-1 block text-sm font-medium">Name</label>
              <Input value={form.name} onChange={(e) => setForm((f) => ({ ...f, name: e.target.value }))} />
            </div>
            <div>
              <label className="mb-1 block text-sm font-medium">Category</label>
              <Select value={form.category} onValueChange={(v) => setForm((f) => ({ ...f, category: v }))}>
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {(metaQuery.data?.categories ?? [{ value: 'general', label: 'General' }]).map((c) => (
                    <SelectItem key={c.value} value={c.value}>
                      {c.label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div>
              <label className="mb-1 block text-sm font-medium">Subject</label>
              <Input value={form.subject} onChange={(e) => setForm((f) => ({ ...f, subject: e.target.value }))} />
            </div>
            <div>
              <label className="mb-1 block text-sm font-medium">Body</label>
              <HtmlRichEditor
                key={editing?.id ?? 'new-email-template'}
                value={form.bodyHtml}
                onChange={(bodyHtml) => setForm((f) => ({ ...f, bodyHtml }))}
              />
            </div>
            <div>
              <label className="mb-1 block text-sm font-medium">Available variables</label>
              <Input
                value={form.availableVariables}
                onChange={(e) => setForm((f) => ({ ...f, availableVariables: e.target.value }))}
                placeholder="name,email,otp,host"
              />
            </div>
            <div>
              <label className="mb-1 block text-sm font-medium">Description</label>
              <Textarea
                value={form.description}
                onChange={(e) => setForm((f) => ({ ...f, description: e.target.value }))}
              />
            </div>
            <div>
              <label className="mb-1 block text-sm font-medium">Status</label>
              <Select value={form.status || 'active'} onValueChange={(v) => setForm((f) => ({ ...f, status: v }))}>
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="active">Active</SelectItem>
                  <SelectItem value="inactive">Inactive</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>
          <DrawerFooter className="shrink-0 flex-row gap-2">
            <Button variant="outline" onClick={() => void onPreview(true)} disabled={previewBusy}>
              <Eye className="mr-1.5 h-4 w-4" />
              {previewBusy ? 'Preview…' : 'Preview'}
            </Button>
            <div className="flex-1" />
            <DrawerClose asChild>
              <Button variant="outline">
                <X className="mr-1.5 h-4 w-4" />
                Cancel
              </Button>
            </DrawerClose>
            <Button onClick={() => void submitForm()} disabled={formBusy}>
              {formBusy ? 'Saving…' : 'Save'}
            </Button>
          </DrawerFooter>
        </DrawerContent>
      </Drawer>

      <Dialog open={Boolean(previewHtml)} onOpenChange={(o) => !o && setPreviewHtml(null)}>
        <DialogContent className="flex max-h-[90vh] w-full flex-col gap-3 overflow-hidden sm:max-w-3xl">
          <DialogHeader className="pr-8">
            <DialogTitle>Email preview</DialogTitle>
            <DialogDescription className="break-words text-left font-medium text-foreground">
              Subject: {previewHtml?.subject || '—'}
            </DialogDescription>
          </DialogHeader>
          <div className="min-h-0 flex-1 overflow-auto rounded-md border bg-slate-100 p-3 sm:p-5">
            <div
              className="mx-auto w-full max-w-[640px] overflow-x-auto rounded-md bg-white p-4 text-[15px] leading-relaxed text-slate-800 shadow-sm [&_a]:text-blue-600 [&_a]:underline [&_img]:max-w-full [&_p]:my-2 [&_table]:w-full"
              dangerouslySetInnerHTML={{ __html: previewHtml?.bodyHtml || '' }}
            />
          </div>
        </DialogContent>
      </Dialog>

      <AlertDialog open={Boolean(deleteTarget)} onOpenChange={(o) => !o && setDeleteTarget(null)}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Delete template?</AlertDialogTitle>
            <AlertDialogDescription>
              This will permanently delete <strong>{deleteTarget?.name}</strong> ({deleteTarget?.templateKey}).
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel disabled={deleteBusy}>Cancel</AlertDialogCancel>
            <AlertDialogAction onClick={() => void onDelete()} disabled={deleteBusy}>
              {deleteBusy ? 'Deleting…' : 'Delete'}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  )
}
