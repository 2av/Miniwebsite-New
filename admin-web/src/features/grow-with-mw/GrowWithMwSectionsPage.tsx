import { useQuery, useQueryClient } from '@tanstack/react-query'
import { ArrowDown, ArrowUp, Folders, Pencil, Plus, RefreshCw, Trash2 } from 'lucide-react'
import { useState, type ReactNode } from 'react'
import {
  createDocSection,
  deleteDocSection,
  fetchDocSections,
  reorderDocSections,
  updateDocSection,
} from '@/features/grow-with-mw/api'
import type { DocSection, UpsertDocSectionPayload } from '@/shared/types/api'
import { ApiError } from '@/shared/api/client'
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
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Checkbox } from '@/components/ui/checkbox'
import { Input } from '@/components/ui/input'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import { Textarea } from '@/components/ui/textarea'

const emptyForm: UpsertDocSectionPayload = {
  title: '',
  slug: '',
  description: '',
  collapsedDefault: false,
}

export function GrowWithMwSectionsPage() {
  const toast = useToast()
  const qc = useQueryClient()
  const [editing, setEditing] = useState<DocSection | null>(null)
  const [form, setForm] = useState<UpsertDocSectionPayload>(emptyForm)
  const [busy, setBusy] = useState(false)
  const [deleteTarget, setDeleteTarget] = useState<DocSection | null>(null)
  const [deleteBusy, setDeleteBusy] = useState(false)

  const listQuery = useQuery({
    queryKey: ['grow-with-mw-sections'],
    queryFn: fetchDocSections,
  })

  const sections = listQuery.data ?? []

  const openCreate = () => {
    setEditing(null)
    setForm({ ...emptyForm })
  }

  const openEdit = (row: DocSection) => {
    setEditing(row)
    setForm({
      title: row.title,
      slug: row.slug,
      description: row.description,
      collapsedDefault: row.collapsedDefault,
    })
  }

  const submit = async () => {
    if (!form.title.trim()) return toast.push('Title is required', 'error')
    setBusy(true)
    try {
      const payload = {
        title: form.title.trim(),
        slug: form.slug?.trim() || undefined,
        description: form.description?.trim() || '',
        collapsedDefault: form.collapsedDefault,
      }
      const res = editing
        ? await updateDocSection(editing.id, payload)
        : await createDocSection(payload)
      toast.push(res.message || (editing ? 'Section updated' : 'Section created'), 'success')
      openCreate()
      await qc.invalidateQueries({ queryKey: ['grow-with-mw-sections'] })
      await qc.invalidateQueries({ queryKey: ['grow-with-mw-meta'] })
    } catch (e) {
      toast.push(e instanceof ApiError ? e.message : 'Save failed', 'error')
    } finally {
      setBusy(false)
    }
  }

  const move = async (index: number, dir: -1 | 1) => {
    const next = index + dir
    if (next < 0 || next >= sections.length) return
    const order = sections.map((s) => s.id)
    ;[order[index], order[next]] = [order[next], order[index]]
    try {
      await reorderDocSections(order)
      await qc.invalidateQueries({ queryKey: ['grow-with-mw-sections'] })
    } catch (e) {
      toast.push(e instanceof ApiError ? e.message : 'Reorder failed', 'error')
    }
  }

  const runDelete = async () => {
    if (!deleteTarget) return
    setDeleteBusy(true)
    try {
      const res = await deleteDocSection(deleteTarget.id)
      toast.push(res.message || 'Section deleted', 'success')
      setDeleteTarget(null)
      if (editing?.id === deleteTarget.id) openCreate()
      await qc.invalidateQueries({ queryKey: ['grow-with-mw-sections'] })
      await qc.invalidateQueries({ queryKey: ['grow-with-mw-meta'] })
    } catch (e) {
      toast.push(e instanceof ApiError ? e.message : 'Delete failed', 'error')
    } finally {
      setDeleteBusy(false)
    }
  }

  return (
    <div className="flex h-full min-h-0 min-w-0 max-w-full flex-col gap-3 overflow-hidden">
      <div className="flex shrink-0 flex-wrap items-end justify-between gap-3">
        <div>
          <h1 className="font-[family-name:var(--font-display)] flex items-center gap-2 text-3xl font-semibold tracking-tight">
            <Folders size={28} className="text-rose-600" />
            Documentation sections
          </h1>
          <p className="mt-1 text-sm text-muted-foreground">
            Sidebar groups for Grow with MW / public docs
          </p>
        </div>
        <Button variant="outline" onClick={() => listQuery.refetch()} disabled={listQuery.isFetching}>
          <RefreshCw size={16} /> Refresh
        </Button>
      </div>

      <div className="grid min-h-0 flex-1 gap-3 lg:grid-cols-[360px_1fr]">
        <Card className="flex min-h-0 flex-col overflow-hidden">
          <CardHeader className="shrink-0 border-b py-4">
            <CardTitle className="text-base">{editing ? 'Edit section' : 'Add section'}</CardTitle>
          </CardHeader>
          <CardContent className="sidebar-nav-scroll space-y-3 overflow-y-auto pt-4">
            <Field label="Title *">
              <Input value={form.title} onChange={(e) => setForm((f) => ({ ...f, title: e.target.value }))} />
            </Field>
            <Field label="Slug">
              <Input
                value={form.slug || ''}
                onChange={(e) => setForm((f) => ({ ...f, slug: e.target.value }))}
                placeholder="auto from title if empty"
              />
            </Field>
            <Field label="Description">
              <Textarea
                rows={2}
                value={form.description || ''}
                onChange={(e) => setForm((f) => ({ ...f, description: e.target.value }))}
              />
            </Field>
            <label className="flex items-center gap-2 text-sm">
              <Checkbox
                checked={form.collapsedDefault}
                onCheckedChange={(v) => setForm((f) => ({ ...f, collapsedDefault: v === true }))}
              />
              Collapsed by default in sidebar
            </label>
            <div className="flex gap-2">
              <Button onClick={() => void submit()} disabled={busy}>
                <Plus size={16} />
                {busy ? 'Saving…' : editing ? 'Save changes' : 'Create section'}
              </Button>
              {editing && (
                <Button variant="outline" onClick={openCreate}>
                  Cancel
                </Button>
              )}
            </div>
          </CardContent>
        </Card>

        <Card className="flex min-h-0 flex-col overflow-hidden py-0">
          <CardContent className="flex min-h-0 flex-1 flex-col p-0">
            <div className="min-h-0 flex-1 overflow-auto">
              <Table>
                <TableHeader className="sticky top-0 z-10 bg-slate-900">
                  <TableRow className="border-slate-800 hover:bg-slate-900">
                    {['Order', 'Title', 'Slug', 'Pages', 'Actions'].map((h) => (
                      <TableHead key={h} className="text-xs font-semibold tracking-wide text-slate-200 uppercase">
                        {h}
                      </TableHead>
                    ))}
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {listQuery.isLoading && (
                    <TableRow>
                      <TableCell colSpan={5} className="py-10 text-center text-muted-foreground">
                        Loading…
                      </TableCell>
                    </TableRow>
                  )}
                  {!listQuery.isLoading && sections.length === 0 && (
                    <TableRow>
                      <TableCell colSpan={5} className="py-10 text-center text-muted-foreground">
                        No sections yet.
                      </TableCell>
                    </TableRow>
                  )}
                  {sections.map((s, i) => (
                    <TableRow key={s.id}>
                      <TableCell>
                        <div className="flex gap-1">
                          <Button size="icon-sm" variant="ghost" disabled={i === 0} onClick={() => void move(i, -1)}>
                            <ArrowUp size={14} />
                          </Button>
                          <Button
                            size="icon-sm"
                            variant="ghost"
                            disabled={i === sections.length - 1}
                            onClick={() => void move(i, 1)}
                          >
                            <ArrowDown size={14} />
                          </Button>
                        </div>
                      </TableCell>
                      <TableCell className="font-medium">
                        {s.title}
                        {s.collapsedDefault && (
                          <Badge variant="secondary" className="ml-2">
                            Collapsed
                          </Badge>
                        )}
                      </TableCell>
                      <TableCell>
                        <code className="text-xs">{s.slug}</code>
                      </TableCell>
                      <TableCell>{s.pageCount}</TableCell>
                      <TableCell>
                        <div className="flex gap-1">
                          <Button size="icon-sm" variant="ghost" onClick={() => openEdit(s)}>
                            <Pencil size={14} />
                          </Button>
                          <Button size="icon-sm" variant="ghost" onClick={() => setDeleteTarget(s)}>
                            <Trash2 size={14} />
                          </Button>
                        </div>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>
          </CardContent>
        </Card>
      </div>

      <AlertDialog open={Boolean(deleteTarget)} onOpenChange={(o) => !o && setDeleteTarget(null)}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Delete this empty section?</AlertDialogTitle>
            <AlertDialogDescription>{deleteTarget?.title}</AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel disabled={deleteBusy}>Cancel</AlertDialogCancel>
            <AlertDialogAction disabled={deleteBusy} onClick={() => void runDelete()}>
              {deleteBusy ? 'Deleting…' : 'Delete'}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
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
