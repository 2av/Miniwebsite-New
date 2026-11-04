import { useEffect, useRef, useState } from 'react'
import { Link, useOutletContext } from 'react-router-dom'
import { ApiError } from '@/shared/api/client'
import { stepPath } from '@/features/website/cardContext'
import { pickVal, websiteApi } from '@/features/website/websiteApi'
import type { WebsiteOutletContext } from '@/features/website/WebsiteBuilderLayout'
import { PortalPageHeader } from '@/components/layout/PortalPageHeader'

const FIELDS = [
  ['firstName', 'First Name'],
  ['lastName', 'Last Name'],
  ['position', 'Position'],
  ['contact', 'Contact'],
  ['contact2', 'Contact 2'],
  ['whatsapp', 'WhatsApp'],
  ['email', 'Email'],
  ['website', 'Website'],
  ['gstNumber', 'GST Number'],
  ['address', 'Address'],
  ['address2', 'Address 2'],
  ['city', 'City'],
  ['state', 'State'],
  ['pincode', 'Pincode'],
  ['country', 'Country'],
  ['location', 'Map Location URL'],
  ['compEstDate', 'Establishment Date'],
] as const

export function CompanyDetailsPage() {
  const { cardId, email } = useOutletContext<WebsiteOutletContext>()
  const [form, setForm] = useState<Record<string, string>>({})
  const [aboutUs, setAboutUs] = useState('')
  const [businessHours, setBusinessHours] = useState('')
  const [logoUrl, setLogoUrl] = useState('')
  const [heroUrl, setHeroUrl] = useState('')
  const [uploading, setUploading] = useState<'logo' | 'hero' | null>(null)
  const [uploadPercent, setUploadPercent] = useState(0)
  const [removing, setRemoving] = useState<'logo' | 'hero' | null>(null)
  const [imageError, setImageError] = useState<Partial<Record<'logo' | 'hero', string>>>({})
  const [msg, setMsg] = useState<string | null>(null)
  const [err, setErr] = useState<string | null>(null)
  const logoRef = useRef<HTMLInputElement>(null)
  const heroRef = useRef<HTMLInputElement>(null)

  useEffect(() => {
    if (!cardId) return
    ;(async () => {
      try {
        const raw = await websiteApi.getCompany(cardId, email)
        const next: Record<string, string> = {}
        for (const [key] of FIELDS) {
          const pascal = key.charAt(0).toUpperCase() + key.slice(1)
          next[key] = String(pickVal(raw, key, pascal) ?? '')
        }
        setForm(next)
        setAboutUs(String(pickVal(raw, 'aboutUs', 'AboutUs') ?? ''))
        setBusinessHours(String(pickVal(raw, 'businessHours', 'BusinessHours') ?? ''))
        applyImages(raw)
      } catch (e) {
        setErr(e instanceof Error ? e.message : 'Failed to load')
      }
    })()
  }, [cardId, email])

  async function save(e: React.FormEvent) {
    e.preventDefault()
    if (!cardId) return
    setErr(null)
    try {
      const res = await websiteApi.putCompany(cardId, email, {
        ...form,
        aboutUs,
        businessHours,
      })
      setMsg(res.message || 'Saved')
    } catch (ex) {
      setErr(ex instanceof ApiError ? ex.message : 'Save failed')
    }
  }

  function applyImages(raw: Record<string, unknown>) {
    const logo = String(pickVal(raw, 'logoUrl', 'LogoUrl') ?? '')
    const hero = String(pickVal(raw, 'heroImageUrl', 'HeroImageUrl') ?? '')
    setLogoUrl(logo ? `${logo}${logo.includes('?') ? '&' : '?'}t=${Date.now()}` : '')
    setHeroUrl(hero ? `${hero}${hero.includes('?') ? '&' : '?'}t=${Date.now()}` : '')
  }

  async function onPick(kind: 'logo' | 'hero', file: File | null) {
    if (!file || !cardId) return
    const allowed = ['image/jpeg', 'image/jpg', 'image/png', 'image/gif', 'image/webp']
    const extOk = /\.(jpe?g|png|gif|webp)$/i.test(file.name)
    if (!allowed.includes(file.type) && !(file.type === '' && extOk) && !file.type.startsWith('image/')) {
      setImageError((prev) => ({ ...prev, [kind]: 'Use a JPG, PNG, GIF, or WebP image.' }))
      return
    }
    if (file.size > 2_500_000) {
      setImageError((prev) => ({ ...prev, [kind]: 'Image is too large (max 2.5MB).' }))
      return
    }
    setImageError((prev) => {
      const next = { ...prev }
      delete next[kind]
      return next
    })
    setUploading(kind)
    setUploadPercent(0)
    setErr(null)
    try {
      const res = await websiteApi.uploadCompanyImage(cardId, email, kind, file, setUploadPercent)
      setUploadPercent(100)
      if (res.data) applyImages(res.data)
      setMsg('Image uploaded.')
    } catch (ex) {
      setImageError((prev) => ({ ...prev, [kind]: ex instanceof ApiError ? ex.message : 'Upload failed' }))
    } finally {
      setUploading(null)
    }
  }

  async function removeImage(kind: 'logo' | 'hero') {
    if (!cardId || removing) return
    setRemoving(kind)
    setImageError((prev) => {
      const next = { ...prev }
      delete next[kind]
      return next
    })
    try {
      const res = await websiteApi.deleteCompanyImage(cardId, email, kind)
      if (res.data) applyImages(res.data)
    } catch (ex) {
      setImageError((prev) => ({ ...prev, [kind]: ex instanceof ApiError ? ex.message : 'Could not remove the image.' }))
    } finally {
      setRemoving(null)
    }
  }

  function ImageBox({
    kind,
    title,
    src,
    inputRef,
  }: {
    kind: 'logo' | 'hero'
    title: string
    src: string
    inputRef: React.RefObject<HTMLInputElement | null>
  }) {
    const note = imageError[kind]
    const busy = uploading === kind || removing === kind
    return (
      <div className="col-md-6">
        <p className="addlogo">{title}</p>
        <label className={`company-image-box${busy ? ' is-busy' : ''}`}>
          {uploading === kind ? (
            <div className="company-image-progress">
              <span>Uploading {uploadPercent}%</span>
              <div className="company-image-track" aria-hidden>
                <div className="company-image-fill" style={{ width: `${uploadPercent}%` }} />
              </div>
            </div>
          ) : src ? (
            <img src={src} alt={title} />
          ) : note ? (
            <p className="company-image-note">{note}</p>
          ) : (
            <span>Choose image</span>
          )}
          {src && note && uploading !== kind ? <p className="company-image-note company-image-note-overlay">{note}</p> : null}
          {src && !busy ? (
            <button
              type="button"
              className="company-image-remove"
              aria-label={`Remove ${title}`}
              onClick={(e) => {
                e.preventDefault()
                e.stopPropagation()
                void removeImage(kind)
              }}
            >
              <i className="fa fa-times" aria-hidden />
            </button>
          ) : null}
          <input
            ref={inputRef}
            type="file"
            accept=".jpg,.jpeg,.png,.gif,.webp,image/*"
            className="company-image-input"
            disabled={busy}
            onChange={(e) => {
              const picked = e.target.files?.[0] ?? null
              e.target.value = ''
              void onPick(kind, picked)
            }}
          />
        </label>
      </div>
    )
  }

  return (
    <>
      <PortalPageHeader title="Company Details" />
      <div className="card">
      <div className="card-body">
        {err && <div className="alert alert-danger">{err}</div>}
        {msg && <div className="alert alert-success">{msg}</div>}
        <form onSubmit={save} className="row g-3">
          {FIELDS.map(([key, label]) => (
            <div className="col-md-6" key={key}>
              <label className="form-label">{label}</label>
              <input
                className="form-control"
                value={form[key] || ''}
                onChange={(e) => setForm((f) => ({ ...f, [key]: e.target.value }))}
              />
            </div>
          ))}
          <div className="col-12">
            <label className="form-label">About Us</label>
            <textarea className="form-control" rows={4} value={aboutUs} onChange={(e) => setAboutUs(e.target.value)} />
          </div>
          <div className="col-12">
            <label className="form-label">Business Hours (JSON or text)</label>
            <textarea className="form-control" rows={2} value={businessHours} onChange={(e) => setBusinessHours(e.target.value)} />
          </div>
          <ImageBox kind="logo" title="Logo" src={logoUrl} inputRef={logoRef} />
          <ImageBox kind="hero" title="Hero image" src={heroUrl} inputRef={heroRef} />
          <div className="col-12 d-flex gap-2">
            <Link className="btn btn-outline-secondary" to={stepPath('/website/select-theme', cardId)}>
              Back
            </Link>
            <button type="submit" className="btn btn-primary">
              Save
            </button>
            <Link className="btn btn-outline-primary" to={stepPath('/website/services', cardId)}>
              Next
            </Link>
          </div>
        </form>
      </div>
    </div>
      <style>{`
        .company-image-box {
          position: relative;
          width: 200px;
          height: 200px;
          margin: 0;
          border: 2px solid darkgray;
          overflow: hidden;
          display: flex;
          align-items: center;
          justify-content: center;
          cursor: pointer;
          background: #fff;
        }
        .company-image-box.is-busy { cursor: default; }
        .addlogo {
          font-size: 18px;
          margin-bottom: 10px;
          margin-top: 0;
        }
        .company-image-box span {
          font-size: 16px;
          font-weight: 500;
          color: #666;
          text-align: center;
        }
        .company-image-box img {
          max-width: 100%;
          max-height: 100%;
          object-fit: contain;
        }
        .company-image-note {
          margin: 0;
          padding: 0 10px;
          font-size: 11px;
          line-height: 1.35;
          text-align: center;
          color: #b42318;
        }
        .company-image-note-overlay {
          position: absolute;
          left: 6px;
          right: 6px;
          bottom: 6px;
          padding: 4px 6px;
          background: rgba(255, 255, 255, 0.94);
          border-radius: 4px;
        }
        .company-image-remove {
          position: absolute;
          top: 6px;
          right: 6px;
          z-index: 2;
          width: 22px;
          height: 22px;
          padding: 0;
          border: 0;
          border-radius: 50%;
          background: rgba(15, 23, 42, 0.72);
          color: #fff;
          display: flex;
          align-items: center;
          justify-content: center;
          cursor: pointer;
          font-size: 12px;
          line-height: 1;
        }
        .company-image-remove:hover { background: #dc3545; }
        .company-image-progress {
          display: flex;
          flex-direction: column;
          align-items: center;
          gap: 6px;
          width: 100%;
          padding: 12px;
        }
        .company-image-progress span {
          font-size: 12px;
          font-weight: 600;
          color: #334155;
        }
        .company-image-track {
          width: 80%;
          height: 4px;
          border-radius: 999px;
          background: #e2e8f0;
          overflow: hidden;
        }
        .company-image-fill {
          height: 100%;
          background: #007bff;
        }
        .company-image-input {
          position: absolute !important;
          width: 1px !important;
          height: 1px !important;
          padding: 0 !important;
          margin: -1px !important;
          overflow: hidden !important;
          clip: rect(0, 0, 0, 0) !important;
          white-space: nowrap !important;
          border: 0 !important;
          opacity: 0 !important;
          appearance: none !important;
        }
      `}</style>
    </>
  )
}
