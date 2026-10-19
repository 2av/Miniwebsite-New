import { useCallback, useEffect, useRef, useState } from 'react'
import { apiGet, apiUploadForm } from '@/shared/api/client'
import { PortalPageHeader } from '@/components/layout/PortalPageHeader'
import { useAuthSession } from '@/shared/auth/useAuthSession'
import { assetUrl } from '@/lib/utils'
import { ProfileImageCropModal } from '@/features/portal/ProfileImageCropModal'

type IdCardProfile = {
  name: string
  email: string
  phone: string
  department: string
  role: string
  profileImageUrl: string
  employeeId: string
  idPrefix: string
  designation: string
  website: string
}

const CARD_WIDTH_INCH = 2.125
const CARD_HEIGHT_INCH = 3.34
const EXPORT_DPI = 300

function pick(raw: Record<string, unknown>, ...keys: string[]): string {
  for (const k of keys) {
    const v = raw[k]
    if (v != null && String(v).trim() !== '') return String(v)
  }
  return ''
}

/** Rewrite absolute media/site URLs through Vite proxy so canvas stays untainted in dev. */
function canvasSafeUrl(url: string): string {
  if (!url || url.startsWith('blob:') || url.startsWith('data:') || url.startsWith('/')) return url
  if (typeof window === 'undefined') return url
  const media = (import.meta.env.VITE_MEDIA_BASE_URL || '').replace(/\/$/, '')
  const site = (import.meta.env.VITE_SITE_URL || '').replace(/\/$/, '')
  for (const base of [media, site]) {
    if (base && url.startsWith(`${base}/`)) {
      return `/php-site${url.slice(base.length)}`
    }
  }
  return url
}

/** Prefer same-origin public copy; fall back to PHP assets. */
function idCardBackgroundUrl(): string {
  const base = import.meta.env.BASE_URL || '/'
  const prefix = base.endsWith('/') ? base : `${base}/`
  return `${prefix}idcard/idcardbackground.PNG`
}

function roundRect(
  ctx: CanvasRenderingContext2D,
  x: number,
  y: number,
  width: number,
  height: number,
  radius: number,
) {
  const r = Math.min(radius, width / 2, height / 2)
  ctx.beginPath()
  ctx.moveTo(x + r, y)
  ctx.lineTo(x + width - r, y)
  ctx.quadraticCurveTo(x + width, y, x + width, y + r)
  ctx.lineTo(x + width, y + height - r)
  ctx.quadraticCurveTo(x + width, y + height, x + width - r, y + height)
  ctx.lineTo(x + r, y + height)
  ctx.quadraticCurveTo(x, y + height, x, y + height - r)
  ctx.lineTo(x, y + r)
  ctx.quadraticCurveTo(x, y, x + r, y)
  ctx.closePath()
}

function drawWebsiteIcon(
  ctx: CanvasRenderingContext2D,
  iconX: number,
  centerY: number,
  iconWidth: number,
) {
  const radius = iconWidth * 0.35
  const centerX = iconX + radius + iconWidth * 0.15

  ctx.save()
  ctx.strokeStyle = '#000000'
  ctx.lineWidth = Math.max(radius * 0.25, 1)

  ctx.beginPath()
  ctx.arc(centerX, centerY, radius, 0, Math.PI * 2)
  ctx.stroke()

  ctx.beginPath()
  ctx.ellipse(centerX, centerY, radius * 0.9, radius * 0.5, 0, 0, Math.PI * 2)
  ctx.stroke()
  ctx.beginPath()
  ctx.ellipse(centerX, centerY, radius * 0.9, radius * 0.25, 0, 0, Math.PI * 2)
  ctx.stroke()

  ctx.beginPath()
  ctx.moveTo(centerX - radius, centerY)
  ctx.lineTo(centerX + radius, centerY)
  ctx.stroke()
  ctx.beginPath()
  ctx.moveTo(centerX, centerY - radius)
  ctx.lineTo(centerX, centerY + radius)
  ctx.stroke()

  ctx.restore()
}

function mapProfile(raw: Record<string, unknown>, fallback?: { name?: string; email?: string; role?: string }): IdCardProfile {
  const role = pick(raw, 'role', 'Role') || fallback?.role || ''
  const isFranchisee = role.toUpperCase() === 'FRANCHISEE'
  return {
    name: pick(raw, 'name', 'Name') || fallback?.name || '',
    email: pick(raw, 'email', 'Email') || fallback?.email || '',
    phone: pick(raw, 'phone', 'Phone'),
    department: pick(raw, 'department', 'Department'),
    role,
    profileImageUrl: pick(raw, 'profileImageUrl', 'ProfileImageUrl'),
    employeeId: pick(raw, 'employeeId', 'EmployeeId'),
    idPrefix: pick(raw, 'idPrefix', 'IdPrefix') || (isFranchisee ? 'FR ID: ' : 'ID: '),
    designation:
      pick(raw, 'designation', 'Designation') ||
      (isFranchisee ? 'Franchise Partner' : 'Sales Executive'),
    website: pick(raw, 'website', 'Website') || 'www.sharebizz.com',
  }
}

export function IdCardPage() {
  const session = useAuthSession()
  const canvasRef = useRef<HTMLCanvasElement>(null)
  const fileInputRef = useRef<HTMLInputElement>(null)

  const [profile, setProfile] = useState<IdCardProfile | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [alert, setAlert] = useState<{ type: 'success' | 'danger'; text: string } | null>(null)
  const [bgReady, setBgReady] = useState(false)
  const [previewUrl, setPreviewUrl] = useState<string | null>(null)
  const [cropFile, setCropFile] = useState<File | null>(null)
  const [cropOpen, setCropOpen] = useState(false)
  const [uploading, setUploading] = useState(false)
  const [pendingBlob, setPendingBlob] = useState<Blob | null>(null)
  const [pendingFileName, setPendingFileName] = useState('profile.jpg')

  const bgImageRef = useRef<HTMLImageElement | null>(null)
  const profileImageRef = useRef<HTMLImageElement | null>(null)
  const [profileReady, setProfileReady] = useState(false)
  const baseSizeRef = useRef({ w: 443, h: 685 })

  useEffect(() => {
    if (!session?.email) return
    const email = session.email
    const fallback = { name: session.name, email: session.email, role: session.role }
    ;(async () => {
      try {
        const raw = await apiGet<Record<string, unknown>>('/api/v1/portal/id-card', { email })
        const mapped = mapProfile(raw, fallback)
        setProfile(mapped)
        if (mapped.profileImageUrl) setPreviewUrl(mapped.profileImageUrl)
      } catch (e) {
        setError(e instanceof Error ? e.message : 'Failed to load')
      }
    })()
  }, [session?.email, session?.name, session?.role])

  // Load background once (public copy first, then PHP assets)
  useEffect(() => {
    let cancelled = false

    const load = (src: string, allowFallback: boolean) => {
      const img = new Image()
      img.crossOrigin = 'anonymous'
      img.onload = () => {
        if (cancelled) return
        bgImageRef.current = img
        baseSizeRef.current = { w: img.width, h: img.height }
        setBgReady(true)
      }
      img.onerror = () => {
        if (cancelled) return
        if (allowFallback) {
          load(canvasSafeUrl(assetUrl('images/idcard/idcardbackground.PNG')), false)
          return
        }
        setError('Failed to load ID card background image. Please contact administrator.')
      }
      img.src = src
    }

    load(idCardBackgroundUrl(), true)
    return () => {
      cancelled = true
    }
  }, [])

  // Load / refresh profile photo for canvas
  useEffect(() => {
    const url = previewUrl
    if (!url) {
      profileImageRef.current = null
      setProfileReady(false)
      return
    }
    let cancelled = false
    const img = new Image()
    img.crossOrigin = 'anonymous'
    img.onload = () => {
      if (cancelled) return
      profileImageRef.current = img
      setProfileReady(true)
    }
    img.onerror = () => {
      if (cancelled) return
      profileImageRef.current = null
      setProfileReady(false)
    }
    img.src = canvasSafeUrl(url)
    return () => {
      cancelled = true
    }
  }, [previewUrl])

  const drawIdCard = useCallback(() => {
    const canvas = canvasRef.current
    const bg = bgImageRef.current
    if (!canvas || !bg || !bgReady) return

    const ctx = canvas.getContext('2d')
    if (!ctx) return

    const width = Math.round(CARD_WIDTH_INCH * EXPORT_DPI)
    const height = Math.round(CARD_HEIGHT_INCH * EXPORT_DPI)
    if (canvas.width !== width || canvas.height !== height) {
      canvas.width = width
      canvas.height = height
    }

    const { w: baseW, h: baseH } = baseSizeRef.current
    const widthScale = canvas.width / baseW
    const heightScale = canvas.height / baseH
    const scaleX = (val: number) => val * widthScale
    const scaleY = (val: number) => val * heightScale

    ctx.clearRect(0, 0, canvas.width, canvas.height)
    ctx.drawImage(bg, 0, 0, canvas.width, canvas.height)

    const cardWidth = canvas.width
    const p = profile

    const employeeId = p?.employeeId || ''
    const idPrefix = p?.idPrefix || 'ID: '
    if (employeeId) {
      ctx.fillStyle = '#ffffff'
      ctx.font = `${Math.max(scaleY(20), 14)}px "Poppins", sans-serif`
      ctx.textAlign = 'right'
      ctx.fillText(idPrefix + employeeId, cardWidth - scaleX(30), scaleY(30))
      ctx.textAlign = 'left'
    }

    const profileWidth = scaleX(199)
    const profileHeight = scaleY(220)
    const profileX = (cardWidth - profileWidth) / 2
    const profileY = scaleY(125)
    const cornerRadius = scaleX(12)
    const borderWidth = scaleX(4)

    ctx.save()
    ctx.fillStyle = '#FFBE17'
    roundRect(
      ctx,
      profileX - borderWidth,
      profileY - borderWidth,
      profileWidth + borderWidth * 2,
      profileHeight + borderWidth * 2,
      cornerRadius + 2,
    )
    ctx.fill()

    ctx.fillStyle = '#ffffff'
    roundRect(ctx, profileX, profileY, profileWidth, profileHeight, cornerRadius)
    ctx.fill()

    const profileImg = profileImageRef.current
    if (profileReady && profileImg?.complete) {
      roundRect(ctx, profileX, profileY, profileWidth, profileHeight, cornerRadius)
      ctx.clip()
      ctx.drawImage(profileImg, profileX, profileY, profileWidth, profileHeight)
    }
    ctx.restore()

    ctx.textAlign = 'center'
    const centerX = cardWidth / 2
    const profileAreaEnd = 125 + 220
    let currentY = scaleY(profileAreaEnd + 50)
    const lineHeight = scaleY(40)

    ctx.fillStyle = '#000000'
    ctx.font = `bold ${Math.max(scaleY(35), 18)}px "Poppins", sans-serif`
    const name = (p?.name || 'Your Name').toUpperCase()
    ctx.fillText(name, centerX, currentY)

    currentY += lineHeight + 3
    ctx.fillStyle = '#0066CC'
    ctx.font = `bold ${Math.max(scaleY(33), 16)}px "Poppins", sans-serif`
    ctx.fillText(`(${p?.designation || 'Sales Executive'})`, centerX, currentY)

    currentY += lineHeight + scaleY(15)
    ctx.textAlign = 'left'
    ctx.textBaseline = 'middle'
    ctx.fillStyle = '#000000'
    ctx.font = `${Math.max(scaleY(21), 10)}px "Poppins", sans-serif`
    const contactIconX = cardWidth / 2 - scaleX(160)
    const iconColumnWidth = scaleX(28)
    const iconTextGap = scaleX(10)
    const contactTextX = contactIconX + iconColumnWidth + iconTextGap

    if (p?.phone) {
      ctx.fillText('☎', contactIconX, currentY)
      ctx.fillText(p.phone, contactTextX, currentY)
      currentY += lineHeight
    }
    if (p?.email) {
      ctx.fillText('✉', contactIconX, currentY)
      ctx.fillText(p.email, contactTextX, currentY)
      currentY += lineHeight
    }
    if (p?.website) {
      drawWebsiteIcon(ctx, contactIconX, currentY, iconColumnWidth)
      ctx.fillText(p.website, contactTextX, currentY)
    }

    ctx.textAlign = 'left'
    ctx.textBaseline = 'alphabetic'
  }, [bgReady, profile, profileReady])

  useEffect(() => {
    drawIdCard()
  }, [drawIdCard])

  const onFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    e.target.value = ''
    if (!file) return
    if (!file.type.startsWith('image/')) {
      setAlert({ type: 'danger', text: 'Please choose an image file (JPG, PNG, GIF, WebP).' })
      return
    }
    if (file.size > 10 * 1024 * 1024) {
      setAlert({ type: 'danger', text: 'Image must be 10MB or less.' })
      return
    }
    setAlert(null)
    setCropFile(file)
    setCropOpen(true)
  }

  const onCropped = (blob: Blob, fileName: string) => {
    setPendingBlob(blob)
    setPendingFileName(fileName)
    const url = URL.createObjectURL(blob)
    setPreviewUrl(url)
    setCropOpen(false)
    setCropFile(null)
  }

  const uploadAndRefresh = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!session?.email) return
    if (!pendingBlob) {
      setAlert({ type: 'danger', text: 'Please select and adjust a profile picture first' })
      return
    }
    setUploading(true)
    setAlert(null)
    try {
      const fd = new FormData()
      fd.append('email', session.email)
      fd.append('profile_image', pendingBlob, pendingFileName)
      const res = await apiUploadForm<{
        profileImageUrl?: string
        ProfileImageUrl?: string
      }>('/api/v1/portal/profile-image', fd)
      const data = (res.data ?? {}) as Record<string, unknown>
      const url = String(data.profileImageUrl ?? data.ProfileImageUrl ?? '')
      if (url) {
        setPreviewUrl(`${url}${url.includes('?') ? '&' : '?'}t=${Date.now()}`)
        setProfile((prev) => (prev ? { ...prev, profileImageUrl: url } : prev))
      }
      setPendingBlob(null)
      setAlert({
        type: 'success',
        text: res.message || 'Profile picture uploaded successfully!',
      })
    } catch (err) {
      setAlert({
        type: 'danger',
        text: err instanceof Error ? err.message : 'Error uploading profile picture',
      })
    } finally {
      setUploading(false)
    }
  }

  const downloadIdCard = () => {
    const canvas = canvasRef.current
    if (!canvas) return
    drawIdCard()
    try {
      const link = document.createElement('a')
      link.download = `id_card_${Date.now()}.png`
      link.href = canvas.toDataURL('image/png')
      link.click()
    } catch {
      setAlert({
        type: 'danger',
        text: 'Could not download ID card image. Try again after the preview finishes loading.',
      })
    }
  }

  return (
    <>
      <PortalPageHeader title="ID Card" />

      {error && <div className="alert alert-danger">{error}</div>}
      {alert && (
        <div className={`alert alert-${alert.type} alert-dismissible fade show`} role="alert">
          {alert.text}
          <button
            type="button"
            className="close"
            aria-label="Close"
            onClick={() => setAlert(null)}
          >
            <span aria-hidden="true">&times;</span>
          </button>
        </div>
      )}

      <div className="card mb-4">
        <div className="card-body">
          <div className="idcard-head">
            <h2 className="heading">ID Card Generator</h2>
            <p className="text-muted mb-4 sub_title">
              Upload your profile picture to generate your personalized ID card. All information is
              automatically loaded from your account.
            </p>

            <div className="row mb-4">
              <div className="col-12">
                <div className="idcard-preview-wrapper">
                  <div className="row align-items-start justify-content-center">
                    <div className="col-12 col-md-7 idcard-canvas-col mb-4 mb-md-0">
                      <canvas ref={canvasRef} id="idcardCanvas" className="idcard-canvas" />
                    </div>

                    <div className="col-12 col-md-5">
                      <div className="upload-download-box">
                        <form onSubmit={uploadAndRefresh} id="uploadForm">
                          <div className="form-group displayFlex">
                            <label htmlFor="profile_picture" className="font-weight-semibold heading">
                              Choose Profile Picture
                            </label>
                            <input
                              ref={fileInputRef}
                              type="file"
                              className="form-control-file"
                              id="profile_picture"
                              name="profile_picture"
                              accept="image/*"
                              onChange={onFileChange}
                            />
                            <small className="form-text text-muted">
                              Max 250KB • JPG, PNG, GIF • Square image works best
                            </small>
                          </div>
                          <div className="form-group">
                            <div id="profilePreview" className="profile-preview">
                              {previewUrl ? (
                                <img src={previewUrl} alt="Profile Preview" id="previewImg" />
                              ) : (
                                <p className="text-muted mb-0">No profile picture uploaded</p>
                              )}
                            </div>
                          </div>
                          <div className="action-buttons d-flex flex-column flex-sm-row align-items-center justify-content-start">
                            <button
                              type="submit"
                              name="process_profile"
                              className="btn btn-primary flex-grow-1 mb-3 mb-sm-0"
                              disabled={uploading}
                            >
                              {uploading ? 'Uploading…' : 'Upload & Refresh Preview'}
                            </button>
                            <button
                              type="button"
                              className="btn btn-success flex-grow-1"
                              onClick={downloadIdCard}
                            >
                              <i className="fa fa-download mr-2" />
                              Download ID Card
                            </button>
                          </div>
                        </form>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <ProfileImageCropModal
        file={cropFile}
        open={cropOpen}
        busy={uploading}
        onClose={() => {
          if (uploading) return
          setCropOpen(false)
          setCropFile(null)
        }}
        onCropped={onCropped}
      />
    </>
  )
}
