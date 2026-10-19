import { useEffect, useState } from 'react'
import { Link, useOutletContext } from 'react-router-dom'
import { ApiError } from '@/shared/api/client'
import { stepPath } from '@/features/website/cardContext'
import { pickVal, websiteApi } from '@/features/website/websiteApi'
import type { WebsiteOutletContext } from '@/features/website/WebsiteBuilderLayout'
import { PortalPageHeader } from '@/components/layout/PortalPageHeader'

type ThemeItem = { number: number; name: string; cssValue: string; previewImageUrl?: string | null }

export function SelectThemePage() {
  const { cardId, email } = useOutletContext<WebsiteOutletContext>()
  const [themes, setThemes] = useState<ThemeItem[]>([])
  const [selected, setSelected] = useState('')
  const [msg, setMsg] = useState<string | null>(null)
  const [err, setErr] = useState<string | null>(null)

  useEffect(() => {
    if (!cardId) return
    ;(async () => {
      try {
        const [catalog, current] = await Promise.all([
          websiteApi.listThemes(),
          websiteApi.getTheme(cardId, email),
        ])
        const list = Array.isArray(catalog) ? catalog : []
        setThemes(
          list.map((t) => {
            const o = (t ?? {}) as Record<string, unknown>
            return {
              number: Number(o.number ?? o.Number ?? 0),
              name: String(o.name ?? o.Name ?? 'Theme'),
              cssValue: String(o.cssValue ?? o.CssValue ?? ''),
              previewImageUrl: (o.previewImageUrl ?? o.PreviewImageUrl) as string | null,
            }
          }),
        )
        setSelected(String(pickVal(current, 'dCss', 'DCss') ?? ''))
      } catch (e) {
        setErr(e instanceof Error ? e.message : 'Failed to load themes')
      }
    })()
  }, [cardId, email])

  async function save() {
    if (!cardId || !selected) return
    setErr(null)
    try {
      const res = await websiteApi.putTheme(cardId, email, selected)
      setMsg(res.message || 'Theme saved')
    } catch (e) {
      setErr(e instanceof ApiError ? e.message : 'Save failed')
    }
  }

  return (
    <>
      <PortalPageHeader title="Select Theme" />
      <div className="card">
      <div className="card-body">
        {err && <div className="alert alert-danger">{err}</div>}
        {msg && <div className="alert alert-success">{msg}</div>}
        <div className="row">
          {themes.map((t) => (
            <div className="col-6 col-md-3 mb-3" key={t.number}>
              <button
                type="button"
                className={`card w-100 p-2 text-start ${selected === t.cssValue ? 'border-primary border-2' : ''}`}
                onClick={() => setSelected(t.cssValue)}
              >
                {t.previewImageUrl ? (
                  <img src={t.previewImageUrl} alt={t.name} className="img-fluid mb-2" style={{ minHeight: 80, objectFit: 'cover' }} />
                ) : (
                  <div className="bg-light mb-2 d-flex align-items-center justify-content-center" style={{ height: 80 }}>
                    {t.number}
                  </div>
                )}
                <div className="small fw-semibold">{t.name}</div>
              </button>
            </div>
          ))}
        </div>
        <div className="d-flex gap-2 mt-2">
          <Link className="btn btn-outline-secondary" to={stepPath('/website/business-name', cardId)}>
            Back
          </Link>
          <button type="button" className="btn btn-primary" onClick={() => void save()} disabled={!selected}>
            Save Theme
          </button>
          <Link className="btn btn-outline-primary" to={stepPath('/website/company-details', cardId)}>
            Next
          </Link>
        </div>
      </div>
    </div>
    </>
  )
}
