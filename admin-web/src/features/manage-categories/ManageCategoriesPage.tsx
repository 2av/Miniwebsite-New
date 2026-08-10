import { useQuery, useQueryClient } from '@tanstack/react-query'
import {
  Download,
  FolderTree,
  Pencil,
  Plus,
  RefreshCw,
  Search,
  Trash2,
  Upload,
  X,
} from 'lucide-react'
import { useEffect, useMemo, useRef, useState, type ReactNode } from 'react'
import {
  createCategory,
  deleteCategory,
  exportCategoriesCsv,
  fetchManageCategories,
  importCategoriesCsv,
  toggleCategoryActive,
  updateCategory,
} from '@/features/manage-categories/api'
import type { ManageCategoryRow, UpsertCategoryPayload } from '@/shared/types/api'
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
import { Checkbox } from '@/components/ui/checkbox'
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

const emptyForm: UpsertCategoryPayload = {
  businessProfileType: '',
  businessHeading: '',
  businessCategory: '',
  businessCategorySlug: '',
  productCategory: '',
  productCategorySlug: '',
  directoryPriority: 0,
  isActive: true,
  keywords: '',
  tags: '',
}

function slugify(text: string) {
  return text
    .trim()
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '') || 'category'
}

export function ManageCategoriesPage() {
  const toast = useToast()
  const qc = useQueryClient()
  const fileRef = useRef<HTMLInputElement>(null)

  const [page, setPage] = useState(1)
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [active, setActive] = useState('all')
  const [filtersOpen, setFiltersOpen] = useState(false)

  const [formOpen, setFormOpen] = useState(false)
  const [editing, setEditing] = useState<ManageCategoryRow | null>(null)
  const [form, setForm] = useState<UpsertCategoryPayload>(emptyForm)
  const [formBusy, setFormBusy] = useState(false)
  const [autoSlug, setAutoSlug] = useState(true)

  const [deleteTarget, setDeleteTarget] = useState<ManageCategoryRow | null>(null)
  const [deleteBusy, setDeleteBusy] = useState(false)
  const [togglingId, setTogglingId] = useState<number | null>(null)

  const [importOpen, setImportOpen] = useState(false)
  const [importFile, setImportFile] = useState<File | null>(null)
  const [replaceAll, setReplaceAll] = useState(false)
  const [skipDuplicates, setSkipDuplicates] = useState(true)
  const [importBusy, setImportBusy] = useState(false)
  const [exportBusy, setExportBusy] = useState(false)

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
      active: active === 'all' ? undefined : active,
    }),
    [page, search, active],
  )
  const queryKey = useMemo(() => ['manage-categories', filters] as const, [filters])

  const listQuery = useQuery({
    queryKey,
    queryFn: () => fetchManageCategories(filters),
  })

  const data = listQuery.data
  const categories = data?.categories ?? []
  const pages = data ? Math.max(1, Math.ceil(data.totalCount / data.pageSize)) : 1
  const hasSearch = Boolean(searchInput.trim())
  const activeFilterCount = (hasSearch ? 1 : 0) + (active !== 'all' ? 1 : 0)

  const clearFilters = () => {
    setSearchInput('')
    setSearch('')
    setActive('all')
    setPage(1)
  }

  const openCreate = () => {
    setEditing(null)
    setForm({ ...emptyForm })
    setAutoSlug(true)
    setFormOpen(true)
  }

  const openEdit = (row: ManageCategoryRow) => {
    setEditing(row)
    setForm({
      businessProfileType: row.businessProfileType,
      businessHeading: row.businessHeading,
      businessCategory: row.businessCategory,
      businessCategorySlug: row.businessCategorySlug,
      productCategory: row.productCategory,
      productCategorySlug: row.productCategorySlug,
      directoryPriority: row.directoryPriority,
      isActive: row.isActive,
      keywords: row.keywords,
      tags: row.tags,
    })
    setAutoSlug(false)
    setFormOpen(true)
  }

  const updateForm = <K extends keyof UpsertCategoryPayload>(key: K, value: UpsertCategoryPayload[K]) => {
    setForm((prev) => {
      const next = { ...prev, [key]: value }
      if (autoSlug && !editing) {
        if (key === 'businessCategory' && typeof value === 'string') {
          next.businessCategorySlug = slugify(value)
        }
        if (key === 'productCategory' && typeof value === 'string') {
          next.productCategorySlug = slugify(value)
        }
      }
      return next
    })
  }

  const submitForm = async () => {
    if (!form.businessCategory.trim() || !form.productCategory.trim()) {
      return toast.push('Business and product category are required', 'error')
    }
    setFormBusy(true)
    try {
      const payload: UpsertCategoryPayload = {
        ...form,
        businessProfileType: form.businessProfileType.trim(),
        businessHeading: form.businessHeading.trim(),
        businessCategory: form.businessCategory.trim(),
        businessCategorySlug: form.businessCategorySlug?.trim() || undefined,
        productCategory: form.productCategory.trim(),
        productCategorySlug: form.productCategorySlug?.trim() || undefined,
        directoryPriority: Number(form.directoryPriority) || 0,
        keywords: form.keywords?.trim() || '',
        tags: form.tags?.trim() || '',
      }
      const res = editing ? await updateCategory(editing.id, payload) : await createCategory(payload)
      toast.push(res.message || (editing ? 'Category updated' : 'Category added'), 'success')
      setFormOpen(false)
      await qc.invalidateQueries({ queryKey: ['manage-categories'] })
    } catch (e) {
      toast.push(e instanceof ApiError ? e.message : 'Save failed', 'error')
    } finally {
      setFormBusy(false)
    }
  }

  const runToggle = async (row: ManageCategoryRow) => {
    setTogglingId(row.id)
    try {
      const res = await toggleCategoryActive(row.id)
      toast.push(res.message || 'Status updated', 'success')
      await qc.invalidateQueries({ queryKey: ['manage-categories'] })
    } catch (e) {
      toast.push(e instanceof ApiError ? e.message : 'Status update failed', 'error')
    } finally {
      setTogglingId(null)
    }
  }

  const runDelete = async () => {
    if (!deleteTarget) return
    setDeleteBusy(true)
    try {
      const res = await deleteCategory(deleteTarget.id)
      toast.push(res.message || 'Deleted', 'success')
      setDeleteTarget(null)
      await qc.invalidateQueries({ queryKey: ['manage-categories'] })
    } catch (e) {
      toast.push(e instanceof ApiError ? e.message : 'Delete failed', 'error')
    } finally {
      setDeleteBusy(false)
    }
  }

  const runExport = async () => {
    setExportBusy(true)
    try {
      await exportCategoriesCsv()
      toast.push('Export started', 'success')
    } catch (e) {
      toast.push(e instanceof ApiError ? e.message : 'Export failed', 'error')
    } finally {
      setExportBusy(false)
    }
  }

  const runImport = async () => {
    if (!importFile) return toast.push('Please select a CSV file', 'error')
    setImportBusy(true)
    try {
      const res = await importCategoriesCsv({
        file: importFile,
        replaceAll,
        skipDuplicates,
      })
      toast.push(res.message || 'Import finished', 'success')
      setImportOpen(false)
      setImportFile(null)
      await qc.invalidateQueries({ queryKey: ['manage-categories'] })
    } catch (e) {
      toast.push(e instanceof ApiError ? e.message : 'Import failed', 'error')
    } finally {
      setImportBusy(false)
    }
  }

  return (
    <div className="flex h-full min-h-0 min-w-0 max-w-full flex-col gap-3">
      <div className="flex shrink-0 flex-wrap items-end justify-between gap-3">
        <div>
          <h1 className="font-[family-name:var(--font-display)] flex items-center gap-2 text-3xl font-semibold tracking-tight">
            <FolderTree size={28} className="text-rose-600" />
            Category Management
          </h1>
          <p className="mt-1 text-sm text-muted-foreground">
            {data?.totalCount ?? '—'} directory rows
          </p>
        </div>
        <div className="flex flex-wrap items-center gap-2">
          <FiltersButton activeCount={activeFilterCount} onClick={() => setFiltersOpen(true)} />
          <Button variant="outline" onClick={() => listQuery.refetch()} disabled={listQuery.isFetching}>
            <RefreshCw size={16} /> Refresh
          </Button>
          <Button variant="outline" onClick={() => void runExport()} disabled={exportBusy}>
            <Download size={16} /> {exportBusy ? 'Exporting…' : 'Export'}
          </Button>
          <Button
            variant="outline"
            onClick={() => {
              setImportFile(null)
              setReplaceAll(false)
              setSkipDuplicates(true)
              setImportOpen(true)
            }}
          >
            <Upload size={16} /> Import CSV
          </Button>
          <Button onClick={openCreate}>
            <Plus size={16} /> Add Category
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
              placeholder="Heading, category, slug, keywords…"
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
          <label className="text-xs font-medium text-muted-foreground">Active</label>
          <Select
            value={active}
            onValueChange={(v) => {
              setActive(v)
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
                  {[
                    'Profile Type',
                    'Heading',
                    'Business Category',
                    'Biz Slug',
                    'Product Category',
                    'Product Slug',
                    'Priority',
                    'Active',
                    'Keywords',
                    'Tags',
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
                    <TableCell colSpan={11} className="py-10 text-center text-muted-foreground">
                      Loading…
                    </TableCell>
                  </TableRow>
                )}
                {listQuery.isError && (
                  <TableRow>
                    <TableCell colSpan={11} className="py-10 text-center text-destructive">
                      {(listQuery.error as Error).message}
                    </TableCell>
                  </TableRow>
                )}
                {!listQuery.isLoading && !listQuery.isError && categories.length === 0 && (
                  <TableRow>
                    <TableCell colSpan={11} className="py-10 text-center text-muted-foreground">
                      No categories found. Import CSV or add a row.
                    </TableCell>
                  </TableRow>
                )}
                {categories.map((row) => (
                  <TableRow key={row.id}>
                    <TableCell className="max-w-[140px]">
                      <div className="line-clamp-2">{row.businessProfileType || '—'}</div>
                    </TableCell>
                    <TableCell className="max-w-[160px] font-medium">
                      <div className="line-clamp-2">{row.businessHeading || '—'}</div>
                    </TableCell>
                    <TableCell className="max-w-[180px]">
                      <div className="line-clamp-2">{row.businessCategory}</div>
                    </TableCell>
                    <TableCell>
                      <code className="rounded bg-muted px-1.5 py-0.5 text-xs">{row.businessCategorySlug}</code>
                    </TableCell>
                    <TableCell className="max-w-[180px]">
                      <div className="line-clamp-2">{row.productCategory}</div>
                    </TableCell>
                    <TableCell>
                      <code className="rounded bg-muted px-1.5 py-0.5 text-xs">{row.productCategorySlug}</code>
                    </TableCell>
                    <TableCell>{row.directoryPriority}</TableCell>
                    <TableCell>
                      <Badge variant={badgeVariantFromTone(row.activeTone)}>{row.activeLabel}</Badge>
                    </TableCell>
                    <TableCell className="max-w-[160px] text-muted-foreground">
                      <div className="line-clamp-2">{row.keywords || '—'}</div>
                    </TableCell>
                    <TableCell className="max-w-[140px] text-muted-foreground">
                      <div className="line-clamp-2">{row.tags || '—'}</div>
                    </TableCell>
                    <TableCell>
                      <div className="flex gap-1">
                        <Button
                          size="sm"
                          variant={row.isActive ? 'default' : 'secondary'}
                          disabled={togglingId === row.id}
                          onClick={() => void runToggle(row)}
                        >
                          {row.isActive ? 'Active' : 'Inactive'}
                        </Button>
                        <Button size="icon-sm" variant="ghost" title="Edit" onClick={() => openEdit(row)}>
                          <Pencil size={14} />
                        </Button>
                        <Button
                          size="icon-sm"
                          variant="ghost"
                          title="Delete"
                          onClick={() => setDeleteTarget(row)}
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

      <Drawer open={formOpen} onOpenChange={setFormOpen} direction="right">
        <DrawerContent className="data-[vaul-drawer-direction=right]:w-full data-[vaul-drawer-direction=right]:sm:max-w-xl">
          <DrawerHeader>
            <DrawerTitle>{editing ? 'Edit Category' : 'Add Category'}</DrawerTitle>
            <DrawerDescription>Directory row for business / product mapping</DrawerDescription>
          </DrawerHeader>
          <div className="sidebar-nav-scroll space-y-3 overflow-y-auto px-4 pb-2">
            <div className="grid gap-3 sm:grid-cols-2">
              <Field label="Business Profile Type">
                <Input
                  value={form.businessProfileType}
                  onChange={(e) => updateForm('businessProfileType', e.target.value)}
                />
              </Field>
              <Field label="Business Heading">
                <Input
                  value={form.businessHeading}
                  onChange={(e) => updateForm('businessHeading', e.target.value)}
                />
              </Field>
              <Field label="Business Category *">
                <Input
                  value={form.businessCategory}
                  onChange={(e) => updateForm('businessCategory', e.target.value)}
                />
              </Field>
              <Field label="Business Category Slug">
                <Input
                  value={form.businessCategorySlug || ''}
                  onChange={(e) => {
                    setAutoSlug(false)
                    updateForm('businessCategorySlug', e.target.value)
                  }}
                />
              </Field>
              <Field label="Product Category *">
                <Input
                  value={form.productCategory}
                  onChange={(e) => updateForm('productCategory', e.target.value)}
                />
              </Field>
              <Field label="Product Category Slug">
                <Input
                  value={form.productCategorySlug || ''}
                  onChange={(e) => {
                    setAutoSlug(false)
                    updateForm('productCategorySlug', e.target.value)
                  }}
                />
              </Field>
              <Field label="Directory Priority">
                <Input
                  type="number"
                  value={form.directoryPriority}
                  onChange={(e) => updateForm('directoryPriority', Number(e.target.value) || 0)}
                />
              </Field>
              <Field label="Active">
                <Select
                  value={form.isActive ? '1' : '0'}
                  onValueChange={(v) => updateForm('isActive', v === '1')}
                >
                  <SelectTrigger className="w-full">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="1">Yes</SelectItem>
                    <SelectItem value="0">No</SelectItem>
                  </SelectContent>
                </Select>
              </Field>
            </div>
            <Field label="Keywords">
              <Textarea
                rows={3}
                value={form.keywords || ''}
                onChange={(e) => updateForm('keywords', e.target.value)}
              />
            </Field>
            <Field label="Tags">
              <Textarea
                rows={2}
                value={form.tags || ''}
                onChange={(e) => updateForm('tags', e.target.value)}
              />
            </Field>
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

      <Drawer open={importOpen} onOpenChange={setImportOpen} direction="right">
        <DrawerContent className="data-[vaul-drawer-direction=right]:w-full data-[vaul-drawer-direction=right]:sm:max-w-md">
          <DrawerHeader>
            <DrawerTitle>Bulk Import (CSV)</DrawerTitle>
            <DrawerDescription>
              Required columns: Business Profile Type, Business Heading, Business Category, Business
              Category Slug, Product Category, Product Category Slug, Directory Priority, Is Active,
              Keywords, Tags
            </DrawerDescription>
          </DrawerHeader>
          <div className="space-y-4 px-4 pb-2">
            <div className="space-y-1.5">
              <label className="text-xs font-medium text-muted-foreground">CSV file</label>
              <Input
                ref={fileRef}
                type="file"
                accept=".csv,text/csv"
                onChange={(e) => setImportFile(e.target.files?.[0] ?? null)}
              />
              {importFile && (
                <p className="text-xs text-muted-foreground">{importFile.name}</p>
              )}
            </div>
            <label className="flex items-center gap-2 text-sm">
              <Checkbox checked={skipDuplicates} onCheckedChange={(v) => setSkipDuplicates(v === true)} />
              Skip duplicates (keep existing rows)
            </label>
            <label className="flex items-center gap-2 text-sm text-destructive">
              <Checkbox checked={replaceAll} onCheckedChange={(v) => setReplaceAll(v === true)} />
              Replace all (truncate table before import)
            </label>
          </div>
          <DrawerFooter>
            <Button onClick={() => void runImport()} disabled={importBusy || !importFile}>
              {importBusy ? 'Importing…' : 'Import'}
            </Button>
            <DrawerClose asChild>
              <Button variant="outline">Cancel</Button>
            </DrawerClose>
          </DrawerFooter>
        </DrawerContent>
      </Drawer>

      <AlertDialog open={Boolean(deleteTarget)} onOpenChange={(o) => !o && setDeleteTarget(null)}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Delete this row?</AlertDialogTitle>
            <AlertDialogDescription>
              {deleteTarget
                ? `${deleteTarget.businessCategory} / ${deleteTarget.productCategory}`
                : ''}
            </AlertDialogDescription>
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
