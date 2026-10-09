import { useEffect, useMemo, useState } from 'react'
import { Link, useNavigate, useOutletContext, useSearchParams } from 'react-router-dom'
import { ApiError } from '@/shared/api/client'
import { setCardInProcess, stepPath } from '@/features/website/cardContext'
import { pickVal, websiteApi } from '@/features/website/websiteApi'
import type { WebsiteOutletContext } from '@/features/website/WebsiteBuilderLayout'
import { assetUrl, siteUrl } from '@/lib/utils'

function publicUrlForSlug(slug: string) {
  const clean = slug.replace(/^\//, '')
  // PHP uses host root: http://localhost/cd1 — siteUrl points at miniwebsite base
  const origin = (import.meta.env.VITE_SITE_URL || window.location.origin).replace(/\/$/, '')
  // Public cards live at origin/{slug} via .htaccess / n.php — mirror PHP host URL
  try {
    const u = new URL(origin)
    return `${u.protocol}//${u.host}/${encodeURIComponent(clean)}`
  } catch {
    return siteUrl(clean)
  }
}

export function BusinessNamePage() {
  const { cardId, email } = useOutletContext<WebsiteOutletContext>()
  const [params] = useSearchParams()
  const isNew = params.get('new') === '1'
  const navigate = useNavigate()
  const [compName, setCompName] = useState('')
  const [displayName, setDisplayName] = useState('')
  const [slug, setSlug] = useState('')
  const [previousSlug, setPreviousSlug] = useState('')
  const [nameChangeCount, setNameChangeCount] = useState(0)
  const [msg, setMsg] = useState<string | null>(null)
  const [err, setErr] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)
  const activeId = isNew ? null : cardId

  useEffect(() => {
    if (!activeId) {
      // New MW: default from session name is handled lightly
      return
    }
    ;(async () => {
      try {
        const raw = await websiteApi.getBusiness(activeId, email)
        const dn = String(pickVal(raw, 'displayName', 'DisplayName') ?? '')
        const cn = String(pickVal(raw, 'compName', 'CompName') ?? '')
        setCompName(cn)
        setDisplayName(dn || cn)
        setSlug(String(pickVal(raw, 'slug', 'Slug') ?? ''))
        setPreviousSlug(String(pickVal(raw, 'previousSlug', 'PreviousSlug') ?? ''))
        setNameChangeCount(Number(pickVal(raw, 'nameChangeCount', 'NameChangeCount') ?? 0))
      } catch (e) {
        setErr(e instanceof Error ? e.message : 'Failed to load')
      }
    })()
  }, [activeId, email])

  const remaining = Math.max(0, 2 - nameChangeCount)
  const businessUrl = useMemo(() => {
    const s = slug || compName.trim().toLowerCase().replace(/[^a-z0-9\-]+/g, '-')
    return s ? publicUrlForSlug(s) : ''
  }, [slug, compName])
  const previousUrl = previousSlug ? publicUrlForSlug(previousSlug) : ''

  async function save(e: React.FormEvent) {
    e.preventDefault()
    setBusy(true)
    setErr(null)
    setMsg(null)
    try {
      if (activeId) {
        const res = await websiteApi.putBusiness(activeId, {
          compName,
          displayName,
          ownerEmail: email,
        })
        setMsg(res.message || 'Saved')
        navigate(stepPath('/website/select-theme', activeId))
      } else {
        const res = await websiteApi.createBusiness({
          compName,
          displayName,
          ownerEmail: email,
        })
        const data = (res.data ?? {}) as Record<string, unknown>
        const id = Number(pickVal(data, 'id', 'Id') ?? 0)
        if (id > 0) {
          setCardInProcess(id)
          setMsg(res.message || 'Created')
          navigate(stepPath('/website/select-theme', id))
        } else {
          setErr('Created but no card id returned')
        }
      }
    } catch (ex) {
      setErr(ex instanceof ApiError ? ex.message : 'Save failed')
    } finally {
      setBusy(false)
    }
  }

  const saveIcon = assetUrl('images/Save.png')
  const nextHref = stepPath('/website/select-theme', activeId)

  return (
    <>
      <div className="main-top mw-page-header">
        <h1 className="heading mw-page-title">Business Name</h1>
        <nav aria-label="breadcrumb">
          <ol className="breadcrumb mw-breadcrumb">
            <li className="breadcrumb-item mw-breadcrumb-item">
              <a href="#">ShareBizz</a>
            </li>
            <li className="breadcrumb-item mw-breadcrumb-item active" aria-current="page">
              Website Builder
            </li>
          </ol>
        </nav>
      </div>

      {err && (
        <div className="alert alert-danger mw-alert mw-alert-danger" role="alert">
          <i className="fa fa-exclamation-circle mw-alert-icon" aria-hidden />
          <div className="mw-alert-body">{err}</div>
        </div>
      )}
      {msg && (
        <div className="alert alert-success mw-alert mw-alert-success" role="alert">
          <i className="fa fa-check-circle mw-alert-icon" aria-hidden />
          <div className="mw-alert-body">{msg}</div>
        </div>
      )}

      <div className="card mb-4 mw-card">
        <div className="card-body mw-card-body">
          <form
            className="business_name_form mw-form mw-business-name-form"
            data-card-number={activeId ?? ''}
            onSubmit={(e) => void save(e)}
          >
            <div className="mw-business-name-fields-row">
              <div className="form-group top_header_section mw-form-group">
                <label className="mw-label mw-label-lg" htmlFor="d_display_name">
                  Business Name<span className="req">*</span>
                </label>
                <input
                  type="text"
                  id="d_display_name"
                  name="d_display_name"
                  className="form-control d_display_name mw-input mw-input-lg"
                  maxLength={199}
                  value={displayName}
                  onChange={(e) => setDisplayName(e.target.value)}
                  placeholder="Enter Business Name"
                  required
                />
              </div>
              <div className="form-group top_header_section mw-form-group">
                <label className="mw-label mw-label-lg" htmlFor="d_comp_name">
                  Business URL<span className="req">*</span>
                </label>
                <input
                  type="text"
                  id="d_comp_name"
                  name="d_comp_name"
                  className="form-control d_comp_name mw-input mw-input-lg"
                  maxLength={199}
                  value={compName}
                  onChange={(e) => setCompName(e.target.value)}
                  placeholder="Enter Your Business URL"
                  pattern="[A-Za-z0-9\s\-]+"
                  title="Only letters, numbers, spaces and hyphen (-) are allowed."
                  required
                />
              </div>
            </div>

            {activeId ? (
              <div className="mw-business-name-url-extra">
                <div className="mw-meta">
                  {businessUrl ? (
                    <p className="mw-meta-line">
                      <i className="fa fa-link" aria-hidden />
                      <span className="mw-meta-label">URL:</span>
                      <a className="mw-meta-link" href={businessUrl} target="_blank" rel="noopener noreferrer">
                        {businessUrl}
                      </a>
                    </p>
                  ) : null}
                  {previousUrl ? (
                    <p className="mw-meta-line">
                      <i className="fa fa-history" aria-hidden />
                      <span className="mw-meta-label">Previous URL (reference):</span>
                      <a
                        className="mw-meta-link mw-meta-link--muted"
                        href={previousUrl}
                        target="_blank"
                        rel="noopener noreferrer"
                      >
                        {previousUrl}
                      </a>
                    </p>
                  ) : null}
                  <p className={`mw-meta-line ${remaining > 0 ? 'is-warning' : 'is-danger'}`}>
                    <i className={`fa ${remaining > 0 ? 'fa-info-circle' : 'fa-lock'}`} aria-hidden />
                    {remaining > 0 ? (
                      <span>
                        You can change your business URL <strong>{remaining}</strong> more time
                        {remaining > 1 ? 's' : ''}.
                      </span>
                    ) : (
                      <span>You have reached the maximum of 2 business name changes.</span>
                    )}
                  </p>
                </div>
              </div>
            ) : null}

            <div className="Product-ServicesBtn mw-btn-row d-flex flex-wrap justify-content-between align-items-center gap-2 mt-4">
              <Link to="/dashboard" className="mw-btn mw-btn-back">
                <span className="mw-btn-angle angle">
                  <i className="fa fa-angle-left" aria-hidden />
                </span>
                <span>Back</span>
              </Link>
              <button type="submit" className="mw-btn mw-btn-save save_btn" disabled={busy}>
                <img src={saveIcon} alt="" style={{ width: 18, height: 18, marginRight: 6 }} />
                <span>{busy ? 'Saving...' : 'Save'}</span>
              </button>
              {activeId ? (
                <Link to={nextHref} className="mw-btn mw-btn-next">
                  <span>Next</span>
                  <span className="mw-btn-angle angle">
                    <i className="fa fa-angle-right" aria-hidden />
                  </span>
                </Link>
              ) : (
                <span style={{ width: 1 }} aria-hidden />
              )}
            </div>
          </form>
        </div>
      </div>
    </>
  )
}
