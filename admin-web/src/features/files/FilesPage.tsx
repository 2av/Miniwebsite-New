import { useQuery } from '@tanstack/react-query'
import { ExternalLink, Files, RefreshCw, Search, X } from 'lucide-react'
import { useEffect, useMemo, useState } from 'react'
import { fetchAdminFiles } from '@/features/files/api'
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

function formatSize(bytes: number) {
  if (!Number.isFinite(bytes) || bytes < 0) return '—'
  if (bytes < 1024) return `${bytes} B`
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
}

function formatWhen(value?: string | null) {
  if (!value) return '—'
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return value
  return date.toLocaleString()
}

function folderLabel(folder: string) {
  if (folder === 'profile') return 'Profile'
  if (folder === 'verification') return 'Verification'
  return folder
}

export function FilesPage() {
  const [page, setPage] = useState(1)
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [folder, setFolder] = useState('all')
  const [status, setStatus] = useState('all')
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

  const filters = useMemo(
    () => ({
      page,
      pageSize: 10,
      search: search || undefined,
      folder: folder === 'all' ? undefined : folder,
      status: status === 'all' ? undefined : status,
    }),
    [page, search, folder, status],
  )

  const listQuery = useQuery({
    queryKey: ['admin-files', filters],
    queryFn: () => fetchAdminFiles(filters),
  })

  const data = listQuery.data
  const files = data?.files ?? []
  const pages = data ? Math.max(1, Math.ceil(data.totalCount / data.pageSize)) : 1
  const hasSearch = Boolean(searchInput.trim())
  const activeFilterCount = (folder === 'all' ? 0 : 1) + (status === 'all' ? 0 : 1) + (search ? 1 : 0)

  const clearFilters = () => {
    setSearchInput('')
    setSearch('')
    setFolder('all')
    setStatus('all')
    setPage(1)
  }

  return (
    <div className="flex h-full min-h-0 min-w-0 max-w-full flex-col gap-3">
      <div className="flex shrink-0 flex-wrap items-end justify-between gap-3">
        <div>
          <h1 className="font-[family-name:var(--font-display)] flex items-center gap-2 text-3xl font-semibold tracking-tight">
            <Files size={28} className="text-violet-600" />
            Files
          </h1>
          <p className="mt-1 text-sm text-muted-foreground">
            {data?.totalCount ?? '—'} uploaded files
            {data ? ` · ${formatSize(data.totalSizeBytes)} total` : ''}. View opens the file on Cloudflare.
          </p>
          <p className="mt-1 text-xs text-muted-foreground">
            Status: Current is the file in use. Replaced is an older file kept after a newer upload.
          </p>
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
              placeholder="Name, email, or file…"
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
          <label className="text-xs font-medium text-muted-foreground">Folder</label>
          <Select
            value={folder}
            onValueChange={(v) => {
              setFolder(v)
              setPage(1)
            }}
          >
            <SelectTrigger className="w-full">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All</SelectItem>
              <SelectItem value="profile">Profile</SelectItem>
              <SelectItem value="verification">Verification</SelectItem>
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
              <SelectItem value="Current">Current</SelectItem>
              <SelectItem value="Replaced">Replaced</SelectItem>
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
                  {['View', 'User', 'Folder', 'Document', 'File name', 'Size', 'Uploaded', 'Status'].map((h) => (
                    <TableHead
                      key={h}
                      title={
                        h === 'Status'
                          ? 'Current is the file in use. Replaced is an older file kept after a newer upload.'
                          : undefined
                      }
                      className="text-xs font-semibold tracking-wide text-slate-200 uppercase"
                    >
                      {h}
                    </TableHead>
                  ))}
                </TableRow>
              </TableHeader>
              <TableBody>
                {listQuery.isLoading && (
                  <TableRow>
                    <TableCell colSpan={8} className="py-10 text-center text-muted-foreground">
                      Loading…
                    </TableCell>
                  </TableRow>
                )}
                {listQuery.isError && (
                  <TableRow>
                    <TableCell colSpan={8} className="py-10 text-center text-destructive">
                      {(listQuery.error as Error).message}
                    </TableCell>
                  </TableRow>
                )}
                {!listQuery.isLoading && !listQuery.isError && files.length === 0 && (
                  <TableRow>
                    <TableCell colSpan={8} className="py-10 text-center text-muted-foreground">
                      No files yet. A profile or verification upload will appear here.
                    </TableCell>
                  </TableRow>
                )}
                {files.map((file) => (
                  <TableRow key={file.id}>
                    <TableCell>
                      {file.viewUrl ? (
                        <a
                          href={file.viewUrl}
                          target="_blank"
                          rel="noreferrer"
                          className="inline-flex items-center gap-1 text-sm font-medium text-violet-700 hover:underline"
                        >
                          View <ExternalLink size={14} />
                        </a>
                      ) : (
                        <span className="text-muted-foreground">—</span>
                      )}
                    </TableCell>
                    <TableCell>
                      <div className="font-medium">{file.userName || '—'}</div>
                      <div className="text-xs text-muted-foreground">{file.userEmail}</div>
                    </TableCell>
                    <TableCell>{folderLabel(file.folder)}</TableCell>
                    <TableCell>{file.document}</TableCell>
                    <TableCell className="max-w-[220px]">
                      <div className="truncate font-medium" title={file.originalFileName}>
                        {file.originalFileName}
                      </div>
                      <div className="truncate text-xs text-muted-foreground" title={file.storedFileName}>
                        {file.storedFileName}
                      </div>
                    </TableCell>
                    <TableCell>{formatSize(file.sizeBytes)}</TableCell>
                    <TableCell className="whitespace-nowrap">{formatWhen(file.uploadedAt)}</TableCell>
                    <TableCell>
                      <Badge
                        title={
                          file.status === 'Current'
                            ? 'The file this user is using now'
                            : 'An older file replaced by a newer upload'
                        }
                        variant={badgeVariantFromTone(file.status === 'Current' ? 'ok' : 'warn')}
                      >
                        {file.status}
                      </Badge>
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
                Previous
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
    </div>
  )
}
