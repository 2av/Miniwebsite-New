import { useQuery, useQueryClient } from '@tanstack/react-query'
import { ArrowDown, ArrowLeft, ArrowUp, ExternalLink, Save } from 'lucide-react'
import { useEffect, useState, type ReactNode } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import {
  createDocPage,
  fetchDocPage,
  fetchGrowWithMwMeta,
  reorderDocPages,
  updateDocPage,
  uploadDocMedia,
} from '@/features/grow-with-mw/api'
import type { UpsertDocPagePayload } from '@/shared/types/api'
import { ApiError } from '@/shared/api/client'
import { HtmlRichEditor } from '@/shared/ui/HtmlRichEditor'
import { useToast } from '@/shared/ui/Toast'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
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

export function GrowWithMwPageEditorPage() {
  const { id } = useParams()
  const isNew = !id || id === 'new'
  const pageId = isNew ? null : Number(id)
  const navigate = useNavigate()
  const toast = useToast()
  const qc = useQueryClient()

  const [title, setTitle] = useState('')
  const [slug, setSlug] = useState('')
  const [sectionId, setSectionId] = useState('')
  const [contentHtml, setContentHtml] = useState('')
  const [metaTitle, setMetaTitle] = useState('')
  const [metaDescription, setMetaDescription] = useState('')
  const [metaKeywords, setMetaKeywords] = useState('')
  const [status, setStatus] = useState('draft')
  const [publicUrl, setPublicUrl] = useState('')
  const [saving, setSaving] = useState(false)

  const metaQuery = useQuery({
    queryKey: ['grow-with-mw-meta'],
    queryFn: fetchGrowWithMwMeta,
    staleTime: Infinity,
  })

  const pageQuery = useQuery({
    queryKey: ['grow-with-mw-page', pageId],
    queryFn: () => fetchDocPage(pageId!),
    enabled: pageId != null && !Number.isNaN(pageId),
  })

  useEffect(() => {
    if (!pageQuery.data) return
    const p = pageQuery.data
    setTitle(p.title)
    setSlug(p.slug)
    setSectionId(String(p.sectionId))
    setContentHtml(p.contentHtml)
    setMetaTitle(p.metaTitle)
    setMetaDescription(p.metaDescription)
    setMetaKeywords(p.metaKeywords)
    setStatus(p.status)
    setPublicUrl(p.publicUrl)
  }, [pageQuery.data])

  useEffect(() => {
    if (!isNew) return
    const first = metaQuery.data?.sections?.[0]
    if (first && !sectionId) setSectionId(String(first.id))
  }, [isNew, metaQuery.data, sectionId])

  const sections = metaQuery.data?.sections ?? []
  const sectionPages = pageQuery.data?.sectionPages ?? []

  const save = async (action: 'draft' | 'publish') => {
    if (!title.trim() || !sectionId) return toast.push('Title and section are required', 'error')
    setSaving(true)
    try {
      const payload: UpsertDocPagePayload = {
        sectionId: Number(sectionId),
        title: title.trim(),
        slug: slug.trim() || undefined,
        contentHtml,
        metaTitle,
        metaDescription,
        metaKeywords,
        action,
      }
      const res =
        pageId != null
          ? await updateDocPage(pageId, payload)
          : await createDocPage(payload)
      toast.push(res.message || (action === 'publish' ? 'Published' : 'Draft saved'), 'success')
      await qc.invalidateQueries({ queryKey: ['grow-with-mw-pages'] })
      if (res.data?.id) {
        if (isNew) navigate(`/grow-with-mw/pages/${res.data.id}`, { replace: true })
        else await qc.invalidateQueries({ queryKey: ['grow-with-mw-page', res.data.id] })
      }
    } catch (e) {
      toast.push(e instanceof ApiError ? e.message : 'Save failed', 'error')
    } finally {
      setSaving(false)
    }
  }

  const moveSectionPage = async (index: number, dir: -1 | 1) => {
    if (!pageId) return
    const next = index + dir
    if (next < 0 || next >= sectionPages.length) return
    const order = sectionPages.map((p) => p.id)
    ;[order[index], order[next]] = [order[next], order[index]]
    try {
      await reorderDocPages(Number(sectionId), order)
      await qc.invalidateQueries({ queryKey: ['grow-with-mw-page', pageId] })
    } catch (e) {
      toast.push(e instanceof ApiError ? e.message : 'Reorder failed', 'error')
    }
  }

  const insertImage = async () => {
    const input = document.createElement('input')
    input.type = 'file'
    input.accept = 'image/jpeg,image/png,image/gif,image/webp'
    input.onchange = async () => {
      const file = input.files?.[0]
      if (!file) return
      try {
        const res = await uploadDocMedia(file)
        const url = res.data?.location
        if (!url) throw new Error('No URL')
        setContentHtml((html) => `${html}<p><img src="${url}" alt="" /></p>`)
        toast.push('Image uploaded and inserted', 'success')
      } catch (e) {
        toast.push(e instanceof ApiError ? e.message : 'Upload failed', 'error')
      }
    }
    input.click()
  }

  if (!isNew && pageQuery.isLoading) {
    return <p className="text-muted-foreground">Loading page…</p>
  }

  if (!isNew && pageQuery.isError) {
    return <p className="text-destructive">{(pageQuery.error as Error).message}</p>
  }

  if (sections.length === 0 && !metaQuery.isLoading) {
    return (
      <div className="space-y-3">
        <p className="text-muted-foreground">Create at least one section before adding pages.</p>
        <Button asChild>
          <Link to="/grow-with-mw/sections">Go to sections</Link>
        </Button>
      </div>
    )
  }

  return (
    <div className="flex h-full min-h-0 min-w-0 max-w-full flex-col gap-3 overflow-hidden">
      <div className="flex shrink-0 flex-wrap items-end justify-between gap-3">
        <div>
          <Button variant="ghost" size="sm" className="mb-1 -ml-2" asChild>
            <Link to="/grow-with-mw">
              <ArrowLeft size={14} /> All pages
            </Link>
          </Button>
          <h1 className="font-[family-name:var(--font-display)] text-3xl font-semibold tracking-tight">
            {isNew ? 'New documentation page' : 'Edit documentation page'}
          </h1>
          {!isNew && (
            <p className="mt-1 text-sm text-muted-foreground">
              Status: <Badge variant="secondary">{status}</Badge>
            </p>
          )}
        </div>
        <div className="flex flex-wrap gap-2">
          {publicUrl && (
            <Button variant="outline" asChild>
              <a href={publicUrl} target="_blank" rel="noreferrer">
                <ExternalLink size={16} /> Live URL
              </a>
            </Button>
          )}
          <Button variant="secondary" disabled={saving} onClick={() => void save('draft')}>
            <Save size={16} />
            {saving ? 'Saving…' : 'Save draft'}
          </Button>
          <Button disabled={saving} onClick={() => void save('publish')}>
            Publish
          </Button>
        </div>
      </div>

      <div className="grid min-h-0 flex-1 gap-3 overflow-hidden lg:grid-cols-[1fr_300px]">
        <div className="sidebar-nav-scroll min-h-0 space-y-3 overflow-y-auto">
          <Card>
            <CardContent className="space-y-3 pt-4">
              <Field label="Title *">
                <Input value={title} onChange={(e) => setTitle(e.target.value)} className="text-lg" />
              </Field>
              <Field label="URL slug">
                <Input
                  value={slug}
                  onChange={(e) => setSlug(e.target.value)}
                  placeholder="auto from title if empty"
                />
              </Field>
              <div className="flex items-center justify-between">
                <label className="text-xs font-medium text-muted-foreground">Content</label>
                <Button type="button" size="sm" variant="outline" onClick={() => void insertImage()}>
                  Insert image
                </Button>
              </div>
              <HtmlRichEditor key={pageId ?? 'new'} value={contentHtml} onChange={setContentHtml} />
            </CardContent>
          </Card>

          {!isNew && sectionPages.length > 0 && (
            <Card>
              <CardHeader className="py-3">
                <CardTitle className="text-base">Order pages in this section</CardTitle>
              </CardHeader>
              <CardContent className="p-0">
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead className="w-20">Order</TableHead>
                      <TableHead>Title</TableHead>
                      <TableHead>Slug</TableHead>
                      <TableHead>Status</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {sectionPages.map((p, i) => (
                      <TableRow key={p.id} className={p.id === pageId ? 'bg-rose-50' : undefined}>
                        <TableCell>
                          <div className="flex gap-1">
                            <Button size="icon-sm" variant="ghost" disabled={i === 0} onClick={() => void moveSectionPage(i, -1)}>
                              <ArrowUp size={14} />
                            </Button>
                            <Button
                              size="icon-sm"
                              variant="ghost"
                              disabled={i === sectionPages.length - 1}
                              onClick={() => void moveSectionPage(i, 1)}
                            >
                              <ArrowDown size={14} />
                            </Button>
                          </div>
                        </TableCell>
                        <TableCell>{p.title}</TableCell>
                        <TableCell>
                          <code className="text-xs">{p.slug}</code>
                        </TableCell>
                        <TableCell>
                          <Badge variant="secondary">{p.status}</Badge>
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </CardContent>
            </Card>
          )}
        </div>

        <div className="sidebar-nav-scroll min-h-0 space-y-3 overflow-y-auto">
          <Card>
            <CardHeader className="py-3">
              <CardTitle className="text-base">Publish</CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              <Field label="Section *">
                <Select value={sectionId} onValueChange={setSectionId}>
                  <SelectTrigger className="w-full">
                    <SelectValue placeholder="Select section" />
                  </SelectTrigger>
                  <SelectContent>
                    {sections.map((s) => (
                      <SelectItem key={s.id} value={String(s.id)}>
                        {s.title}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </Field>
            </CardContent>
          </Card>
          <Card>
            <CardHeader className="py-3">
              <CardTitle className="text-base">SEO</CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              <Field label="Meta title">
                <Input value={metaTitle} onChange={(e) => setMetaTitle(e.target.value)} />
              </Field>
              <Field label="Meta description">
                <Textarea
                  rows={2}
                  value={metaDescription}
                  onChange={(e) => setMetaDescription(e.target.value)}
                />
              </Field>
              <Field label="Meta keywords">
                <Input value={metaKeywords} onChange={(e) => setMetaKeywords(e.target.value)} />
              </Field>
            </CardContent>
          </Card>
        </div>
      </div>
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
