import { useEffect, useMemo, useRef, useState } from 'react'
import { useNavigate, useOutletContext, useSearchParams } from 'react-router-dom'
import { ApiError } from '@/shared/api/client'
import { clearCardInProcess, savePendingProfile, stepPath } from '@/features/website/cardContext'
import { pickVal, websiteApi } from '@/features/website/websiteApi'
import type { WebsiteOutletContext } from '@/features/website/WebsiteBuilderLayout'
import { siteUrl } from '@/lib/utils'
import { PortalPageHeader } from '@/components/layout/PortalPageHeader'
import { MwAlert, MwField, MwInput, MwStepActions } from '@/components/form/MwControls'

function urlFromName(name: string) {
  return name.trim().replace(/[^A-Za-z0-9\s-]/g, '').replace(/ /g, '-')
}

type UrlCheck = { state: 'idle' | 'checking' | 'available' | 'taken'; message: string; slug: string }

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
  const [msg, setMsg] = useState<string | null>(null)
  const [err, setErr] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)
  const [urlCheck, setUrlCheck] = useState<UrlCheck>({ state: 'idle', message: '', slug: '' })
  const urlEdited = useRef(false)
  const activeId = isNew ? null : cardId

  useEffect(() => {
    if (!activeId) {
      if (isNew) clearCardInProcess()
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
        urlEdited.current = true
      } catch (e) {
        setErr(e instanceof Error ? e.message : 'Failed to load')
      }
    })()
  }, [activeId, email, isNew])

  const previewSlug = urlCheck.slug || urlFromName(compName) || slug
  const businessUrl = useMemo(() => (previewSlug ? publicUrlForSlug(previewSlug) : ''), [previewSlug])
  const previousUrl = previousSlug ? publicUrlForSlug(previousSlug) : ''

  useEffect(() => {
    const name = compName.trim()
    if (!name) {
      setUrlCheck({ state: 'idle', message: '', slug: '' })
      return
    }
    let cancelled = false
    setUrlCheck({ state: 'checking', message: 'Checking availability…', slug: '' })
    const timer = window.setTimeout(() => {
      void (async () => {
        try {
          const check = await websiteApi.checkName(name, activeId, email)
          if (cancelled) return
          const raw = check as unknown as Record<string, unknown>
          const available = Boolean(pickVal(raw, 'available', 'Available'))
          const message = String(pickVal(raw, 'message', 'Message') ?? '')
          const nextSlug = String(pickVal(raw, 'slug', 'Slug') ?? '')
          setUrlCheck({
            state: available ? 'available' : 'taken',
            message: available ? 'This business URL is available.' : message || 'This business URL is not available.',
            slug: nextSlug,
          })
        } catch (e) {
          if (!cancelled) {
            setUrlCheck({
              state: 'taken',
              message: e instanceof Error ? e.message : 'Could not check this URL.',
              slug: '',
            })
          }
        }
      })()
    }, 400)
    return () => {
      cancelled = true
      window.clearTimeout(timer)
    }
  }, [compName, activeId, email])

  function onBusinessName(value: string) {
    setDisplayName(value)
    if (!urlEdited.current) setCompName(urlFromName(value))
  }

  function onBusinessUrl(value: string) {
    const next = value.replace(/[^A-Za-z0-9\s-]/g, '')
    setCompName(next)
    urlEdited.current = next !== urlFromName(displayName)
  }

  async function save(e: React.FormEvent) {
    e.preventDefault()
    setBusy(true)
    setErr(null)
    setMsg(null)
    try {
      if (compName.trim() && urlCheck.state === 'taken') {
        setErr(urlCheck.message || 'This business URL is not available.')
        return
      }
      if (activeId) {
        const res = await websiteApi.putBusiness(activeId, {
          compName,
          displayName,
          ownerEmail: email,
        })
        setMsg(res.message || 'Saved')
        navigate(stepPath('/website/select-theme', activeId))
      } else {
        const name = displayName.trim()
        const urlName = compName.trim()
        if (!name || !urlName) {
          setErr('Enter the business name and business URL.')
          return
        }
        if (urlCheck.state === 'taken') {
          setErr(urlCheck.message || 'This business URL is not available.')
          return
        }
        const check = await websiteApi.checkName(urlName, null, email)
        const available = Boolean(pickVal(check as unknown as Record<string, unknown>, 'available', 'Available'))
        if (!available) {
          const message = String(pickVal(check as unknown as Record<string, unknown>, 'message', 'Message') || 'This business URL is not available.')
          setUrlCheck({ state: 'taken', message, slug: String(pickVal(check as unknown as Record<string, unknown>, 'slug', 'Slug') ?? '') })
          setErr(message)
          return
        }
        savePendingProfile({ compName: urlName, displayName: name })
        navigate('/payment/db-profile?new=1')
      }
    } catch (ex) {
      setErr(ex instanceof ApiError ? ex.message : 'Save failed')
    } finally {
      setBusy(false)
    }
  }

  const nextHref = stepPath('/website/select-theme', activeId)

  return (
    <>
      <PortalPageHeader title="Business Name" subtitle="Website Builder" />

      {err ? <MwAlert tone="danger">{err}</MwAlert> : null}
      {msg ? <MwAlert tone="success">{msg}</MwAlert> : null}

      <div className="card mb-4 mw-card">
        <div className="card-body mw-card-body">
          <form
            className="business_name_form mw-form mw-business-name-form"
            data-card-number={activeId ?? ''}
            onSubmit={(e) => void save(e)}
          >
            <div className="mw-business-name-fields-row">
              <MwField label="Business Name" required htmlFor="d_display_name" large>
                <MwInput
                  type="text"
                  id="d_display_name"
                  name="d_display_name"
                  className="d_display_name mw-input-lg"
                  maxLength={199}
                  value={displayName}
                  onChange={(e) => onBusinessName(e.target.value)}
                  placeholder="Enter Business Name"
                  required
                />
              </MwField>
              <MwField label="Business URL" required htmlFor="d_comp_name" large>
                <MwInput
                  type="text"
                  id="d_comp_name"
                  name="d_comp_name"
                  className="d_comp_name mw-input-lg"
                  maxLength={199}
                  value={compName}
                  onChange={(e) => onBusinessUrl(e.target.value)}
                  placeholder="Business URL"
                  pattern="[A-Za-z0-9\s\-]+"
                  title="Only letters, numbers, spaces and hyphen (-) are allowed."
                  required
                />
                {urlCheck.state !== 'idle' ? <p className={`mw-url-status is-${urlCheck.state}`}>{urlCheck.message}</p> : null}
              </MwField>
            </div>

            {businessUrl || activeId ? (
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
                </div>
              </div>
            ) : null}

            <MwStepActions
              back="/dashboard"
              next={activeId ? nextHref : undefined}
              saveLabel={activeId ? 'Save' : 'Payment'}
              busyLabel={activeId ? 'Saving...' : 'Please wait...'}
              busy={busy}
              disabled={urlCheck.state === 'taken' || urlCheck.state === 'checking'}
            />
          </form>
        </div>
      </div>
    </>
  )
}
