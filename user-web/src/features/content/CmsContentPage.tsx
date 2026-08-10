import { useEffect, useState } from 'react'
import { apiGet } from '@/shared/api/client'
import { useSiteStyles } from '@/shared/site/useSiteStyles'

type PublicContent = {
  contentType: string
  title: string
  contentHtml: string
  metaDescription?: string | null
  lastUpdatedDisplay?: string | null
}

function normalize(raw: unknown): PublicContent {
  const o = (raw ?? {}) as Record<string, unknown>
  return {
    contentType: String(o.contentType ?? o.ContentType ?? ''),
    title: String(o.title ?? o.Title ?? 'Content'),
    contentHtml: String(o.contentHtml ?? o.ContentHtml ?? o.content ?? o.Content ?? ''),
    metaDescription: (o.metaDescription ?? o.MetaDescription) as string | null,
    lastUpdatedDisplay: (o.lastUpdatedDisplay ?? o.LastUpdatedDisplay) as string | null,
  }
}

/** CMS legal/content pages — matches thin PHP CMS readers. */
export function CmsContentPage({ contentType }: { contentType: string }) {
  useSiteStyles()
  const [data, setData] = useState<PublicContent | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false
    ;(async () => {
      try {
        const raw = await apiGet<unknown>(`/api/v1/public/content/${encodeURIComponent(contentType)}`)
        if (!cancelled) {
          const c = normalize(raw)
          setData(c)
          document.title = `${c.title} - MiniWebsite`
        }
      } catch (e) {
        if (!cancelled) setError(e instanceof Error ? e.message : 'Failed to load content')
      }
    })()
    return () => {
      cancelled = true
    }
  }, [contentType])

  if (error) {
    return (
      <div className="container" style={{ padding: 20, margin: 'auto', fontFamily: 'Arial, sans-serif' }}>
        <p>{error}</p>
      </div>
    )
  }

  if (!data) {
    return (
      <div className="container" style={{ padding: 20, margin: 'auto', fontFamily: 'Arial, sans-serif' }}>
        <p>Loading...</p>
      </div>
    )
  }

  return (
    <div className="container" style={{ padding: 20, margin: 'auto', fontFamily: 'Arial, sans-serif', lineHeight: 1.4 }}>
      {data.lastUpdatedDisplay && (
        <div style={{ fontSize: '0.9em', color: '#6c757d', fontStyle: 'italic', marginBottom: 20 }}>
          Last updated: {data.lastUpdatedDisplay}
        </div>
      )}
      <div className="content" dangerouslySetInnerHTML={{ __html: data.contentHtml }} />
    </div>
  )
}
