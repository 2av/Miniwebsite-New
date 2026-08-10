import { useQuery, useQueryClient } from '@tanstack/react-query'
import { Copy, Images, RefreshCw, Upload } from 'lucide-react'
import { useRef, useState } from 'react'
import { fetchDocMedia, uploadDocMedia } from '@/features/grow-with-mw/api'
import { ApiError } from '@/shared/api/client'
import { useToast } from '@/shared/ui/Toast'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'

export function GrowWithMwMediaPage() {
  const toast = useToast()
  const qc = useQueryClient()
  const fileRef = useRef<HTMLInputElement>(null)
  const [uploading, setUploading] = useState(false)

  const listQuery = useQuery({
    queryKey: ['grow-with-mw-media'],
    queryFn: fetchDocMedia,
  })

  const items = listQuery.data?.items ?? []

  const onUpload = async (file: File | null) => {
    if (!file) return
    setUploading(true)
    try {
      const res = await uploadDocMedia(file)
      toast.push(res.message || 'Uploaded', 'success')
      await qc.invalidateQueries({ queryKey: ['grow-with-mw-media'] })
    } catch (e) {
      toast.push(e instanceof ApiError ? e.message : 'Upload failed', 'error')
    } finally {
      setUploading(false)
      if (fileRef.current) fileRef.current.value = ''
    }
  }

  const copyUrl = async (url: string) => {
    try {
      await navigator.clipboard.writeText(url)
      toast.push('URL copied', 'success')
    } catch {
      toast.push('Could not copy', 'error')
    }
  }

  return (
    <div className="flex h-full min-h-0 min-w-0 max-w-full flex-col gap-3">
      <div className="flex shrink-0 flex-wrap items-end justify-between gap-3">
        <div>
          <h1 className="font-[family-name:var(--font-display)] flex items-center gap-2 text-3xl font-semibold tracking-tight">
            <Images size={28} className="text-rose-600" />
            Documentation media
          </h1>
          <p className="mt-1 text-sm text-muted-foreground">
            Uploads for the rich text editor (JPG, PNG, GIF, WebP · max 5MB)
          </p>
        </div>
        <div className="flex flex-wrap items-center gap-2">
          <Button variant="outline" onClick={() => listQuery.refetch()} disabled={listQuery.isFetching}>
            <RefreshCw size={16} /> Refresh
          </Button>
          <Input
            ref={fileRef}
            type="file"
            accept="image/jpeg,image/png,image/gif,image/webp"
            className="hidden"
            onChange={(e) => void onUpload(e.target.files?.[0] ?? null)}
          />
          <Button disabled={uploading} onClick={() => fileRef.current?.click()}>
            <Upload size={16} />
            {uploading ? 'Uploading…' : 'Upload image'}
          </Button>
        </div>
      </div>

      <Card className="flex min-h-0 min-w-0 max-w-full flex-1 flex-col overflow-hidden py-0">
        <CardContent className="flex min-h-0 flex-1 flex-col p-0">
          <div className="min-h-0 flex-1 overflow-auto">
            <Table className="w-max min-w-full">
              <TableHeader className="sticky top-0 z-10 bg-slate-900">
                <TableRow className="border-slate-800 hover:bg-slate-900">
                  {['Preview', 'File', 'URL', 'Size', 'When', ''].map((h) => (
                    <TableHead key={h || 'a'} className="text-xs font-semibold tracking-wide text-slate-200 uppercase">
                      {h}
                    </TableHead>
                  ))}
                </TableRow>
              </TableHeader>
              <TableBody>
                {listQuery.isLoading && (
                  <TableRow>
                    <TableCell colSpan={6} className="py-10 text-center text-muted-foreground">
                      Loading…
                    </TableCell>
                  </TableRow>
                )}
                {!listQuery.isLoading && items.length === 0 && (
                  <TableRow>
                    <TableCell colSpan={6} className="py-10 text-center text-muted-foreground">
                      No uploads yet.
                    </TableCell>
                  </TableRow>
                )}
                {items.map((m) => (
                  <TableRow key={m.id}>
                    <TableCell>
                      {m.isImage ? (
                        <img
                          src={m.url}
                          alt=""
                          className="h-12 w-12 rounded object-cover"
                        />
                      ) : (
                        '—'
                      )}
                    </TableCell>
                    <TableCell>
                      <code className="text-xs">{m.filename}</code>
                    </TableCell>
                    <TableCell className="max-w-[320px]">
                      <a
                        href={m.url}
                        target="_blank"
                        rel="noreferrer"
                        className="line-clamp-2 text-sm text-rose-600 hover:underline"
                      >
                        {m.url}
                      </a>
                    </TableCell>
                    <TableCell>{m.sizeBytes}</TableCell>
                    <TableCell className="text-muted-foreground">{m.createdAtDisplay}</TableCell>
                    <TableCell>
                      <Button size="icon-sm" variant="ghost" title="Copy URL" onClick={() => void copyUrl(m.url)}>
                        <Copy size={14} />
                      </Button>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </div>
        </CardContent>
      </Card>
    </div>
  )
}
