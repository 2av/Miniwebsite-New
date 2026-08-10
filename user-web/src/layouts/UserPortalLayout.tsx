import { useEffect, useMemo, useRef, useState } from 'react'
import { Link, NavLink, Navigate, Outlet, useLocation, useSearchParams } from 'react-router-dom'
import { apiGet, apiUploadForm } from '@/shared/api/client'
import { clearAuthSession, loginPathForRole, isSessionValid, redirectToLogin } from '@/shared/auth/session'
import { useAuthSession } from '@/shared/auth/useAuthSession'
import { assetUrl, mediaUrl } from '@/lib/utils'
import { getCardInProcess, setCardInProcess } from '@/features/website/cardContext'
import {
  getPortalMenu,
  getWebsiteBuilderMenu,
  type PortalFlags,
  type WebsiteNavItem,
} from '@/shared/portal/menu'
import { usePortalStyles } from '@/shared/site/usePortalStyles'
import { ProfileImageCropModal } from '@/features/portal/ProfileImageCropModal'

function pick(o: Record<string, unknown>, camel: string, pascal: string) {
  return o[camel] ?? o[pascal]
}

const DEFAULT_PROFILE_IMG = () => assetUrl('images/profile-default.png')

function resolveProfileSrc(pathOrUrl?: string | null) {
  if (!pathOrUrl) return DEFAULT_PROFILE_IMG()
  const v = String(pathOrUrl).trim()
  if (!v || v === 'null' || v === 'undefined') return DEFAULT_PROFILE_IMG()
  if (/^https?:\/\//i.test(v)) return v
  // DB stores "assets/upload/profile_image/…"; served from media host (CDN / live site)
  return mediaUrl(v.replace(/^\//, ''))
}

function navIconClass(icon: string) {
  const raw = icon.replace(/^fa-/, '')
  const aliases: Record<string, string> = {
    'pencil-square-o': 'edit',
    'picture-o': 'image',
    'clock-o': 'clock',
    money: 'credit-card',
    'line-chart': 'chart-line',
    'bar-chart': 'chart-bar',
    'check-square-o': 'square-check',
    'share-alt': 'share-nodes',
    tachometer: 'tachometer',
    'paint-brush': 'paint-brush',
    cogs: 'cogs',
    'video-camera': 'video-camera',
    'shopping-cart': 'shopping-cart',
    'external-link': 'external-link',
  }
  return aliases[raw] || raw
}

export function UserPortalLayout() {
  usePortalStyles()
  const session = useAuthSession()
  const location = useLocation()
  const [searchParams] = useSearchParams()
  const profileRef = useRef<HTMLLIElement>(null)
  const fileInputRef = useRef<HTMLInputElement>(null)
  const [profileOpen, setProfileOpen] = useState(false)
  const [mwId, setMwId] = useState<number | null>(null)
  const [builderSlug, setBuilderSlug] = useState<string | null>(null)
  const [profileImageUrl, setProfileImageUrl] = useState<string | null>(null)
  const [profileUploadMsg, setProfileUploadMsg] = useState<string | null>(null)
  const [profileUploading, setProfileUploading] = useState(false)
  const [cropFile, setCropFile] = useState<File | null>(null)
  const [cropOpen, setCropOpen] = useState(false)
  const [flags, setFlags] = useState<PortalFlags>({
    collaborationEnabled: Boolean(session?.collaborationEnabled),
    saleskitEnabled: Boolean(session?.saleskitEnabled),
    influencer: false,
    mwPaid: false,
  })
  const [franchiseInvoiceAvailable, setFranchiseInvoiceAvailable] = useState(false)

  const inWebsiteBuilder = location.pathname.startsWith('/website')

  const builderCardId = useMemo(() => {
    const fromUrl = Number(searchParams.get('card_number') || searchParams.get('cardNumber') || 0)
    if (fromUrl > 0) return fromUrl
    return getCardInProcess()
  }, [searchParams, location.pathname])

  useEffect(() => {
    if (builderCardId && builderCardId > 0) setCardInProcess(builderCardId)
  }, [builderCardId])

  useEffect(() => {
    if (!session?.email) return
    let cancelled = false
    ;(async () => {
      try {
        const raw = await apiGet<Record<string, unknown>>('/api/v1/portal/me', { email: session.email })
        if (cancelled) return
        setFlags({
          collaborationEnabled:
            String(pick(raw, 'collaborationEnabled', 'CollaborationEnabled') ?? 'NO').toUpperCase() === 'YES',
          saleskitEnabled:
            String(pick(raw, 'saleskitEnabled', 'SaleskitEnabled') ?? 'NO').toUpperCase() === 'YES',
          influencer: String(pick(raw, 'influencer', 'Influencer') ?? 'NO').toUpperCase() === 'YES',
          mwPaid: Boolean(pick(raw, 'mwPaid', 'MwPaid')),
        })
        setFranchiseInvoiceAvailable(
          Boolean(pick(raw, 'franchiseInvoiceAvailable', 'FranchiseInvoiceAvailable')),
        )
        const img = String(pick(raw, 'profileImageUrl', 'ProfileImageUrl') ?? '')
        if (img) setProfileImageUrl(img)
      } catch {
        // keep session flags
      }
      try {
        const cards = await apiGet<unknown[]>('/api/v1/digi-cards', { userEmail: session.email })
        if (cancelled || !Array.isArray(cards) || cards.length === 0) return
        const ids = cards.map((c) => Number(pick((c ?? {}) as Record<string, unknown>, 'id', 'Id') ?? 0))
        const maxId = Math.max(...ids.filter((n) => n > 0))
        if (maxId > 0) setMwId(maxId)
      } catch {
        // optional MW ID chip
      }
    })()
    return () => {
      cancelled = true
    }
  }, [session?.email])

  // When editing a card, show that card's MW ID + resolve slug for Preview
  useEffect(() => {
    if (!session?.email || !inWebsiteBuilder || !builderCardId) {
      setBuilderSlug(null)
      return
    }
    let cancelled = false
    setMwId(builderCardId)
    ;(async () => {
      try {
        const raw = await apiGet<Record<string, unknown>>(`/api/v1/digi-cards/${builderCardId}/business-name`, {
          ownerEmail: session.email,
        })
        if (cancelled) return
        const slug = String(pick(raw, 'slug', 'Slug') ?? '')
        setBuilderSlug(slug || null)
      } catch {
        if (!cancelled) setBuilderSlug(null)
      }
    })()
    return () => {
      cancelled = true
    }
  }, [session?.email, inWebsiteBuilder, builderCardId])

  useEffect(() => {
    const onDoc = (e: MouseEvent) => {
      if (profileRef.current && !profileRef.current.contains(e.target as Node)) {
        setProfileOpen(false)
      }
    }
    document.addEventListener('mousedown', onDoc)
    return () => document.removeEventListener('mousedown', onDoc)
  }, [])

  const toggleSidebar = () => {
    document.body.classList.toggle('sb-sidenav-toggled')
  }

  const portalMenu = useMemo(
    () => (session ? getPortalMenu(session.role, flags) : []),
    [session, flags],
  )

  const websiteMenu = useMemo(
    () => getWebsiteBuilderMenu(builderCardId, flags, builderSlug),
    [builderCardId, flags, builderSlug],
  )

  if (!session || !isSessionValid(session)) {
    if (session && !isSessionValid(session)) {
      redirectToLogin('expired', session.role)
      return null
    }
    return <Navigate to={loginPathForRole(session?.role)} replace />
  }

  const displayName = session.name || session.email.split('@')[0] || 'User'
  const profileImg = resolveProfileSrc(profileImageUrl)
  const idLabel =
    session.role === 'FRANCHISEE'
      ? null
      : mwId
        ? `MW ID: MW${mwId}`
        : null

  const openProfileFilePicker = (e: React.MouseEvent) => {
    e.preventDefault()
    e.stopPropagation()
    setProfileOpen(false)
    setProfileUploadMsg(null)
    fileInputRef.current?.click()
  }

  const toggleProfileMenu = (e: React.MouseEvent) => {
    e.preventDefault()
    e.stopPropagation()
    setProfileOpen((v) => !v)
  }

  const onProfileFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    e.target.value = ''
    if (!file) return
    if (!file.type.startsWith('image/')) {
      setProfileUploadMsg('Please choose an image file (JPG, PNG, GIF, WebP).')
      return
    }
    // PHP ImageCropUpload allows up to 10MB before crop
    if (file.size > 10 * 1024 * 1024) {
      setProfileUploadMsg('Image must be 10MB or less.')
      return
    }
    setProfileUploadMsg(null)
    setCropFile(file)
    setCropOpen(true)
  }

  const uploadCroppedProfile = async (blob: Blob, fileName: string) => {
    if (!session?.email) return
    setProfileUploading(true)
    setProfileUploadMsg(null)
    try {
      const fd = new FormData()
      fd.append('email', session.email)
      fd.append('profile_image', blob, fileName)
      const res = await apiUploadForm<{
        imagePath?: string
        ImagePath?: string
        profileImageUrl?: string
        ProfileImageUrl?: string
      }>('/api/v1/portal/profile-image', fd)
      const data = (res.data ?? {}) as Record<string, unknown>
      const url = String(data.profileImageUrl ?? data.ProfileImageUrl ?? data.imagePath ?? data.ImagePath ?? '')
      if (url) setProfileImageUrl(`${url}${url.includes('?') ? '&' : '?'}t=${Date.now()}`)
      setProfileUploadMsg(res.message || 'Profile image updated successfully!')
      setCropOpen(false)
      setCropFile(null)
    } catch (err) {
      setProfileUploadMsg(err instanceof Error ? err.message : 'Upload failed')
    } finally {
      setProfileUploading(false)
    }
  }

  const isActivePath = (url: string) => {
    const [pathPart, query = ''] = url.split('?')
    const path = pathPart.startsWith('/') ? pathPart : `/${pathPart}`

    if (path === '/dashboard') return location.pathname === '/dashboard'
    if (!(location.pathname === path || location.pathname.startsWith(`${path}/`))) return false

    // Kit links share /kit but differ by ?kit= — match query exactly (PHP header.php)
    if (path === '/kit') {
      const wantKit = new URLSearchParams(query).get('kit') || ''
      const haveKit = new URLSearchParams(location.search).get('kit') || ''
      return wantKit === haveKit
    }

    if (query) {
      const want = new URLSearchParams(query)
      const have = new URLSearchParams(location.search)
      for (const [k, v] of want.entries()) {
        if (have.get(k) !== v) return false
      }
    }
    return true
  }

  const closeMobileNav = () => {
    if (window.innerWidth < 992) document.body.classList.add('sb-sidenav-toggled')
  }

  const renderPortalNavItem = (item: { id: string; label: string; icon: string; url: string }) => (
    <NavLink
      key={item.id}
      to={item.url}
      className={() => {
        const on = isActivePath(item.url)
        return `nav-link collapsed group mw-portal-nav-link${on ? ' active mw-nav-link-active' : ''}`
      }}
      onClick={closeMobileNav}
    >
      <div className="sb-nav-link-icon mw-portal-nav-icon-wrap">
        <i className={`fa fa-${navIconClass(item.icon)} mw-nav-icon`} aria-hidden />
      </div>
      <span className="flex-1 truncate">{item.label}</span>
    </NavLink>
  )

  const renderWebsiteNavItem = (item: WebsiteNavItem) => {
    const on = !item.locked && isActivePath(item.url)
    const linkClass = `nav-link collapsed group mw-portal-nav-link${on ? ' active mw-nav-link-active' : ''}${
      item.locked ? ' opacity-50' : ''
    }`

    const inner = (
      <>
        <div className={`sb-nav-link-icon mw-portal-nav-icon-wrap${item.locked ? ' bg-slate-50' : ''}`}>
          <i className={`fa fa-${navIconClass(item.icon)} mw-nav-icon`} aria-hidden />
        </div>
        <span className={`flex-1 truncate${item.locked ? ' text-slate-400' : ''}`}>{item.label}</span>
        {item.locked ? <i className="fa fa-lock text-slate-400 text-xs shrink-0" aria-hidden /> : null}
      </>
    )

    return (
      <div key={item.id}>
        {item.separatorBefore ? <hr className="my-2 mx-3 border-t border-border" /> : null}
        {item.locked ? (
          <span className={linkClass} aria-disabled="true" title="Complete Business Name and payment to unlock">
            {inner}
          </span>
        ) : item.external ? (
          <a className={linkClass} href={item.url} target="_blank" rel="noopener noreferrer" onClick={closeMobileNav}>
            {inner}
          </a>
        ) : (
          <NavLink
            to={item.url}
            className={() => linkClass}
            onClick={closeMobileNav}
            aria-current={on ? 'page' : undefined}
          >
            {inner}
          </NavLink>
        )}
      </div>
    )
  }

  return (
    <>
      <nav className="sb-topnav navbar navbar-expand navbar-dark bg-dark !shadow-card">
        <div className="head-left flex items-center">
          <Link className="navbar-brand ps-3 inline-flex items-center" to="/dashboard" aria-label="Go to dashboard">
            <img
              src={assetUrl('images/mw_logo_mobile.png')}
              className="mw-logo mw-logo--mobile img-fluid"
              alt="Mini Website"
              width={144}
              height={40}
            />
            <img
              src={assetUrl('img/logo.png')}
              className="mw-logo mw-logo--desktop img-fluid"
              alt="Mini Website — Create Share Succeed"
              width={270}
              height={73}
            />
          </Link>
          <button
            className="btn btn-link btn-sm order-1 order-lg-0"
            id="sidebarToggle"
            type="button"
            aria-label="Toggle navigation sidebar"
            aria-controls="layoutSidenav_nav"
            onClick={toggleSidebar}
          >
            <i className="fa fa-bars" aria-hidden />
          </button>
        </div>
        
        <div className="head-right">
          <ul className="navbar-nav ms-auto ms-md-0 flex items-center">
            <li className="nav-item upload-profile-wrap relative" ref={profileRef}>
              <div className="nav-link">
                <div className="upload-profile">
                  <div className="circle">
                    <img
                      className="profile-pic"
                      src={profileImg}
                      alt={`${displayName} profile`}
                      onError={(e) => {
                        const el = e.currentTarget
                        if (el.dataset.fallback === '1') return
                        el.dataset.fallback = '1'
                        el.src = DEFAULT_PROFILE_IMG()
                      }}
                    />
                    {/* PHP: only .profile-text click opens dropdown (assets/js/scripts.js) */}
                    <div
                      className="profile-text"
                      role="button"
                      tabIndex={0}
                      aria-haspopup="true"
                      aria-expanded={profileOpen}
                      aria-label="Open profile menu"
                      onClick={toggleProfileMenu}
                      onKeyDown={(e) => {
                        if (e.key === 'Enter' || e.key === ' ') {
                          e.preventDefault()
                          toggleProfileMenu(e as unknown as React.MouseEvent)
                        }
                      }}
                    >
                      <div className="profile-name-line">
                        {displayName}
                        <i className="fa fa-angle-double-down" aria-hidden />
                      </div>
                      {idLabel ? <span className="profile-mw-id">{idLabel}</span> : null}
                    </div>
                  </div>
                  {/* PHP: camera / .upload-button → file picker → crop modal */}
                  <div
                    className="p-image"
                    title="Click to upload profile image (Max: 10MB, Formats: JPG, PNG, GIF, WebP)"
                  >
                    <img
                      src={assetUrl('images/camera.png')}
                      className="upload-button img-fluid"
                      alt="Upload profile photo"
                      role="button"
                      tabIndex={0}
                      onClick={openProfileFilePicker}
                      onKeyDown={(ev) => {
                        if (ev.key === 'Enter' || ev.key === ' ') {
                          ev.preventDefault()
                          openProfileFilePicker(ev as unknown as React.MouseEvent)
                        }
                      }}
                    />
                    <input
                      ref={fileInputRef}
                      className="file-upload"
                      type="file"
                      accept="image/jpeg,image/png,image/gif,image/webp,image/*"
                      name="profile_image"
                      id="profile_image"
                      onChange={onProfileFileChange}
                    />
                  </div>
                </div>
              </div>
              {profileUploading ? (
                <div className="small text-muted px-2" style={{ position: 'absolute', right: 0, top: '100%' }}>
                  Uploading…
                </div>
              ) : null}
              {profileUploadMsg ? (
                <div
                  className="small px-2"
                  style={{ position: 'absolute', right: 0, top: '100%', whiteSpace: 'nowrap', color: '#334155', zIndex: 20 }}
                  role="status"
                >
                  {profileUploadMsg}
                </div>
              ) : null}
              <ul
                className={`dropdown-menu dropdown-menu-end mw-profile-dropdown${profileOpen ? ' show' : ''}`}
                role="menu"
              >
                <li role="none">
                  <Link role="menuitem" className="dropdown-item" to="/dashboard" onClick={() => setProfileOpen(false)}>
                    <i className="fa fa-tachometer w-4" aria-hidden />
                    <span>Dashboard</span>
                  </Link>
                </li>
                {portalMenu
                  .filter((m) => m.id !== 'dashboard')
                  .slice(0, 3)
                  .map((m) => (
                    <li key={m.id} role="none">
                      <Link
                        role="menuitem"
                        className="dropdown-item"
                        to={m.url}
                        onClick={() => setProfileOpen(false)}
                      >
                        <i className="fa fa-angle-right w-4" aria-hidden />
                        <span>{m.label}</span>
                      </Link>
                    </li>
                  ))}
                <li role="none">
                  <Link
                    role="menuitem"
                    className="dropdown-item"
                    to="/change-password"
                    onClick={() => setProfileOpen(false)}
                  >
                    <i className="fa fa-key w-4" aria-hidden />
                    <span>Change Password</span>
                  </Link>
                </li>
                <li role="none">
                  <hr className="dropdown-divider" />
                </li>
                <li role="none">
                  <button
                    type="button"
                    role="menuitem"
                    className="dropdown-item text-danger"
                    onClick={() => {
                      clearAuthSession()
                      window.location.href = '/logout'
                    }}
                  >
                    <i className="fa fa-sign-out w-4" aria-hidden />
                    <span>Logout</span>
                  </button>
                </li>
              </ul>
            </li>
          </ul>
        </div>
      </nav>

      <div id="layoutSidenav">
        <div id="layoutSidenav_nav">
          <nav className="sb-sidenav accordion" id="sidenavAccordion" aria-label="Primary">
            <div className="sb-sidenav-menu">
              <div className="nav" role="navigation" style={{ gap: 2, padding: '0 0.5rem' }}>
                {inWebsiteBuilder ? (
                  websiteMenu.map(renderWebsiteNavItem)
                ) : (
                  <>
                    {portalMenu.map(renderPortalNavItem)}
                    {session?.role === 'FRANCHISEE' ? (
                      <div className="mw-sidebar-invoice">
                        {franchiseInvoiceAvailable && session.email ? (
                          <a
                            href={`/api/v1/portal/franchise-invoice/html?email=${encodeURIComponent(session.email)}`}
                            className="mw-sidebar-invoice-btn"
                            target="_blank"
                            rel="noopener noreferrer"
                          >
                            <i className="fa fa-download" aria-hidden />
                            <span>Download Invoice</span>
                          </a>
                        ) : (
                          <span
                            className="mw-sidebar-invoice-btn is-disabled"
                            title="Invoice will be available only after franchise agreement payment verification."
                            aria-disabled="true"
                          >
                            <i className="fa fa-download" aria-hidden />
                            <span>Download Invoice</span>
                          </span>
                        )}
                      </div>
                    ) : null}
                  </>
                )}
              </div>
            </div>
          </nav>
        </div>

        <div id="layoutSidenav_content">
          <main className="Dashboard mw-page">
            <div className="container-fluid customer_content_area">
              <Outlet />
            </div>
          </main>
          <footer className="mw-portal-footer py-4 mt-auto" role="contentinfo">
            <div className="container-fluid px-4">
              <div className="Copyright">
                <div className="Copyright-left">
                  <a href="https://www.miniwebsite.in" target="_blank" rel="noopener noreferrer">
                    <i className="fa fa-globe" aria-hidden /> <span>www.miniwebsite.in</span>
                  </a>
                </div>
                <div className="Copyright-right">
                  <p>&copy; {new Date().getFullYear()} Mini Website. All Rights Reserved.</p>
                </div>
              </div>
            </div>
          </footer>
        </div>
      </div>

      <ProfileImageCropModal
        open={cropOpen}
        file={cropFile}
        busy={profileUploading}
        onClose={() => {
          if (profileUploading) return
          setCropOpen(false)
          setCropFile(null)
        }}
        onCropped={(blob, fileName) => {
          void uploadCroppedProfile(blob, fileName)
        }}
      />
    </>
  )
}
