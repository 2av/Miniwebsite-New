import { useQuery, useQueryClient } from '@tanstack/react-query'
import { FileText, RefreshCw, Save } from 'lucide-react'
import { useEffect, useMemo, useState } from 'react'
import { fetchManageContent, upsertManageContent } from '@/features/manage-content/api'
import type { ManageContentItem } from '@/shared/types/api'
import { ApiError } from '@/shared/api/client'
import { useToast } from '@/shared/ui/Toast'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Textarea } from '@/components/ui/textarea'
import { HtmlRichEditor } from '@/shared/ui/HtmlRichEditor'

export const CONTENT_TYPES = [
  { value: 'terms_conditions', slug: 'terms-conditions', label: 'Terms & Conditions', badge: 'Legal' },
  { value: 'privacy_policy', slug: 'privacy-policy', label: 'Privacy Policy', badge: 'Privacy' },
  { value: 'franchisee_agreement', slug: 'franchisee-agreement', label: 'Franchisee Agreement', badge: 'Partnership' },
  { value: 'franchisee_distributer', slug: 'franchisee-distributer', label: 'Franchisee Distributer', badge: 'Program' },
  {
    value: 'mw_full_franchise_agreement',
    slug: 'mw-full-franchise-agreement',
    label: 'MW Full Franchise Agreement',
    badge: 'Franchise',
  },
  {
    value: 'mw_franchisee_operation_policy',
    slug: 'mw-franchisee-operation-policy',
    label: 'MW Franchisee Operation Policy',
    badge: 'Policy',
  },
] as const

export type ContentTypeValue = (typeof CONTENT_TYPES)[number]['value']

type FormState = {
  title: string
  content: string
  metaDescription: string
  metaKeywords: string
}

const emptyForm: FormState = {
  title: '',
  content: '',
  metaDescription: '',
  metaKeywords: '',
}

function toForm(item?: ManageContentItem | null): FormState {
  if (!item) return { ...emptyForm }
  return {
    title: item.title || '',
    content: item.content || '',
    metaDescription: item.metaDescription || '',
    metaKeywords: item.metaKeywords || '',
  }
}

export function ManageContentPage({ contentType }: { contentType: ContentTypeValue }) {
  const toast = useToast()
  const qc = useQueryClient()
  const [form, setForm] = useState<FormState>(emptyForm)
  const [dirty, setDirty] = useState(false)
  const [saving, setSaving] = useState(false)

  const meta = CONTENT_TYPES.find((t) => t.value === contentType) ?? CONTENT_TYPES[0]

  const listQuery = useQuery({
    queryKey: ['manage-content'],
    queryFn: fetchManageContent,
  })

  const current = useMemo(() => {
    return (listQuery.data?.items ?? []).find((item) => item.contentType === contentType)
  }, [listQuery.data, contentType])

  useEffect(() => {
    setForm(toForm(current))
    setDirty(false)
  }, [contentType, current])

  const updateField = <K extends keyof FormState>(key: K, value: FormState[K]) => {
    setForm((f) => ({ ...f, [key]: value }))
    setDirty(true)
  }

  const save = async () => {
    if (!form.title.trim()) return toast.push('Title is required', 'error')
    setSaving(true)
    try {
      const res = await upsertManageContent({
        contentType,
        title: form.title.trim(),
        content: form.content,
        metaDescription: form.metaDescription,
        metaKeywords: form.metaKeywords,
      })
      toast.push(res.message || 'Content saved', 'success')
      setDirty(false)
      await qc.invalidateQueries({ queryKey: ['manage-content'] })
    } catch (e) {
      toast.push(e instanceof ApiError ? e.message : 'Save failed', 'error')
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="flex h-full min-h-0 min-w-0 max-w-full flex-col gap-3 overflow-hidden">
      <div className="flex shrink-0 flex-wrap items-end justify-between gap-3">
        <div>
          <h1 className="font-[family-name:var(--font-display)] flex items-center gap-2 text-3xl font-semibold tracking-tight">
            <FileText size={28} className="text-rose-600" />
            {meta.label}
          </h1>
          <p className="mt-1 text-sm text-muted-foreground">Content Management</p>
        </div>
        <div className="flex gap-2">
          <Button variant="outline" onClick={() => listQuery.refetch()} disabled={listQuery.isFetching}>
            <RefreshCw size={16} /> Refresh
          </Button>
          <Button onClick={() => void save()} disabled={saving || !dirty}>
            <Save size={16} />
            {saving ? 'Saving…' : 'Save Content'}
          </Button>
        </div>
      </div>

      <Card className="flex min-h-0 flex-1 flex-col overflow-hidden">
        <CardHeader className="shrink-0 border-b py-4">
          <div className="flex flex-wrap items-start justify-between gap-2">
            <div>
              <CardTitle className="text-lg">{meta.label}</CardTitle>
              <CardDescription>
                {current?.lastUpdatedDisplay
                  ? `Last updated ${current.lastUpdatedDisplay}${current.updatedBy ? ` by ${current.updatedBy}` : ''}`
                  : 'Never updated'}
                {dirty ? ' · Unsaved changes' : ''}
              </CardDescription>
            </div>
            <Badge variant="secondary">{meta.badge}</Badge>
          </div>
        </CardHeader>
        <CardContent className="sidebar-nav-scroll min-h-0 flex-1 space-y-4 overflow-y-auto pt-4">
          {listQuery.isLoading && <p className="text-sm text-muted-foreground">Loading…</p>}
          {listQuery.isError && (
            <p className="text-sm text-destructive">{(listQuery.error as Error).message}</p>
          )}
          {!listQuery.isLoading && !listQuery.isError && (
            <>
              <div className="space-y-1.5">
                <label className="text-xs font-medium text-muted-foreground">Title *</label>
                <Input
                  value={form.title}
                  onChange={(e) => updateField('title', e.target.value)}
                  placeholder="Page title"
                />
              </div>
              <div className="grid gap-4 md:grid-cols-2">
                <div className="space-y-1.5">
                  <label className="text-xs font-medium text-muted-foreground">Meta Description</label>
                  <Textarea
                    value={form.metaDescription}
                    onChange={(e) => updateField('metaDescription', e.target.value)}
                    rows={3}
                    placeholder="SEO meta description"
                  />
                </div>
                <div className="space-y-1.5">
                  <label className="text-xs font-medium text-muted-foreground">Meta Keywords</label>
                  <Textarea
                    value={form.metaKeywords}
                    onChange={(e) => updateField('metaKeywords', e.target.value)}
                    rows={3}
                    placeholder="keyword1, keyword2"
                  />
                </div>
              </div>
              <div className="space-y-1.5">
                <label className="text-xs font-medium text-muted-foreground">Content</label>
                <HtmlRichEditor
                  key={contentType}
                  value={form.content}
                  onChange={(content) => updateField('content', content)}
                />
              </div>
            </>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
