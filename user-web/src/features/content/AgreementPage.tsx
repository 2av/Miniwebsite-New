import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { apiGet } from '@/shared/api/client'
import { siteUrl } from '@/lib/utils'
import { useSiteStyles } from '@/shared/site/useSiteStyles'

type Props = {
  contentType: string
  /** Optional deep-link to legacy payment flow until payments are migrated. */
  paymentPath?: string
  agreeLabel?: string
}

/** Agreement-style CMS pages (may include payment CTA linking to live site for now). */
export function AgreementPage({ contentType, paymentPath, agreeLabel = 'Agree & Continue' }: Props) {
  useSiteStyles()
  const [html, setHtml] = useState('')
  const [title, setTitle] = useState('Agreement')
  const [updated, setUpdated] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false
    ;(async () => {
      try {
        const raw = await apiGet<Record<string, unknown>>(`/api/v1/public/content/${encodeURIComponent(contentType)}`)
        if (cancelled) return
        setTitle(String(raw.title ?? raw.Title ?? 'Agreement'))
        setHtml(String(raw.contentHtml ?? raw.ContentHtml ?? raw.content ?? raw.Content ?? ''))
        setUpdated((raw.lastUpdatedDisplay ?? raw.LastUpdatedDisplay) as string | null)
        document.title = `${String(raw.title ?? raw.Title ?? 'Agreement')} - MiniWebsite`
      } catch (e) {
        if (!cancelled) setError(e instanceof Error ? e.message : 'Failed to load')
      }
    })()
    return () => {
      cancelled = true
    }
  }, [contentType])

  if (error) return <div className="container py-5">{error}</div>
  if (!html && !error) return <div className="container py-5">Loading...</div>

  return (
    <div className="container" style={{ padding: 20, margin: 'auto', fontFamily: 'Arial, sans-serif', lineHeight: 1.4 }}>
      <h1 style={{ color: '#002169', marginBottom: 12 }}>{title}</h1>
      {updated && (
        <div style={{ fontSize: '0.9em', color: '#6c757d', fontStyle: 'italic', marginBottom: 20 }}>
          Last updated: {updated}
        </div>
      )}
      <div className="content" dangerouslySetInnerHTML={{ __html: html }} />
      {paymentPath && (
        <div style={{ marginTop: 32, textAlign: 'center' }}>
          <a className="btn btn-primary" href={siteUrl(paymentPath)} style={{ padding: '10px 24px' }}>
            {agreeLabel}
          </a>
          <div style={{ marginTop: 12 }}>
            <Link to="/">Back to Home</Link>
          </div>
        </div>
      )}
    </div>
  )
}
