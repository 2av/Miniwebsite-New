import { useEffect, useState } from 'react'
import { Link, useOutletContext } from 'react-router-dom'
import { ApiError } from '@/shared/api/client'
import { stepPath } from '@/features/website/cardContext'
import { pickVal, websiteApi } from '@/features/website/websiteApi'
import type { WebsiteOutletContext } from '@/features/website/WebsiteBuilderLayout'

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
  const [logoLocation, setLogoLocation] = useState('')
  const [heroLocation, setHeroLocation] = useState('')
  const [msg, setMsg] = useState<string | null>(null)
  const [err, setErr] = useState<string | null>(null)

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
        setLogoLocation(String(pickVal(raw, 'logoLocation', 'LogoLocation') ?? ''))
        setHeroLocation(String(pickVal(raw, 'heroImageLocation', 'HeroImageLocation') ?? ''))
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
        logoLocation: logoLocation || null,
        heroImageLocation: heroLocation || null,
      })
      setMsg(res.message || 'Saved')
    } catch (ex) {
      setErr(ex instanceof ApiError ? ex.message : 'Save failed')
    }
  }

  return (
    <div className="card">
      <div className="card-body">
        <h3 className="mb-3">Company Details</h3>
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
          <div className="col-md-6">
            <label className="form-label">Logo filename</label>
            <input className="form-control" value={logoLocation} onChange={(e) => setLogoLocation(e.target.value)} placeholder="uploaded filename" />
          </div>
          <div className="col-md-6">
            <label className="form-label">Hero image filename</label>
            <input className="form-control" value={heroLocation} onChange={(e) => setHeroLocation(e.target.value)} placeholder="uploaded filename" />
          </div>
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
        <p className="small text-muted mt-3">Image binary upload still uses the live site upload flow; save the returned filename here.</p>
      </div>
    </div>
  )
}
