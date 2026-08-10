import { useQuery, useQueryClient } from '@tanstack/react-query'
import {
  Briefcase,
  ChevronRight,
  Download,
  Eye,
  EyeOff,
  FileText,
  Folder,
  FolderOpen,
  FolderPlus,
  Handshake,
  Home,
  ImageIcon,
  Megaphone,
  Pencil,
  RefreshCw,
  Trash2,
  Upload,
  Video,
} from 'lucide-react'
import { useMemo, useRef, useState, type ReactNode } from 'react'
import { Link, Navigate, useParams, useSearchParams } from 'react-router-dom'
import {
  addKitVideoUrl,
  createKitFolder,
  deleteKitFolder,
  deleteKitItem,
  fetchKitExplorer,
  fetchKitMeta,
  moveKitItem,
  toggleKitItemStatus,
  updateKitFile,
  updateKitFolder,
  updateKitImage,
  updateKitVideo,
  uploadKitFile,
  uploadKitImage,
  uploadKitVideoFile,
} from '@/features/kit-management/api'
import type { KitFolderOption, KitFolderTile, KitItem } from '@/shared/types/api'
import { ApiError } from '@/shared/api/client'
import { badgeVariantFromTone } from '@/shared/lib/badgeTone'
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
import { Input } from '@/components/ui/input'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { cn } from '@/lib/utils'

const KIT_TABS = [
  { slug: 'sales', key: 'sales', label: 'MW Sales Kit', icon: Briefcase },
  { slug: 'marketing', key: 'marketing', label: 'Creator Kit', icon: Megaphone },
  { slug: 'franchise-sales', key: 'franchise_sales', label: 'Franchise Sales Kit', icon: Handshake },
] as const

function categoryKeyFromSlug(slug?: string) {
  return KIT_TABS.find((t) => t.slug === slug)?.key ?? 'sales'
}

function folderSelectLabel(opt: KitFolderOption) {
  return `${opt.depth > 0 ? `${'— '.repeat(opt.depth)}` : ''}${opt.title}`
}

function descendantIds(options: KitFolderOption[], rootId: number): Set<number> {
  const byParent = new Map<number, number[]>()
  for (const o of options) {
    const pid = o.parentId ?? 0
    if (!byParent.has(pid)) byParent.set(pid, [])
    byParent.get(pid)!.push(o.id)
  }
  const out = new Set<number>()
  const queue = [...(byParent.get(rootId) ?? [])]
  while (queue.length) {
    const id = queue.shift()!
    if (out.has(id)) continue
    out.add(id)
    queue.push(...(byParent.get(id) ?? []))
  }
  return out
}

function FolderSelect({
  value,
  onChange,
  options,
  excludeId,
  allowNone = true,
}: {
  value: string
  onChange: (v: string) => void
  options: KitFolderOption[]
  excludeId?: number
  allowNone?: boolean
}) {
  const blocked = useMemo(() => {
    if (!excludeId) return new Set<number>()
    const d = descendantIds(options, excludeId)
    d.add(excludeId)
    return d
  }, [options, excludeId])

  const filtered = options.filter((o) => !blocked.has(o.id))

  return (
    <Select value={value} onValueChange={onChange}>
      <SelectTrigger>
        <SelectValue placeholder="Select folder" />
      </SelectTrigger>
      <SelectContent>
        {allowNone && <SelectItem value="0">No folder (uncategorized)</SelectItem>}
        {filtered.map((o) => (
          <SelectItem key={o.id} value={String(o.id)}>
            {folderSelectLabel(o)}
          </SelectItem>
        ))}
      </SelectContent>
    </Select>
  )
}

function StatChip({ label, value }: { label: string; value: number }) {
  return (
    <div className="rounded-xl border border-slate-200 bg-white px-4 py-3 text-center shadow-sm">
      <div className="font-[family-name:var(--font-display)] text-2xl font-semibold text-slate-900">{value}</div>
      <div className="text-xs tracking-wide text-muted-foreground uppercase">{label}</div>
    </div>
  )
}

export function KitManagementPage() {
  const { categorySlug } = useParams()
  const [searchParams, setSearchParams] = useSearchParams()
  const category = categoryKeyFromSlug(categorySlug)
  const folderId = Number(searchParams.get('folder') || '0') || 0

  const toast = useToast()
  const qc = useQueryClient()

  const metaQuery = useQuery({ queryKey: ['kit-meta'], queryFn: fetchKitMeta })
  const explorerQuery = useQuery({
    queryKey: ['kit-explorer', category, folderId],
    queryFn: () => fetchKitExplorer(category, folderId),
  })

  const explorer = explorerQuery.data
  const counts = metaQuery.data?.categories ?? []

  const [folderDialog, setFolderDialog] = useState<'create' | 'edit' | null>(null)
  const [editingFolder, setEditingFolder] = useState<KitFolderTile | null>(null)
  const [deleteFolderTarget, setDeleteFolderTarget] = useState<KitFolderTile | null>(null)
  const [itemDialog, setItemDialog] = useState<'image' | 'video-url' | 'video-file' | 'file' | null>(null)
  const [editingItem, setEditingItem] = useState<KitItem | null>(null)
  const [moveItem, setMoveItem] = useState<KitItem | null>(null)
  const [deleteItemTarget, setDeleteItemTarget] = useState<KitItem | null>(null)
  const [busy, setBusy] = useState(false)

  const [folderTitle, setFolderTitle] = useState('')
  const [folderOrder, setFolderOrder] = useState('0')
  const [folderParent, setFolderParent] = useState('0')
  const [folderStatus, setFolderStatus] = useState('active')

  const [itemTitle, setItemTitle] = useState('')
  const [itemOrder, setItemOrder] = useState('0')
  const [itemFolder, setItemFolder] = useState(String(folderId))
  const [itemStatus, setItemStatus] = useState('active')
  const [videoUrl, setVideoUrl] = useState('')

  const imageRef = useRef<HTMLInputElement>(null)
  const videoFileRef = useRef<HTMLInputElement>(null)
  const fileRef = useRef<HTMLInputElement>(null)
  const editFileRef = useRef<HTMLInputElement>(null)

  const invalidate = async () => {
    await qc.invalidateQueries({ queryKey: ['kit-explorer', category] })
    await qc.invalidateQueries({ queryKey: ['kit-meta'] })
  }

  const goFolder = (id: number) => {
    const next = new URLSearchParams(searchParams)
    if (id > 0) next.set('folder', String(id))
    else next.delete('folder')
    setSearchParams(next)
  }

  const resetFolderForm = (parent = folderId) => {
    setFolderTitle('')
    setFolderOrder('0')
    setFolderParent(String(parent))
    setFolderStatus('active')
  }

  const resetItemForm = (targetFolder = folderId) => {
    setItemTitle('')
    setItemOrder('0')
    setItemFolder(String(targetFolder))
    setItemStatus('active')
    setVideoUrl('')
  }

  const run = async (fn: () => Promise<{ message?: string | null }>) => {
    setBusy(true)
    try {
      const res = await fn()
      toast.push(res.message || 'Done', 'success')
      await invalidate()
      return true
    } catch (e) {
      toast.push(e instanceof ApiError ? e.message : 'Request failed', 'error')
      return false
    } finally {
      setBusy(false)
    }
  }

  const onCreateFolder = async () => {
    const ok = await run(() =>
      createKitFolder({
        category,
        title: folderTitle,
        parentId: Number(folderParent) || null,
        displayOrder: Number(folderOrder) || 0,
      }),
    )
    if (ok) {
      setFolderDialog(null)
      resetFolderForm()
    }
  }

  const onUpdateFolder = async () => {
    if (!editingFolder) return
    const ok = await run(() =>
      updateKitFolder(editingFolder.id, {
        category,
        title: folderTitle,
        parentId: Number(folderParent) || null,
        displayOrder: Number(folderOrder) || 0,
        status: folderStatus,
      }),
    )
    if (ok) {
      setFolderDialog(null)
      setEditingFolder(null)
    }
  }

  const onDeleteFolder = async () => {
    if (!deleteFolderTarget) return
    const ok = await run(() => deleteKitFolder(deleteFolderTarget.id, category))
    if (ok) {
      if (folderId === deleteFolderTarget.id) goFolder(0)
      setDeleteFolderTarget(null)
    }
  }

  const onAddImage = async (file: File | null) => {
    if (!file) return
    const form = new FormData()
    form.append('category', category)
    form.append('title', itemTitle)
    form.append('folderId', itemFolder === '0' ? '' : itemFolder)
    form.append('displayOrder', itemOrder)
    form.append('file', file)
    const ok = await run(() => uploadKitImage(form))
    if (ok) {
      setItemDialog(null)
      resetItemForm()
      if (imageRef.current) imageRef.current.value = ''
    }
  }

  const onAddVideoUrl = async () => {
    const ok = await run(() =>
      addKitVideoUrl({
        category,
        title: itemTitle,
        videoUrl,
        folderId: Number(itemFolder) || null,
        displayOrder: Number(itemOrder) || 0,
      }),
    )
    if (ok) {
      setItemDialog(null)
      resetItemForm()
    }
  }

  const onAddVideoFile = async (file: File | null) => {
    if (!file) return
    const form = new FormData()
    form.append('category', category)
    form.append('title', itemTitle)
    form.append('folderId', itemFolder === '0' ? '' : itemFolder)
    form.append('displayOrder', itemOrder)
    form.append('file', file)
    const ok = await run(() => uploadKitVideoFile(form))
    if (ok) {
      setItemDialog(null)
      resetItemForm()
      if (videoFileRef.current) videoFileRef.current.value = ''
    }
  }

  const onAddFile = async (file: File | null) => {
    if (!file) return
    const form = new FormData()
    form.append('category', category)
    form.append('title', itemTitle)
    form.append('folderId', itemFolder === '0' ? '' : itemFolder)
    form.append('displayOrder', itemOrder)
    form.append('file', file)
    const ok = await run(() => uploadKitFile(form))
    if (ok) {
      setItemDialog(null)
      resetItemForm()
      if (fileRef.current) fileRef.current.value = ''
    }
  }

  const onSaveEditItem = async (file?: File | null) => {
    if (!editingItem) return
    if (editingItem.type === 'video') {
      const ok = await run(() =>
        updateKitVideo(editingItem.id, {
          category,
          title: itemTitle,
          videoUrl: videoUrl || null,
          folderId: Number(itemFolder) || null,
          displayOrder: Number(itemOrder) || 0,
          status: itemStatus,
        }),
      )
      if (ok) setEditingItem(null)
      return
    }

    const form = new FormData()
    form.append('category', category)
    form.append('title', itemTitle)
    form.append('folderId', itemFolder === '0' ? '' : itemFolder)
    form.append('displayOrder', itemOrder)
    form.append('status', itemStatus)
    if (file) form.append('file', file)

    const ok = await run(() =>
      editingItem.type === 'image' ? updateKitImage(editingItem.id, form) : updateKitFile(editingItem.id, form),
    )
    if (ok) {
      setEditingItem(null)
      if (editFileRef.current) editFileRef.current.value = ''
    }
  }

  const onMove = async () => {
    if (!moveItem) return
    const ok = await run(() =>
      moveKitItem(moveItem.id, { category, folderId: Number(itemFolder) || null }),
    )
    if (ok) setMoveItem(null)
  }

  const onDeleteItem = async () => {
    if (!deleteItemTarget) return
    const ok = await run(() => deleteKitItem(deleteItemTarget.id))
    if (ok) setDeleteItemTarget(null)
  }

  const openEditFolder = (folder: KitFolderTile) => {
    const meta = folderOptions.find((o) => o.id === folder.id)
    setEditingFolder(folder)
    setFolderTitle(folder.title)
    setFolderOrder(String(folder.displayOrder))
    setFolderParent(String(meta?.parentId ?? 0))
    setFolderStatus(folder.status)
    setFolderDialog('edit')
  }

  const openEditItem = (item: KitItem) => {
    setEditingItem(item)
    setItemTitle(item.title)
    setItemOrder(String(item.displayOrder))
    setItemFolder(String(item.folderId ?? 0))
    setItemStatus(item.status)
    setVideoUrl(item.videoUrl ?? '')
  }

  const folderOptions = explorer?.folderOptions ?? []

  return (
    <div className="flex h-full min-h-0 min-w-0 max-w-full flex-col gap-4">
      <div className="flex shrink-0 flex-wrap items-end justify-between gap-3">
        <div>
          <h1 className="font-[family-name:var(--font-display)] flex items-center gap-2 text-3xl font-semibold tracking-tight">
            <FolderOpen size={28} className="text-rose-600" />
            Kit Management
          </h1>
          <p className="mt-1 text-sm text-muted-foreground">
            Promotional materials and resources for franchisees
          </p>
        </div>
        <Button variant="outline" onClick={() => explorerQuery.refetch()} disabled={explorerQuery.isFetching}>
          <RefreshCw size={16} className={cn(explorerQuery.isFetching && 'animate-spin')} />
          Refresh
        </Button>
      </div>

      <div className="flex shrink-0 flex-wrap gap-2">
        {KIT_TABS.map((tab) => {
          const count = counts.find((c) => c.key === tab.key)?.itemCount ?? 0
          const active = category === tab.key
          return (
            <Link
              key={tab.key}
              to={`/kit-management/${tab.slug}${folderId ? `?folder=${folderId}` : ''}`}
              className={cn(
                'inline-flex items-center gap-2 rounded-xl border px-4 py-2 text-sm font-medium transition',
                active
                  ? 'border-rose-300 bg-white text-rose-700 shadow-sm ring-1 ring-rose-200'
                  : 'border-slate-200 bg-white text-slate-600 hover:border-slate-300',
              )}
            >
              <tab.icon size={16} />
              {tab.label}
              <Badge variant="secondary">{count}</Badge>
            </Link>
          )
        })}
      </div>

      {explorer?.stats && (
        <div className="grid shrink-0 grid-cols-2 gap-2 sm:grid-cols-3 lg:grid-cols-6">
          <StatChip label="Folders" value={explorer.stats.folders} />
          <StatChip label="Images" value={explorer.stats.images} />
          <StatChip label="Videos" value={explorer.stats.videos} />
          <StatChip label="Files" value={explorer.stats.files} />
          <StatChip label="Active" value={explorer.stats.activeItems} />
          <StatChip label="Total" value={explorer.stats.totalItems} />
        </div>
      )}

      <Card className="flex min-h-0 min-w-0 flex-1 flex-col overflow-hidden py-0">
        <div className="flex shrink-0 flex-wrap items-center justify-between gap-2 border-b px-4 py-3">
          <nav className="flex flex-wrap items-center gap-1 text-sm">
            <button
              type="button"
              className="inline-flex items-center gap-1 text-rose-700 hover:underline"
              onClick={() => goFolder(0)}
            >
              <Home size={14} />
              {explorer?.categoryLabel ?? 'Kit'}
            </button>
            {explorer?.breadcrumb.map((crumb) => (
              <span key={crumb.id} className="inline-flex items-center gap-1">
                <ChevronRight size={14} className="text-muted-foreground" />
                {crumb.id === folderId ? (
                  <span className="font-medium text-slate-900">{crumb.title}</span>
                ) : (
                  <button type="button" className="text-rose-700 hover:underline" onClick={() => goFolder(crumb.id)}>
                    {crumb.title}
                  </button>
                )}
              </span>
            ))}
          </nav>
          <Badge variant="outline">
            {explorer?.subfolders.length ?? 0} folders · {explorer?.items.length ?? 0} items
          </Badge>
        </div>

        <CardContent className="flex min-h-0 flex-1 flex-col gap-4 overflow-auto p-4">
          <div className="flex flex-wrap gap-2">
            <Button
              onClick={() => {
                resetFolderForm(folderId)
                setEditingFolder(null)
                setFolderDialog('create')
              }}
            >
              <FolderPlus size={16} /> Add Folder
            </Button>
            <Button
              variant="secondary"
              onClick={() => {
                resetItemForm(folderId)
                setItemDialog('image')
              }}
            >
              <ImageIcon size={16} /> Add Image
            </Button>
            <Button
              variant="secondary"
              onClick={() => {
                resetItemForm(folderId)
                setItemDialog('video-url')
              }}
            >
              <Video size={16} /> Video Link
            </Button>
            <Button
              variant="outline"
              onClick={() => {
                resetItemForm(folderId)
                setItemDialog('video-file')
              }}
            >
              <Upload size={16} /> Upload Video
            </Button>
            <Button
              variant="outline"
              onClick={() => {
                resetItemForm(folderId)
                setItemDialog('file')
              }}
            >
              <FileText size={16} /> Add File
            </Button>
          </div>

          {explorerQuery.isLoading && (
            <p className="py-10 text-center text-muted-foreground">Loading kit contents…</p>
          )}

          {!explorerQuery.isLoading && explorer && explorer.subfolders.length > 0 && (
            <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-6">
              {explorer.subfolders.map((folder) => (
                <div
                  key={folder.id}
                  className="group relative rounded-xl border border-amber-200/80 bg-gradient-to-b from-amber-50 to-white p-3 shadow-sm transition hover:shadow-md"
                >
                  <button type="button" className="w-full text-left" onClick={() => goFolder(folder.id)}>
                    <Folder size={36} className="mb-2 text-amber-500" />
                    <div className="line-clamp-2 text-sm font-medium text-slate-900">{folder.title}</div>
                    <div className="mt-1 text-xs text-muted-foreground">
                      {folder.subfolderCount} sub · {folder.directItemCount} items
                    </div>
                  </button>
                  <div className="absolute top-2 right-2 flex gap-1 opacity-0 transition group-hover:opacity-100">
                    <Button size="icon" variant="secondary" className="h-7 w-7" onClick={() => openEditFolder(folder)}>
                      <Pencil size={12} />
                    </Button>
                    <Button
                      size="icon"
                      variant="secondary"
                      className="h-7 w-7 text-red-600"
                      onClick={() => setDeleteFolderTarget(folder)}
                    >
                      <Trash2 size={12} />
                    </Button>
                  </div>
                </div>
              ))}
            </div>
          )}

          {!explorerQuery.isLoading && explorer && explorer.items.length > 0 && (
            <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
              {explorer.items.map((item) => (
                <KitItemCard
                  key={item.id}
                  item={item}
                  onEdit={() => openEditItem(item)}
                  onMove={() => {
                    setMoveItem(item)
                    setItemFolder(String(item.folderId ?? 0))
                  }}
                  onToggle={async () => {
                    const next = item.status === 'active' ? 'inactive' : 'active'
                    await run(() => toggleKitItemStatus(item.id, next))
                  }}
                  onDelete={() => setDeleteItemTarget(item)}
                />
              ))}
            </div>
          )}

          {!explorerQuery.isLoading &&
            explorer &&
            explorer.subfolders.length === 0 &&
            explorer.items.length === 0 && (
              <div className="flex flex-col items-center justify-center rounded-xl border border-dashed py-16 text-center">
                <FolderOpen size={40} className="mb-3 text-muted-foreground" />
                <h3 className="text-lg font-medium">{folderId ? 'This folder is empty' : 'No folders or items yet'}</h3>
                <p className="mt-1 max-w-md text-sm text-muted-foreground">
                  Create a folder or add images, videos, and files here.
                </p>
              </div>
            )}
        </CardContent>
      </Card>

      <Dialog open={folderDialog !== null} onOpenChange={(o) => !o && setFolderDialog(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{folderDialog === 'edit' ? 'Edit folder' : 'Create folder'}</DialogTitle>
          </DialogHeader>
          <div className="space-y-3">
            <div>
              <label className="mb-1 block text-sm font-medium">Parent folder</label>
              <FolderSelect
                value={folderParent}
                onChange={setFolderParent}
                options={folderOptions}
                excludeId={editingFolder?.id}
              />
            </div>
            <div>
              <label className="mb-1 block text-sm font-medium">Folder name</label>
              <Input value={folderTitle} onChange={(e) => setFolderTitle(e.target.value)} />
            </div>
            <div>
              <label className="mb-1 block text-sm font-medium">Display order</label>
              <Input type="number" min={0} value={folderOrder} onChange={(e) => setFolderOrder(e.target.value)} />
            </div>
            {folderDialog === 'edit' && (
              <div>
                <label className="mb-1 block text-sm font-medium">Status</label>
                <Select value={folderStatus} onValueChange={setFolderStatus}>
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="active">Active</SelectItem>
                    <SelectItem value="inactive">Inactive</SelectItem>
                  </SelectContent>
                </Select>
              </div>
            )}
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setFolderDialog(null)}>
              Cancel
            </Button>
            <Button disabled={busy || !folderTitle.trim()} onClick={() => void (folderDialog === 'edit' ? onUpdateFolder() : onCreateFolder())}>
              Save
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <Dialog open={deleteFolderTarget !== null} onOpenChange={(o) => !o && setDeleteFolderTarget(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Delete folder</DialogTitle>
            <DialogDescription>
              Delete &quot;{deleteFolderTarget?.title}&quot;? All subfolders and items inside will be permanently deleted.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="outline" onClick={() => setDeleteFolderTarget(null)}>
              Cancel
            </Button>
            <Button variant="destructive" disabled={busy} onClick={() => void onDeleteFolder()}>
              Delete
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <ItemFormDialog
        open={itemDialog === 'image'}
        title="Add image"
        onClose={() => setItemDialog(null)}
        busy={busy}
        folderOptions={folderOptions}
        itemFolder={itemFolder}
        setItemFolder={setItemFolder}
        itemTitle={itemTitle}
        setItemTitle={setItemTitle}
        itemOrder={itemOrder}
        setItemOrder={setItemOrder}
        onSubmit={() => imageRef.current?.click()}
      >
        <Input
          ref={imageRef}
          type="file"
          accept="image/jpeg,image/png,image/gif"
          className="hidden"
          onChange={(e) => void onAddImage(e.target.files?.[0] ?? null)}
        />
        <p className="text-xs text-muted-foreground">JPG, PNG, GIF · max 10MB</p>
        <Button type="button" variant="outline" onClick={() => imageRef.current?.click()}>
          Choose image
        </Button>
      </ItemFormDialog>

      <ItemFormDialog
        open={itemDialog === 'video-url'}
        title="Add video link"
        onClose={() => setItemDialog(null)}
        busy={busy}
        folderOptions={folderOptions}
        itemFolder={itemFolder}
        setItemFolder={setItemFolder}
        itemTitle={itemTitle}
        setItemTitle={setItemTitle}
        itemOrder={itemOrder}
        setItemOrder={setItemOrder}
        onSubmit={() => void onAddVideoUrl()}
        submitLabel="Add link"
      >
        <div>
          <label className="mb-1 block text-sm font-medium">Video URL</label>
          <Input value={videoUrl} onChange={(e) => setVideoUrl(e.target.value)} placeholder="https://..." />
        </div>
      </ItemFormDialog>

      <ItemFormDialog
        open={itemDialog === 'video-file'}
        title="Upload video file"
        onClose={() => setItemDialog(null)}
        busy={busy}
        folderOptions={folderOptions}
        itemFolder={itemFolder}
        setItemFolder={setItemFolder}
        itemTitle={itemTitle}
        setItemTitle={setItemTitle}
        itemOrder={itemOrder}
        setItemOrder={setItemOrder}
        onSubmit={() => videoFileRef.current?.click()}
      >
        <Input
          ref={videoFileRef}
          type="file"
          accept="video/mp4,video/webm,video/quicktime,video/x-msvideo"
          className="hidden"
          onChange={(e) => void onAddVideoFile(e.target.files?.[0] ?? null)}
        />
        <p className="text-xs text-muted-foreground">MP4, WEBM, MOV, AVI · max 50MB</p>
        <Button type="button" variant="outline" onClick={() => videoFileRef.current?.click()}>
          Choose video
        </Button>
      </ItemFormDialog>

      <ItemFormDialog
        open={itemDialog === 'file'}
        title="Add file"
        onClose={() => setItemDialog(null)}
        busy={busy}
        folderOptions={folderOptions}
        itemFolder={itemFolder}
        setItemFolder={setItemFolder}
        itemTitle={itemTitle}
        setItemTitle={setItemTitle}
        itemOrder={itemOrder}
        setItemOrder={setItemOrder}
        onSubmit={() => fileRef.current?.click()}
      >
        <Input
          ref={fileRef}
          type="file"
          accept=".pdf,.doc,.docx,.xls,.xlsx,.ppt,.pptx,.txt,.zip,.rar,.mp4,.avi,.mov,.mp3,.wav"
          className="hidden"
          onChange={(e) => void onAddFile(e.target.files?.[0] ?? null)}
        />
        <p className="text-xs text-muted-foreground">PDF, Office docs, archives, media · max 10MB</p>
        <Button type="button" variant="outline" onClick={() => fileRef.current?.click()}>
          Choose file
        </Button>
      </ItemFormDialog>

      <Dialog open={editingItem !== null} onOpenChange={(o) => !o && setEditingItem(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Edit {editingItem?.type}</DialogTitle>
          </DialogHeader>
          <div className="space-y-3">
            <div>
              <label className="mb-1 block text-sm font-medium">Title</label>
              <Input value={itemTitle} onChange={(e) => setItemTitle(e.target.value)} />
            </div>
            {editingItem?.type === 'video' && (
              <div>
                <label className="mb-1 block text-sm font-medium">Video URL</label>
                <Input value={videoUrl} onChange={(e) => setVideoUrl(e.target.value)} placeholder="Optional if file uploaded" />
              </div>
            )}
            <div>
              <label className="mb-1 block text-sm font-medium">Folder</label>
              <FolderSelect value={itemFolder} onChange={setItemFolder} options={folderOptions} />
            </div>
            <div>
              <label className="mb-1 block text-sm font-medium">Display order</label>
              <Input type="number" min={0} value={itemOrder} onChange={(e) => setItemOrder(e.target.value)} />
            </div>
            <div>
              <label className="mb-1 block text-sm font-medium">Status</label>
              <Select value={itemStatus} onValueChange={setItemStatus}>
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="active">Active</SelectItem>
                  <SelectItem value="inactive">Inactive</SelectItem>
                </SelectContent>
              </Select>
            </div>
            {(editingItem?.type === 'image' || editingItem?.type === 'file') && (
              <div>
                <label className="mb-1 block text-sm font-medium">Replace file (optional)</label>
                <Input
                  ref={editFileRef}
                  type="file"
                  onChange={(e) => void onSaveEditItem(e.target.files?.[0] ?? null)}
                />
              </div>
            )}
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setEditingItem(null)}>
              Cancel
            </Button>
            <Button
              disabled={busy}
              onClick={() => void onSaveEditItem(editingItem?.type === 'video' ? undefined : editFileRef.current?.files?.[0])}
            >
              Save
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <Dialog open={moveItem !== null} onOpenChange={(o) => !o && setMoveItem(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Move to folder</DialogTitle>
          </DialogHeader>
          <FolderSelect value={itemFolder} onChange={setItemFolder} options={folderOptions} />
          <DialogFooter>
            <Button variant="outline" onClick={() => setMoveItem(null)}>
              Cancel
            </Button>
            <Button disabled={busy} onClick={() => void onMove()}>
              Move
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <Dialog open={deleteItemTarget !== null} onOpenChange={(o) => !o && setDeleteItemTarget(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Delete item</DialogTitle>
            <DialogDescription>
              Permanently delete &quot;{deleteItemTarget?.title || 'Untitled'}&quot;?
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="outline" onClick={() => setDeleteItemTarget(null)}>
              Cancel
            </Button>
            <Button variant="destructive" disabled={busy} onClick={() => void onDeleteItem()}>
              Delete
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}

function ItemFormDialog({
  open,
  title,
  onClose,
  busy,
  folderOptions,
  itemFolder,
  setItemFolder,
  itemTitle,
  setItemTitle,
  itemOrder,
  setItemOrder,
  onSubmit,
  submitLabel = 'Save',
  children,
}: {
  open: boolean
  title: string
  onClose: () => void
  busy: boolean
  folderOptions: KitFolderOption[]
  itemFolder: string
  setItemFolder: (v: string) => void
  itemTitle: string
  setItemTitle: (v: string) => void
  itemOrder: string
  setItemOrder: (v: string) => void
  onSubmit: () => void
  submitLabel?: string
  children?: ReactNode
}) {
  return (
    <Dialog open={open} onOpenChange={(o) => !o && onClose()}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{title}</DialogTitle>
        </DialogHeader>
        <div className="space-y-3">
          <div>
            <label className="mb-1 block text-sm font-medium">Folder</label>
            <FolderSelect value={itemFolder} onChange={setItemFolder} options={folderOptions} />
          </div>
          <div>
            <label className="mb-1 block text-sm font-medium">Title</label>
            <Input value={itemTitle} onChange={(e) => setItemTitle(e.target.value)} />
          </div>
          <div>
            <label className="mb-1 block text-sm font-medium">Display order</label>
            <Input type="number" min={0} value={itemOrder} onChange={(e) => setItemOrder(e.target.value)} />
          </div>
          {children}
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={onClose}>
            Cancel
          </Button>
          <Button disabled={busy} onClick={onSubmit}>
            {submitLabel}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}

function KitItemCard({
  item,
  onEdit,
  onMove,
  onToggle,
  onDelete,
}: {
  item: KitItem
  onEdit: () => void
  onMove: () => void
  onToggle: () => void
  onDelete: () => void
}) {
  const tone = item.status === 'active' ? 'ok' : 'neutral'
  const ext = item.filePath?.split('.').pop()?.toUpperCase()

  return (
    <div className="flex flex-col overflow-hidden rounded-xl border bg-white shadow-sm">
      {item.type === 'image' && item.fileUrl && (
        <img src={item.fileUrl} alt={item.title} className="aspect-[4/3] w-full object-cover" />
      )}
      {item.type === 'video' && (
        <div className="flex aspect-video items-center justify-center bg-slate-100">
          {item.fileUrl ? (
            <video controls className="max-h-full w-full" src={item.fileUrl} />
          ) : (
            <Video size={40} className="text-rose-500" />
          )}
        </div>
      )}
      {item.type === 'file' && (
        <div className="flex aspect-[4/3] items-center justify-center bg-slate-50">
          <FileText size={48} className="text-sky-600" />
        </div>
      )}
      <div className="flex flex-1 flex-col p-3">
        <h3 className="line-clamp-2 font-medium">{item.title || 'Untitled'}</h3>
        {item.type === 'file' && ext && <p className="text-xs text-muted-foreground">{ext} file</p>}
        {item.type === 'video' && item.videoUrl && (
          <p className="mt-1 line-clamp-2 text-xs break-all text-muted-foreground">{item.videoUrl}</p>
        )}
        <div className="mt-2 flex items-center justify-between gap-2">
          <span className="text-xs text-muted-foreground">Order {item.displayOrder}</span>
          <Badge variant={badgeVariantFromTone(tone)}>{item.status}</Badge>
        </div>
        <div className="mt-3 flex flex-wrap gap-1">
          {item.fileUrl && (
            <Button size="icon" variant="outline" className="h-8 w-8" asChild>
              <a href={item.fileUrl} target="_blank" rel="noreferrer">
                <Download size={14} />
              </a>
            </Button>
          )}
          <Button size="icon" variant="outline" className="h-8 w-8" onClick={onMove}>
            <Folder size={14} />
          </Button>
          <Button size="icon" variant="outline" className="h-8 w-8" onClick={onEdit}>
            <Pencil size={14} />
          </Button>
          <Button size="icon" variant="outline" className="h-8 w-8" onClick={onToggle}>
            {item.status === 'active' ? <EyeOff size={14} /> : <Eye size={14} />}
          </Button>
          <Button size="icon" variant="outline" className="h-8 w-8 text-red-600" onClick={onDelete}>
            <Trash2 size={14} />
          </Button>
        </div>
      </div>
    </div>
  )
}

export function KitManagementRedirect() {
  return <Navigate to="/kit-management/sales" replace />
}

export function KitManagementInvalidCategory() {
  const { categorySlug } = useParams()
  const valid = KIT_TABS.some((t) => t.slug === categorySlug)
  if (!valid) return <Navigate to="/kit-management/sales" replace />
  return <KitManagementPage />
}
