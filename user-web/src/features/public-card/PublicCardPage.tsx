import { useCallback, useEffect, useMemo, useRef, useState, type CSSProperties } from 'react'
import { Link, useParams } from 'react-router-dom'
import { apiGet } from '@/shared/api/client'
import { assetUrl, publicUrl } from '@/lib/utils'
import { useProfileEdit } from '@/features/website/profileEditContext'
import { EditSaveBar, HoursField, InlineText, MiniIcon } from '@/features/website/profileEditorUi'
import {
  SAMPLE_WEBSITE,
  emailError,
  formatMobile,
  gstError,
  gstText,
  mobileDigits,
  mobileError,
} from '@/features/website/profileFieldRules'

type PublicCardViewer = {
  status?: string
  Status?: string
  message?: string
  Message?: string
  companyLabel?: string
  CompanyLabel?: string
  redirectSlug?: string
  RedirectSlug?: string
  redirectUrl?: string
  RedirectUrl?: string
  expiryDateDisplay?: string
  ExpiryDateDisplay?: string
  card?: PublicCard | null
  Card?: PublicCard | null
}

type PublicCard = {
  id?: number
  Id?: number
  slug?: string
  Slug?: string
  themeNumber?: number
  ThemeNumber?: number
  themeCssPath?: string
  ThemeCssPath?: string
  layoutCssPath?: string
  LayoutCssPath?: string
  heroName?: string
  HeroName?: string
  primaryCategoryTitle?: string
  PrimaryCategoryTitle?: string
  heroCoverUrl?: string
  HeroCoverUrl?: string
  logoUrl?: string
  LogoUrl?: string
  defaultImageUrl?: string
  DefaultImageUrl?: string
  about?: string
  About?: string
  phone?: string
  Phone?: string
  whatsapp?: string
  Whatsapp?: string
  email?: string
  Email?: string
  location?: string
  Location?: string
  googleDirection?: string
  GoogleDirection?: string
  website?: string
  Website?: string
  shareUrl?: string
  ShareUrl?: string
  gstNumber?: string
  GstNumber?: string
  fullAddress?: string
  FullAddress?: string
  businessHours?: BusinessHour[]
  BusinessHours?: BusinessHour[]
  socialLinks?: SocialLink[]
  SocialLinks?: SocialLink[]
  services?: ServiceItem[]
  Services?: ServiceItem[]
  offers?: OfferItem[]
  Offers?: OfferItem[]
  products?: ProductItem[]
  Products?: ProductItem[]
  galleryUrls?: string[]
  GalleryUrls?: string[]
  videoUrls?: string[]
  VideoUrls?: string[]
  paymentQrs?: PaymentQr[]
  PaymentQrs?: PaymentQr[]
  appointmentBookingEnabled?: boolean
  AppointmentBookingEnabled?: boolean
}

type BusinessHour = { days?: string; Days?: string; hours?: string; Hours?: string }
type SocialLink = { icon?: string; Icon?: string; url?: string; Url?: string }
type ServiceItem = {
  name?: string
  Name?: string
  description?: string
  Description?: string
  imageUrl?: string
  ImageUrl?: string
}
type OfferItem = {
  title?: string
  Title?: string
  description?: string
  Description?: string
  imageUrl?: string
  ImageUrl?: string
  badge?: string
  Badge?: string
  discountPercentage?: number
  DiscountPercentage?: number
  startDate?: string
  StartDate?: string
  startTime?: string
  StartTime?: string
  endDate?: string
  EndDate?: string
  endTime?: string
  EndTime?: string
}
type ProductItem = {
  name?: string
  Name?: string
  description?: string
  Description?: string
  imageUrl?: string
  ImageUrl?: string
  mrp?: number
  Mrp?: number
  sellingPrice?: number
  SellingPrice?: number
  priceType?: string
  PriceType?: string
  pricingUnit?: string
  PricingUnit?: string
  ctaText?: string
  CtaText?: string
  categoryLabel?: string
  CategoryLabel?: string
}
type PaymentQr = {
  label?: string
  Label?: string
  imageDataUrl?: string
  ImageDataUrl?: string
  upi?: string
  Upi?: string
}

function pick<T>(a: T | undefined, b: T | undefined): T | undefined {
  return a !== undefined && a !== null ? a : b
}

function ensureLink(href: string, id: string) {
  const existing = document.getElementById(id) as HTMLLinkElement | null
  if (existing) {
    if (existing.getAttribute('href') !== href) existing.setAttribute('href', href)
    return
  }
  const link = document.createElement('link')
  link.id = id
  link.rel = 'stylesheet'
  link.href = href
  document.head.appendChild(link)
}

function releaseEditorStyles() {
  for (const id of ['mw-card-theme', 'mw-card-layout', 'mw-card-components']) {
    document.getElementById(id)?.remove()
  }
}

function ensureScript(src: string, id: string): Promise<void> {
  if (document.getElementById(id)) return Promise.resolve()
  return new Promise((resolve, reject) => {
    const s = document.createElement('script')
    s.id = id
    s.src = src
    s.onload = () => resolve()
    s.onerror = () => reject(new Error(`Failed to load ${src}`))
    document.head.appendChild(s)
  })
}

function parseVideo(url: string, fallback: string) {
  const u = url.trim()
  const watch = /^https?:\/\//i.test(u) ? u : `https://${u}`
  const yt = u.match(/(?:youtube\.com\/watch\?v=|youtu\.be\/|youtube\.com\/shorts\/)([a-zA-Z0-9_-]{11})/)
  if (yt) {
    return {
      title: 'YouTube Video',
      thumb: `https://img.youtube.com/vi/${yt[1]}/hqdefault.jpg`,
      playMode: 'iframe' as const,
      embedUrl: `https://www.youtube.com/embed/${yt[1]}?autoplay=1`,
      watchUrl: watch,
    }
  }
  return {
    title: 'Video',
    thumb: fallback,
    playMode: 'external' as const,
    embedUrl: '',
    watchUrl: watch,
  }
}

function appointmentLink(phone: string, whatsapp: string, email: string) {
  const message = 'Hi, I would like to book an appointment.'
  const wa = whatsapp.replace(/\D/g, '')
  if (wa) return { href: `https://wa.me/${wa}?text=${encodeURIComponent(message)}`, external: true }
  const tel = phone.replace(/[^\d+]/g, '')
  if (tel) return { href: `tel:${tel}`, external: false }
  const mail = email.trim()
  if (mail) {
    return {
      href: `mailto:${mail}?subject=${encodeURIComponent('Appointment request')}&body=${encodeURIComponent(message)}`,
      external: false,
    }
  }
  return null
}

function absHttp(url: string) {
  const u = url.trim()
  if (!u) return ''
  return /^https?:\/\//i.test(u) ? u : `https://${u}`
}

const BANNER_OFFSET = 76

function withUploadVersion(url: string, version: number) {
  if (!url || version <= 0) return url
  return `${url}${url.includes('?') ? '&' : '?'}v=${version}`
}

function assetPath(url: string) {
  return url.split('?')[0]
}

function scrollToSection(id: string) {
  document.getElementById(id)?.scrollIntoView({ behavior: 'smooth', block: 'start' })
}

type BannerJump = {
  id: string
  label: string
  present: boolean
  onMissing?: () => void
}

function EditorBanner({ jumps }: { jumps: BannerJump[] }) {
  const edit = useProfileEdit()
  const [active, setActive] = useState('mw-hero')
  const [canScrollLeft, setCanScrollLeft] = useState(false)
  const [canScrollRight, setCanScrollRight] = useState(false)
  const menuRef = useRef<HTMLDivElement>(null)
  const jumpKey = jumps.map((item) => `${item.id}:${item.present ? 1 : 0}`).join('|')

  useEffect(() => {
    const ids = ['mw-hero', ...jumpKey.split('|').filter(Boolean).map((part) => part.split(':')[0])]
    const onScroll = () => {
      let current = 'mw-hero'
      for (const id of ids) {
        const node = document.getElementById(id)
        if (!node) continue
        if (node.getBoundingClientRect().top <= BANNER_OFFSET + 24) current = id
      }
      setActive(current)
    }
    onScroll()
    window.addEventListener('scroll', onScroll, { passive: true })
    return () => window.removeEventListener('scroll', onScroll)
  }, [jumpKey])

  useEffect(() => {
    const menu = menuRef.current
    if (!menu) return
    const update = () => {
      const max = menu.scrollWidth - menu.clientWidth
      setCanScrollLeft(menu.scrollLeft > 4)
      setCanScrollRight(max - menu.scrollLeft > 4)
    }
    update()
    menu.addEventListener('scroll', update, { passive: true })
    const observer = new ResizeObserver(update)
    observer.observe(menu)
    return () => {
      menu.removeEventListener('scroll', update)
      observer.disconnect()
    }
  }, [jumpKey])

  useEffect(() => {
    const menu = menuRef.current
    const chip = menu?.querySelector<HTMLElement>(`[data-jump="${active}"]`)
    if (!menu || !chip) return
    const left = chip.offsetLeft - menu.clientWidth / 2 + chip.offsetWidth / 2
    menu.scrollTo({ left: Math.max(0, left), behavior: 'smooth' })
  }, [active])

  const nudgeMenu = (direction: -1 | 1) => {
    const menu = menuRef.current
    if (!menu) return
    menu.scrollBy({ left: direction * Math.max(160, menu.clientWidth * 0.65), behavior: 'smooth' })
  }

  const go = (item: BannerJump) => {
    if (!item.present) item.onMissing?.()
    window.setTimeout(() => scrollToSection(item.id), item.present ? 0 : 80)
  }

  return (
    <header
      className="mw-editor-banner"
      style={{
        position: 'fixed',
        top: 0,
        left: 0,
        right: 0,
        zIndex: 64,
        display: 'block',
        minHeight: 64,
        overflow: 'visible',
        background: 'linear-gradient(180deg, rgba(22, 12, 48, 0.96), rgba(42, 22, 86, 0.94))',
        backdropFilter: 'blur(18px)',
        borderBottom: '1px solid rgba(244, 214, 154, 0.28)',
        boxShadow: '0 12px 32px rgba(16, 8, 36, 0.35)',
        fontFamily: 'Poppins, Inter, sans-serif',
      }}
    >
      <style>{'.mw-editor-jumps::-webkit-scrollbar{display:none}'}</style>
      <div
        ref={menuRef}
        className="mw-editor-jumps"
        aria-label="Profile sections"
        style={{
          display: 'flex',
          flexWrap: 'nowrap',
          alignItems: 'center',
          gap: 8,
          width: '100%',
          maxWidth: '100vw',
          minWidth: 0,
          padding: `12px ${canScrollRight ? 44 : 16}px 12px ${canScrollLeft ? 44 : 16}px`,
          boxSizing: 'border-box',
          overflowX: 'auto',
          overflowY: 'hidden',
          WebkitOverflowScrolling: 'touch',
          touchAction: 'pan-x',
          overscrollBehaviorX: 'contain',
          scrollbarWidth: 'none',
        }}
      >
        <div style={{ display: 'flex', flexWrap: 'nowrap', alignItems: 'center', gap: 8, width: 'max-content', paddingRight: 8 }}>
          <Link to="/dashboard" style={bannerActionStyle}>
            Dashboard
          </Link>
          <button
            type="button"
            style={{ ...bannerActionStyle, border: 'none', cursor: 'pointer', fontFamily: 'inherit' }}
            onClick={() => edit?.openThemeDrawer()}
          >
            Change theme
          </button>
          <span style={{ width: 1, height: 22, flex: '0 0 auto', background: 'rgba(255,255,255,0.16)' }} />
          <button type="button" data-jump="mw-hero" style={jumpStyle(active === 'mw-hero')} onClick={() => scrollToSection('mw-hero')}>
            Home
          </button>
          {jumps.map((item) => (
            <button
              key={item.id}
              type="button"
              data-jump={item.id}
              style={jumpStyle(active === item.id, !item.present)}
              onClick={() => go(item)}
            >
              {item.present ? item.label : `Add ${item.label.toLowerCase()}`}
            </button>
          ))}
        </div>
      </div>
      {canScrollLeft ? (
        <button type="button" aria-label="Scroll menu left" style={menuArrowStyle('left')} onClick={() => nudgeMenu(-1)}>
          <i className="fas fa-chevron-left" />
        </button>
      ) : null}
      {canScrollRight ? (
        <button type="button" aria-label="Scroll menu right" style={menuArrowStyle('right')} onClick={() => nudgeMenu(1)}>
          <i className="fas fa-chevron-right" />
        </button>
      ) : null}
    </header>
  )
}

function menuArrowStyle(side: 'left' | 'right'): CSSProperties {
  return {
    position: 'absolute',
    top: '50%',
    transform: 'translateY(-50%)',
    [side]: 8,
    width: 30,
    height: 30,
    borderRadius: '50%',
    border: '1px solid rgba(244, 214, 154, 0.7)',
    background: 'linear-gradient(180deg, #f8e7c0, #e2c27a)',
    color: '#2c1a08',
    display: 'grid',
    placeItems: 'center',
    cursor: 'pointer',
    zIndex: 2,
    padding: 0,
    fontSize: 12,
    boxShadow: '0 4px 14px rgba(0,0,0,0.28)',
  }
}

const bannerActionStyle = {
  display: 'inline-flex',
  alignItems: 'center',
  height: 36,
  padding: '0 14px',
  borderRadius: 999,
  background: 'linear-gradient(180deg, #f8e7c0, #e2c27a)',
  color: '#2c1a08',
  textDecoration: 'none',
  fontSize: 13,
  fontWeight: 700,
  letterSpacing: '0.01em',
  whiteSpace: 'nowrap',
  flex: '0 0 auto',
  boxShadow: 'inset 0 1px 0 rgba(255,255,255,0.65)',
} as const

function jumpStyle(active: boolean, missing = false): CSSProperties {
  return {
    height: 34,
    padding: '0 12px',
    borderRadius: 999,
    border: missing ? '1px solid rgba(255, 138, 138, 0.8)' : active ? '1px solid #f4d69a' : '1px solid rgba(255,255,255,0.14)',
    background: missing ? 'rgba(255, 82, 82, 0.14)' : active ? 'rgba(244, 214, 154, 0.2)' : 'rgba(255,255,255,0.06)',
    color: missing ? '#ff8d8d' : '#fff',
    fontFamily: 'Poppins, Inter, sans-serif',
    fontSize: 12.5,
    fontWeight: 600,
    letterSpacing: '0.01em',
    whiteSpace: 'nowrap',
    cursor: 'pointer',
    flex: '0 0 auto',
  }
}

const sectionActionStyle = {
  alignSelf: 'flex-start',
  marginTop: 8,
  background: 'transparent',
  color: 'inherit',
  border: '1px solid currentColor',
  borderRadius: 8,
  padding: '4px 10px',
  font: 'inherit',
  cursor: 'pointer',
} as const

function serviceNameError(value: string) {
  const text = value.trim()
  if (!text) return 'Service name is required.'
  if (text.length > 30) return 'Service name can be at most 30 characters.'
  return null
}

function serviceDescriptionError(value: string) {
  if (value.trim().length > 400) return 'Service description can be at most 400 characters.'
  return null
}

function offerTitleError(value: string) {
  if (!value.trim()) return 'Offer title is required.'
  return null
}

function offerBadgeError(value: string) {
  if (value.trim().length > 20) return 'Offer badge can be at most 20 characters.'
  return null
}

function offerDescriptionError(value: string) {
  if (value.trim().length > 500) return 'Offer description can be at most 500 characters.'
  return null
}

function pricingTitleError(value: string) {
  const text = value.trim()
  if (!text) return 'Title is required.'
  if (text.length > 30) return 'Title can be at most 30 characters.'
  return null
}

function pricingDescriptionError(value: string) {
  if (value.trim().length > 400) return 'Description can be at most 400 characters.'
  return null
}

function productPriceKind(value: string) {
  const text = value.toLowerCase().replace(/[\s_]+/g, '')
  if (text.includes('request')) return 'request'
  if (text.includes('start')) return 'starting'
  return 'fixed'
}

function ProductPriceLine({ kind, selling, unit }: { kind: string; selling: number; unit: string }) {
  if (kind === 'request') return <p className="text-heading font-bold mt-2">Price on Request{unit ? ` · ${unit}` : ''}</p>
  const amount = `₹${selling}${unit ? ` ${unit}` : ''}`
  return <p className="text-heading font-bold mt-2">{kind === 'starting' ? `Starting from ${amount}` : amount}</p>
}

function ProductCta({ text }: { text: string }) {
  if (!text) return null
  return (
    <p className="mt-3">
      <span className="inline-block px-3 py-1.5 rounded-theme text-sm font-semibold" style={{ background: 'var(--mw-offer-cta-bg, var(--color-primary))', color: '#111' }}>
        {text}
      </span>
    </p>
  )
}

function offerWhen(offer: OfferItem) {
  const startDate = pick(offer.startDate, offer.StartDate)
  const startTime = pick(offer.startTime, offer.StartTime)
  const endDate = pick(offer.endDate, offer.EndDate)
  const endTime = pick(offer.endTime, offer.EndTime)
  const start = formatOfferMoment(startDate, startTime)
  const end = formatOfferMoment(endDate, endTime)
  if (start && end) return `${start} – ${end}`
  return start || end
}

function formatOfferMoment(date?: string, time?: string) {
  if (!date) return ''
  const match = date.match(/^(\d{4})-(\d{2})-(\d{2})/)
  if (!match) return date
  const months = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec']
  let label = `${Number(match[3])} ${months[Number(match[2]) - 1]} ${match[1]}`
  const clock = (time || '').match(/^(\d{2}):(\d{2})/)
  if (clock) {
    let hour = Number(clock[1])
    const suffix = hour >= 12 ? 'PM' : 'AM'
    hour = hour % 12 || 12
    label += `, ${hour}:${clock[2]} ${suffix}`
  }
  return label
}

/** Public digi-card viewer — mirrors n.php sections + theme CSS. */
export function PublicCardPage({ demo = false, forcedSlug }: { demo?: boolean; forcedSlug?: string }) {
  const params = useParams()
  const slug = forcedSlug ?? params.slug ?? ''
  const edit = useProfileEdit()
  const revision = edit?.revision ?? 0
  const [viewer, setViewer] = useState<PublicCardViewer | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [stylesReady, setStylesReady] = useState(false)
  const [videoModal, setVideoModal] = useState<string | null>(null)
  const [galleryIdx, setGalleryIdx] = useState<number | null>(null)
  const [waShare, setWaShare] = useState('')
  const [openSection, setOpenSection] = useState<string | null>(null)

  useEffect(() => {
    if (!slug) return
    let cancelled = false
    ;(async () => {
      try {
        const data = await apiGet<PublicCardViewer>(`/api/v1/public/cards/${encodeURIComponent(slug)}`)
        if (!cancelled) {
          setViewer(data)
          setError(null)
        }
      } catch (e) {
        if (!cancelled) setError(e instanceof Error ? e.message : 'Failed to load card')
      }
    })()
    return () => {
      cancelled = true
    }
  }, [slug, revision])

  useEffect(() => {
    if (!forcedSlug) return
    const previousTitle = document.title
    return () => {
      releaseEditorStyles()
      document.title = previousTitle
    }
  }, [forcedSlug])

  const status = pick(viewer?.status, viewer?.Status) || ''
  const card = pick(viewer?.card, viewer?.Card) || null

  useEffect(() => {
    if (!card) return
    let cancelled = false
    ;(async () => {
      try {
        ensureLink(
          'https://fonts.googleapis.com/css2?family=Great+Vibes&family=Inter:wght@300;400;500;600;700&family=Roboto+Condensed:wght@400;700&display=swap',
          'mw-card-fonts',
        )
        ensureLink('https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.5.1/css/all.min.css', 'mw-card-fa')
        ensureLink(publicUrl('theme/css/components.css'), 'mw-card-components')
        ensureLink(
          publicUrl(pick(card.themeCssPath, card.ThemeCssPath) || 'theme/css/theme1.css'),
          'mw-card-theme',
        )
        ensureLink(
          publicUrl(pick(card.layoutCssPath, card.LayoutCssPath) || 'theme/css/layout1.css'),
          'mw-card-layout',
        )
        await ensureScript('https://cdn.tailwindcss.com', 'mw-card-tailwind')
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        const tw = (window as any).tailwind
        if (tw?.config) {
          tw.config = {
            theme: {
              extend: {
                colors: {
                  primary: 'var(--mw-primary-color)',
                  secondary: 'var(--mw-secondary-color)',
                  bgbase: 'var(--mw-background-color)',
                  cardbg: 'var(--mw-card-background)',
                  textmain: 'var(--mw-text-color)',
                  heading: 'var(--mw-heading-color)',
                },
                fontFamily: {
                  heading: ['var(--mw-font-heading)', 'cursive'],
                  body: ['var(--mw-font-body)', 'sans-serif'],
                },
                borderRadius: { theme: 'var(--mw-border-radius)' },
              },
            },
          }
        }
        if (!cancelled) setStylesReady(true)
      } catch {
        if (!cancelled) setStylesReady(true)
      }
    })()
    return () => {
      cancelled = true
    }
  }, [card])

  const heroName = pick(card?.heroName, card?.HeroName) || 'ShareBizz'
  useEffect(() => {
    if (status === 'ok' && heroName) document.title = heroName
  }, [status, heroName])

  const defaultImg = pick(card?.defaultImageUrl, card?.DefaultImageUrl) || assetUrl('img/default.jpg')
  const shareUrl = pick(card?.shareUrl, card?.ShareUrl) || ''
  const services = pick(card?.services, card?.Services) || []
  const offers = pick(card?.offers, card?.Offers) || []
  const products = pick(card?.products, card?.Products) || []
  const gallery = pick(card?.galleryUrls, card?.GalleryUrls) || []
  const videos = useMemo(() => {
    const urls = pick(card?.videoUrls, card?.VideoUrls) || []
    return urls.map((u) => parseVideo(u, defaultImg))
  }, [card, defaultImg])
  const paymentQrs = pick(card?.paymentQrs, card?.PaymentQrs) || []
  const social = pick(card?.socialLinks, card?.SocialLinks) || []
  const hours = pick(card?.businessHours, card?.BusinessHours) || []
  const appointmentEnabled = Boolean(pick(card?.appointmentBookingEnabled, card?.AppointmentBookingEnabled))

  const onShareWa = useCallback(() => {
    const digits = waShare.replace(/\D/g, '')
    if (digits.length < 10) {
      window.alert('Enter a valid WhatsApp number')
      return
    }
    const text = encodeURIComponent(`Check out this ShareBizz: ${shareUrl}`)
    window.open(`https://wa.me/${digits}?text=${text}`, '_blank')
  }, [waShare, shareUrl])

  const onCopyLink = useCallback(async () => {
    try {
      await navigator.clipboard.writeText(shareUrl)
      window.alert('Link copied!')
    } catch {
      window.prompt('Copy link:', shareUrl)
    }
  }, [shareUrl])

  if (error) {
    return (
      <div className="min-h-screen flex items-center justify-center p-6 bg-slate-50 text-slate-800">
        <div className="max-w-md text-center space-y-4">
          <p>{error}</p>
          <Link to="/">Go to ShareBizz</Link>
        </div>
      </div>
    )
  }

  if (!viewer) {
    return (
      <div className="min-h-screen flex items-center justify-center p-6 bg-slate-50 text-slate-600">
        Loading…
      </div>
    )
  }

  if (status === 'not_found') {
    return (
      <div className="min-h-screen flex items-center justify-center p-6 bg-slate-50 text-slate-800">
        <div className="max-w-md text-center space-y-6">
          <p className="text-lg text-slate-600">
            {pick(viewer.message, viewer.Message) ||
              'This digital card is not available. It may have been removed or the link might be incorrect.'}
          </p>
          <Link
            to="/"
            className="inline-flex items-center justify-center rounded-lg bg-slate-900 text-white px-6 py-3 text-sm font-medium"
          >
            Go to ShareBizz
          </Link>
        </div>
      </div>
    )
  }

  if (status === 'redirect') {
    const label = pick(viewer.companyLabel, viewer.CompanyLabel) || 'Company'
    const url = pick(viewer.redirectUrl, viewer.RedirectUrl) || ''
    const newSlug = pick(viewer.redirectSlug, viewer.RedirectSlug) || ''
    return (
      <div className="min-h-screen flex items-center justify-center p-6 bg-slate-50 text-slate-800">
        <div className="max-w-lg w-full text-center space-y-6">
          <p className="text-lg text-slate-700 leading-relaxed">
            {label} ShareBizz URL changed to new URL.{' '}
            <Link
              to={demo ? `/demo/${newSlug}` : `/n/${newSlug}`}
              className="text-slate-900 font-medium underline underline-offset-2"
            >
              Click here to Visit: {url || newSlug}
            </Link>
          </p>
        </div>
      </div>
    )
  }

  if (status === 'expired') {
    return (
      <div className="min-h-screen flex items-center justify-center p-6" style={{ background: '#0f172a', color: '#e5e7eb' }}>
        <div className="w-full max-w-xl rounded-2xl border border-white/10 bg-[#111827] shadow-2xl p-8 md:p-10 text-center">
          <div className="mx-auto mb-5 flex h-14 w-14 items-center justify-center rounded-full bg-red-500/20 text-red-300">
            <i className="fas fa-calendar-xmark text-2xl" />
          </div>
          <h1 className="text-2xl md:text-3xl font-bold tracking-tight">ShareBizz is expired</h1>
          <p className="mt-3 text-sm md:text-base text-[#9ca3af]">
            {pick(viewer.message, viewer.Message) ||
              'This ShareBizz has reached its validity end date and is no longer available.'}
          </p>
          {pick(viewer.expiryDateDisplay, viewer.ExpiryDateDisplay) && (
            <p className="mt-4 inline-flex items-center rounded-full border border-red-300/30 bg-red-500/10 px-4 py-1.5 text-sm text-red-200">
              Expired on: {pick(viewer.expiryDateDisplay, viewer.ExpiryDateDisplay)}
            </p>
          )}
        </div>
      </div>
    )
  }

  if (!card || !stylesReady) {
    return (
      <div className="min-h-screen flex items-center justify-center p-6 bg-slate-50 text-slate-600">
        Loading…
      </div>
    )
  }

  const phone = pick(card.phone, card.Phone) || ''
  const whatsapp = pick(card.whatsapp, card.Whatsapp) || ''
  const email = pick(card.email, card.Email) || ''
  const booking = appointmentLink(phone, whatsapp, email)
  const location = pick(card.location, card.Location) || ''
  const direction = pick(card.googleDirection, card.GoogleDirection) || ''
  const website = pick(card.website, card.Website) || ''
  const about = pick(card.about, card.About) || ''
  const category = pick(card.primaryCategoryTitle, card.PrimaryCategoryTitle) || ''
  const rawCover = pick(card.heroCoverUrl, card.HeroCoverUrl) || ''
  const rawLogo = pick(card.logoUrl, card.LogoUrl) || ''
  const logoFollowsCover = Boolean(rawLogo && rawCover && assetPath(rawLogo) === assetPath(rawCover))
  const cover = withUploadVersion(rawCover || defaultImg, edit?.revision ?? 0)
  const logo = withUploadVersion(logoFollowsCover ? defaultImg : rawLogo || defaultImg, edit?.revision ?? 0)
  const gst = pick(card.gstNumber, card.GstNumber) || ''
  const fullAddress = pick(card.fullAddress, card.FullAddress) || location
  const qrSrc = `https://api.qrserver.com/v1/create-qr-code/?size=150x150&data=${encodeURIComponent(shareUrl)}`

  return (
    <>
      {edit ? (
        <>
          <div style={{ height: 64 }} aria-hidden />
          <EditorBanner
            jumps={[
              {
                id: 'mw-about',
                label: 'About',
                present: Boolean(about.trim()),
                onMissing: () => edit.openDialog({ kind: 'about' }),
              },
              {
                id: 'mw-contact',
                label: 'Contact',
                present: Boolean(email || phone || location || direction),
                onMissing: () => setOpenSection('contact'),
              },
              {
                id: 'mw-appointment',
                label: 'Appointment',
                present: Boolean(pick(card?.appointmentBookingEnabled, card?.AppointmentBookingEnabled)),
                onMissing: () => edit.openDialog({ kind: 'appointment' }),
              },
              {
                id: 'mw-social',
                label: 'Social',
                present: social.length > 0,
                onMissing: () => edit.openDialog({ kind: 'social' }),
              },
              {
                id: 'mw-services',
                label: 'Services',
                present: services.length > 0,
                onMissing: () => edit.openDialog({ kind: 'service-add' }),
              },
              {
                id: 'mw-offers',
                label: 'Offers',
                present: offers.length > 0,
                onMissing: () => edit.openDialog({ kind: 'offer-add' }),
              },
              {
                id: 'mw-products',
                label: 'Pricing',
                present: products.length > 0,
                onMissing: () => edit.openDialog({ kind: 'product-add' }),
              },
              {
                id: 'mw-videos',
                label: 'Videos',
                present: videos.length > 0,
                onMissing: () => edit.openDialog({ kind: 'videos' }),
              },
              {
                id: 'mw-gallery',
                label: 'Gallery',
                present: gallery.length > 0,
                onMissing: () => edit.openDialog({ kind: 'gallery-add' }),
              },
              {
                id: 'mw-pay',
                label: 'Payment',
                present: paymentQrs.length > 0,
                onMissing: () => edit.openDialog({ kind: 'payment' }),
              },
              { id: 'mw-hours', label: 'Hours', present: true },
            ]}
          />
        </>
      ) : null}
      <div className="app-container">
        <section id="mw-hero" className="mw-hero relative" style={edit ? { scrollMarginTop: BANNER_OFFSET } : undefined}>
          {edit ? (
            <span style={{ position: 'absolute', top: 12, right: 12, zIndex: 30, display: 'flex', gap: 6 }}>
              <MiniIcon label="Edit profile images" icon="fa-pen" onClick={() => edit.openDialog({ kind: 'images' })} />
            </span>
          ) : null}
          <div className="h-64 md:h-80 w-full overflow-hidden relative">
            <img
              src={cover}
              alt="Cover"
              className="w-full h-full object-cover opacity-60"
              onError={(e) => {
                ;(e.target as HTMLImageElement).src = defaultImg
              }}
            />
            <div className="absolute inset-0 bg-gradient-to-t from-bgbase to-transparent" style={{ pointerEvents: 'none' }} />
          </div>
          <div className="mw-section-padding relative -mt-24 text-center z-10 flex flex-col items-center">
            <div className="w-32 h-32 rounded-full border-4 border-bgbase shadow-card overflow-hidden mb-4 relative">
              <img
                src={logo}
                alt="Logo"
                className="w-full h-full object-cover"
                onError={(e) => {
                  ;(e.target as HTMLImageElement).src = defaultImg
                }}
              />
            </div>
            <h1 className="text-3xl md:text-4xl font-bold mb-1">{heroName}</h1>
            {category ? (
              <p className="text-heading font-semibold text-base md:text-lg mb-3 mt-0">{category}</p>
            ) : null}
            <div className="flex gap-4 w-full max-w-sm justify-center">
              {phone ? (
                <a
                  href={`tel:${phone}`}
                  className="flex-1 bg-cardbg border border-primary text-primary py-3 px-4 rounded-theme hover:bg-primary hover:text-bgbase transition-colors flex items-center justify-center gap-2 font-medium"
                >
                  <i className="fas fa-phone-alt" /> Call Now
                </a>
              ) : null}
              {whatsapp ? (
                <a
                  href={`https://wa.me/${whatsapp.replace(/\D/g, '')}`}
                  className="flex-1 bg-cardbg border border-primary text-primary py-3 px-4 rounded-theme hover:bg-primary hover:text-bgbase transition-colors flex items-center justify-center gap-2 font-medium"
                >
                  <i className="fab fa-whatsapp" /> WhatsApp
                </a>
              ) : null}
            </div>
          </div>
        </section>

        {social.length > 0 && (
          <section id="mw-social" className="mw-social-links pb-6 flex justify-center gap-5" style={edit ? { position: 'relative', scrollMarginTop: BANNER_OFFSET } : undefined}>
            {edit ? (
              <span style={{ position: 'absolute', top: 0, right: 12, zIndex: 5 }}>
                <MiniIcon label="Edit social links" icon="fa-pen" onClick={() => edit.openDialog({ kind: 'social' })} />
              </span>
            ) : null}
            {social.map((s, i) => (
              <a
                key={i}
                href={pick(s.url, s.Url)}
                target="_blank"
                rel="noopener noreferrer"
                className="w-10 h-10 rounded-full bg-cardbg flex items-center justify-center text-primary hover:bg-primary hover:text-bgbase transition"
              >
                <i className={`fab fa-${pick(s.icon, s.Icon)}`} />
              </a>
            ))}
          </section>
        )}

        {about ? (
          <section id="mw-about" className="mw-business-intro mw-section-padding text-center" style={edit ? { position: 'relative', scrollMarginTop: BANNER_OFFSET } : undefined}>
            {edit ? (
              <span style={{ position: 'absolute', top: 12, right: 12, zIndex: 5 }}>
                <MiniIcon label="Edit about" icon="fa-pen" onClick={() => edit.openDialog({ kind: 'about' })} />
              </span>
            ) : null}
            <p className="text-sm md:text-base leading-relaxed max-w-3xl mx-auto whitespace-pre-line">{about}</p>
          </section>
        ) : null}

        {(openSection === 'contact' || email || phone || location || direction) && (
          <section id="mw-contact" className="mw-action-grid mw-section-padding" style={edit ? { position: 'relative', scrollMarginTop: BANNER_OFFSET } : undefined}>
            <EditSaveBar
              enabled={Boolean(edit)}
              editing={openSection === 'contact'}
              editLabel="Edit contact"
              onEdit={() => setOpenSection((current) => (current === 'contact' ? null : 'contact'))}
            >
            <h2 className="mw-section-title">Contact</h2>
            <div className="grid grid-cols-2 gap-4 max-w-4xl mx-auto">
              {(edit && openSection === 'contact') || email ? (
                <div className="mw-card p-4 flex flex-col gap-2">
                  <span className="mw-contact-icon inline-flex w-10 h-10 items-center justify-center rounded-theme text-primary">
                    <i className="fas fa-envelope text-xl" />
                  </span>
                  <h3 className="text-xs uppercase tracking-wider text-textmain mt-2">Email Address</h3>
                  {edit && openSection === 'contact' ? (
                    <InlineText
                      draftId="contact-email"
                      value={edit.company.email}
                      placeholder="Enter your email (optional)"
                      className="text-heading font-medium text-sm"
                      validate={emailError}
                      onSave={(value) => edit.patchCompany({ email: value.trim() })}
                    />
                  ) : (
                    <p className="text-heading font-medium text-sm truncate">{email}</p>
                  )}
                </div>
              ) : null}
              {(edit && openSection === 'contact') || phone ? (
                <div className="mw-card p-4 flex flex-col gap-2">
                  <span className="mw-contact-icon inline-flex w-10 h-10 items-center justify-center rounded-theme text-primary">
                    <i className="fas fa-phone-alt text-xl" />
                  </span>
                  <h3 className="text-xs uppercase tracking-wider text-textmain mt-2">Phone Number</h3>
                  {edit && openSection === 'contact' ? (
                    <InlineText
                      draftId="contact-phone"
                      value={formatMobile(edit.company.contact)}
                      placeholder="10-digit mobile number"
                      className="text-heading font-medium text-sm"
                      normalize={formatMobile}
                      validate={mobileError}
                      onSave={(value) => edit.patchCompany({ contact: mobileDigits(value) })}
                    />
                  ) : (
                    <p className="text-heading font-medium text-sm">{phone}</p>
                  )}
                </div>
              ) : null}
              {edit && openSection === 'contact' ? (
                <div className="mw-card p-4 flex flex-col gap-2">
                  <span className="mw-contact-icon inline-flex w-10 h-10 items-center justify-center rounded-theme text-primary">
                    <i className="fab fa-whatsapp text-xl" />
                  </span>
                  <h3 className="text-xs uppercase tracking-wider text-textmain mt-2">WhatsApp</h3>
                  <InlineText
                    draftId="contact-whatsapp"
                    value={formatMobile(edit.company.whatsapp)}
                    placeholder="10-digit mobile number"
                    className="text-heading font-medium text-sm"
                    normalize={formatMobile}
                    validate={mobileError}
                    onSave={(value) => edit.patchCompany({ whatsapp: mobileDigits(value) })}
                  />
                </div>
              ) : null}
              {edit && openSection === 'contact' ? (
                <div className="mw-card p-4 flex flex-col gap-2">
                  <span className="mw-contact-icon inline-flex w-10 h-10 items-center justify-center rounded-theme text-primary">
                    <i className="fas fa-phone text-xl" />
                  </span>
                  <h3 className="text-xs uppercase tracking-wider text-textmain mt-2">Alternate Number</h3>
                  <InlineText
                    draftId="contact-alternate"
                    value={formatMobile(edit.company.contact2)}
                    placeholder="10-digit mobile number"
                    className="text-heading font-medium text-sm"
                    normalize={formatMobile}
                    validate={mobileError}
                    onSave={(value) => edit.patchCompany({ contact2: mobileDigits(value) })}
                  />
                </div>
              ) : null}
              {(edit && openSection === 'contact') || location ? (
                <div className="mw-card p-4 flex flex-col gap-2">
                  <span className="mw-contact-icon inline-flex w-10 h-10 items-center justify-center rounded-theme text-primary">
                    <i className="fas fa-map-marker-alt text-xl" />
                  </span>
                  <h3 className="text-xs uppercase tracking-wider text-textmain mt-2">Location</h3>
                  <p className="text-heading font-medium text-sm">{location || 'Add address'}</p>
                  {edit && openSection === 'contact' ? (
                    <button type="button" style={sectionActionStyle} onClick={() => edit.openDialog({ kind: 'address' })}>
                      Edit address
                    </button>
                  ) : null}
                </div>
              ) : null}
              {direction ? (
                <div className="mw-card p-4 flex flex-col gap-2">
                  <span className="mw-contact-icon inline-flex w-10 h-10 items-center justify-center rounded-theme text-primary">
                    <i className="fas fa-directions text-xl" />
                  </span>
                  <h3 className="text-xs uppercase tracking-wider text-textmain mt-2">Google Direction</h3>
                  <a
                    href={absHttp(direction)}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="text-heading font-medium text-sm truncate hover:underline"
                  >
                    Get Directions
                  </a>
                </div>
              ) : null}
            </div>
            </EditSaveBar>
          </section>
        )}

        {appointmentEnabled ? (
          <section id="mw-appointment" className="mw-appointment mw-section-padding" style={edit ? { position: 'relative', scrollMarginTop: BANNER_OFFSET } : undefined}>
            {edit ? (
              <span style={{ position: 'absolute', top: 12, right: 12, zIndex: 5 }}>
                <MiniIcon label="Edit appointment booking" icon="fa-pen" onClick={() => edit.openDialog({ kind: 'appointment' })} />
              </span>
            ) : null}
            <h2 className="mw-section-title">Appointment Booking</h2>
            <div className="max-w-md mx-auto text-center">
              <p className="text-sm md:text-base mb-4">Choose a time that works for you. We will confirm your appointment.</p>
              {booking ? (
                <a
                  href={booking.href}
                  {...(booking.external ? { target: '_blank', rel: 'noopener noreferrer' } : {})}
                  className="inline-flex items-center justify-center gap-2 bg-cardbg border border-primary text-primary py-3 px-6 rounded-theme hover:bg-primary hover:text-bgbase transition-colors font-medium"
                >
                  <i className="fas fa-calendar-check" /> Book Appointment
                </a>
              ) : (
                <p className="text-sm">
                  {edit
                    ? 'Add a phone, WhatsApp number, or email so visitors can request an appointment.'
                    : 'Contact us to book an appointment.'}
                </p>
              )}
            </div>
          </section>
        ) : null}

        <section className="mw-qr-share mw-section-padding bg-cardbg/50">
          <h2 className="mw-section-title">Share Profile</h2>
          <div className="max-w-md mx-auto mw-card p-6 text-center">
            <div className="bg-white p-2 w-40 h-40 mx-auto rounded-lg mb-6">
              <img src={qrSrc} alt="QR Code" className="w-full h-full" />
            </div>
            <div className="space-y-4">
              <input
                type="tel"
                placeholder="Enter WhatsApp Number"
                className="mw-input"
                maxLength={15}
                value={waShare}
                onChange={(e) => setWaShare(e.target.value)}
              />
              <div className="flex gap-3 items-center">
                <button
                  type="button"
                  onClick={onShareWa}
                  style={{ background: 'var(--mw-offer-cta-bg)', color: '#111' }}
                  className="mw-share-btn flex-1 bg-cardbg border border-primary text-primary py-3 px-4 rounded-theme flex items-center justify-center gap-2 font-medium cursor-pointer"
                >
                  <i className="fab fa-whatsapp" /> Share on WhatsApp
                </button>
              </div>
              <div className="flex gap-4">
                <button
                  type="button"
                  onClick={onCopyLink}
                  className="mw-share-btn flex-1 bg-cardbg border border-primary text-primary py-3 px-4 rounded-theme flex items-center justify-center gap-2 font-medium cursor-pointer"
                >
                  Share Link
                </button>
              </div>
            </div>
          </div>
        </section>

        {services.length > 0 && (
          <section id="mw-services" className="mw-services mw-section-padding bg-cardbg/30" style={edit ? { position: 'relative', scrollMarginTop: BANNER_OFFSET } : undefined}>
            <EditSaveBar
              enabled={Boolean(edit)}
              editing={openSection === 'services'}
              editLabel="Edit services"
              onEdit={() => setOpenSection((current) => (current === 'services' ? null : 'services'))}
            >
            <h2 className="mw-section-title">Our Services</h2>
            {edit && openSection === 'services' ? (
              <div style={{ display: 'flex', justifyContent: 'center', marginBottom: 16 }}>
                {services.length >= 10 ? (
                  <p className="text-sm text-textmain text-center" style={{ margin: 0 }}>You can add up to 10 services.</p>
                ) : (
                  <button type="button" style={sectionActionStyle} onClick={() => edit.openDialog({ kind: 'service-add' })}>
                    Add service
                  </button>
                )}
              </div>
            ) : null}
            {services.length === 0 ? (
              <p className="text-sm text-textmain text-center">No services yet.</p>
            ) : (
              <div className="mw-grid-services">
                {services.map((svc, i) => (
                  <div key={i} className="mw-card mw-offer-card mw-service-card bg-cardbg rounded-theme overflow-hidden relative">
                    <div className="mw-service-image-wrap" style={{ position: 'relative' }}>
                      <img
                        src={pick(svc.imageUrl, svc.ImageUrl) || defaultImg}
                        alt={pick(svc.name, svc.Name) || ''}
                        onError={(e) => {
                          ;(e.target as HTMLImageElement).src = defaultImg
                        }}
                      />
                      {edit && openSection === 'services' ? (
                        <span style={{ position: 'absolute', top: 8, right: 8, display: 'flex', gap: 6 }}>
                          <MiniIcon label="Change service image" icon="fa-pen" onClick={() => edit.openDialog({ kind: 'service-image', index: i })} />
                          <MiniIcon label="Delete service" icon="fa-trash" tone="danger" onClick={() => void edit.removeService(i)} />
                        </span>
                      ) : null}
                    </div>
                    <div className="p-5">
                      {edit && openSection === 'services' ? (
                        <>
                          <h3 className="text-heading font-semibold text-lg mb-1">
                            <InlineText
                              draftId={`service-${i}-name`}
                              block
                              maxLength={30}
                              value={pick(svc.name, svc.Name) || ''}
                              placeholder="Enter service name (e.g. Haircut, AC Repair)"
                              validate={serviceNameError}
                              onSave={(value) => edit.patchService(i, { productName: value.trim().slice(0, 30) })}
                            />
                          </h3>
                          <InlineText
                            draftId={`service-${i}-description`}
                            block
                            multiline
                            maxLength={400}
                            className="mw-service-desc-preview text-sm text-textmain"
                            value={pick(svc.description, svc.Description) || ''}
                            placeholder="Describe your service in short (features, benefits, pricing idea)"
                            validate={serviceDescriptionError}
                            onSave={(value) => edit.patchService(i, { productDescription: value.trim().slice(0, 400) })}
                          />
                        </>
                      ) : (
                        <>
                          <h3 className="text-heading font-semibold text-lg mb-1">{pick(svc.name, svc.Name)}</h3>
                          <p className="mw-service-desc-preview text-sm text-textmain line-clamp-3">
                            {pick(svc.description, svc.Description) || 'Contact us for details.'}
                          </p>
                        </>
                      )}
                    </div>
                  </div>
                ))}
              </div>
            )}
            </EditSaveBar>
          </section>
        )}

        {offers.length > 0 && (
          <section id="mw-offers" className="mw-offers mw-section-padding" style={edit ? { position: 'relative', scrollMarginTop: BANNER_OFFSET } : undefined}>
            <EditSaveBar
              enabled={Boolean(edit)}
              editing={openSection === 'offers'}
              editLabel="Edit offers"
              onEdit={() => setOpenSection((current) => (current === 'offers' ? null : 'offers'))}
            >
            <h2 className="mw-section-title">Special Offers</h2>
            {edit && openSection === 'offers' ? (
              <div style={{ display: 'flex', justifyContent: 'center', marginBottom: 16 }}>
                {(edit.offerCount ?? offers.length) >= 5 ? (
                  <p className="text-sm text-textmain text-center" style={{ margin: 0 }}>You can add up to 5 special offers.</p>
                ) : (
                  <button type="button" style={sectionActionStyle} onClick={() => edit.openDialog({ kind: 'offer-add' })}>
                    Add offer
                  </button>
                )}
              </div>
            ) : null}
            {offers.length === 0 ? (
              <p className="text-sm text-textmain text-center">No offers yet.</p>
            ) : (
              <div className="mw-grid-offers">
                {offers.map((o, i) => {
                  const discount = pick(o.discountPercentage, o.DiscountPercentage) || 0
                  return (
                    <div key={i} className="mw-card mw-offer-card bg-cardbg rounded-theme overflow-hidden" style={{ position: 'relative' }}>
                      <img
                        src={pick(o.imageUrl, o.ImageUrl) || defaultImg}
                        alt={pick(o.title, o.Title) || ''}
                        className="w-full aspect-video object-cover"
                        onError={(e) => {
                          ;(e.target as HTMLImageElement).src = defaultImg
                        }}
                      />
                      {edit && openSection === 'offers' ? (
                        <span style={{ position: 'absolute', top: 8, right: 8, display: 'flex', gap: 6, zIndex: 2 }}>
                          <MiniIcon label="Change offer image" icon="fa-pen" onClick={() => edit.openDialog({ kind: 'offer-image', index: i })} />
                          <MiniIcon label="Delete offer" icon="fa-trash" tone="danger" onClick={() => void edit.removeOffer(i)} />
                        </span>
                      ) : null}
                      <div className="p-5">
                        {!(edit && openSection === 'offers') && pick(o.badge, o.Badge) ? (
                          <span className="text-xs uppercase tracking-wider text-primary">{pick(o.badge, o.Badge)}</span>
                        ) : null}
                        {edit && openSection === 'offers' ? (
                          <>
                            <InlineText
                              draftId={`offer-${i}-badge`}
                              value={pick(o.badge, o.Badge) || ''}
                              placeholder="e.g. Flat 20% Off"
                              maxLength={20}
                              validate={offerBadgeError}
                              className="text-xs uppercase tracking-wider text-primary"
                              onSave={(value) => edit.patchOffer(i, { badge: value.trim().slice(0, 20) })}
                            />
                            <h3 className="text-heading font-semibold text-lg">
                              <InlineText
                                draftId={`offer-${i}-title`}
                                block
                                value={pick(o.title, o.Title) || ''}
                                placeholder="Offer title"
                                validate={offerTitleError}
                                onSave={(value) => edit.patchOffer(i, { offerTitle: value.trim() })}
                              />
                            </h3>
                            <InlineText
                              draftId={`offer-${i}-description`}
                              block
                              multiline
                              maxLength={500}
                              className="text-sm text-textmain mt-1"
                              value={pick(o.description, o.Description) || ''}
                              placeholder="Explain the offer clearly (validity, benefits, conditions)"
                              validate={offerDescriptionError}
                              onSave={(value) => edit.patchOffer(i, { offerDescription: value.trim().slice(0, 500) })}
                            />
                            {offerWhen(o) ? <p className="text-sm text-textmain mt-2">{offerWhen(o)}</p> : null}
                          </>
                        ) : (
                          <>
                            <h3 className="text-heading font-semibold text-lg">{pick(o.title, o.Title)}</h3>
                            <p className="text-sm text-textmain mt-1">{pick(o.description, o.Description)}</p>
                            {discount > 0 ? <p className="text-primary font-semibold mt-2">{discount}% OFF</p> : null}
                            {offerWhen(o) ? <p className="text-sm text-textmain mt-2">{offerWhen(o)}</p> : null}
                          </>
                        )}
                      </div>
                    </div>
                  )
                })}
              </div>
            )}
            </EditSaveBar>
          </section>
        )}

        {products.length > 0 && (
          <section id="mw-products" className="mw-products mw-section-padding bg-cardbg/20" style={edit ? { position: 'relative', scrollMarginTop: BANNER_OFFSET } : undefined}>
            <EditSaveBar
              enabled={Boolean(edit)}
              editing={openSection === 'products'}
              editLabel="Edit products"
              onEdit={() => setOpenSection((current) => (current === 'products' ? null : 'products'))}
            >
            <h2 className="mw-section-title">Details &amp; Pricing</h2>
            {edit && openSection === 'products' ? (
              <div style={{ display: 'flex', justifyContent: 'center', marginBottom: 16 }}>
                {(edit.productCount ?? products.length) >= 60 ? (
                  <p className="text-sm text-textmain text-center" style={{ margin: 0 }}>You can add up to 60 products or services.</p>
                ) : (
                  <button type="button" style={sectionActionStyle} onClick={() => edit.openDialog({ kind: 'product-add' })}>
                    Add product or service
                  </button>
                )}
              </div>
            ) : null}
            {products.length === 0 ? (
              <p className="text-sm text-textmain text-center">No products yet.</p>
            ) : (
              <div className="mw-grid-products">
                {products.map((p, i) => {
                  const selling = pick(p.sellingPrice, p.SellingPrice) ?? 0
                  const kind = productPriceKind(String(pick(p.priceType, p.PriceType) || ''))
                  const unit = pick(p.pricingUnit, p.PricingUnit) || ''
                  const cta = pick(p.ctaText, p.CtaText) || ''
                  const category = pick(p.categoryLabel, p.CategoryLabel) || ''
                  return (
                    <div key={i} className="mw-card p-4" style={{ position: 'relative' }}>
                      <img
                        src={pick(p.imageUrl, p.ImageUrl) || defaultImg}
                        alt={pick(p.name, p.Name) || ''}
                        className="w-full aspect-square object-cover rounded-theme mb-3"
                        onError={(e) => {
                          ;(e.target as HTMLImageElement).src = defaultImg
                        }}
                      />
                      {edit && openSection === 'products' ? (
                        <span style={{ position: 'absolute', top: 16, right: 16, display: 'flex', gap: 6 }}>
                          <MiniIcon label="Change product image" icon="fa-pen" onClick={() => edit.openDialog({ kind: 'product-image', index: i })} />
                          <MiniIcon label="Delete product" icon="fa-trash" tone="danger" onClick={() => void edit.removeProduct(i)} />
                        </span>
                      ) : null}
                      {edit && openSection === 'products' ? (
                        <>
                          {category ? <p className="text-xs uppercase tracking-wider text-primary mb-1">{category}</p> : null}
                          <h3 className="text-heading font-semibold">
                            <InlineText
                              draftId={`product-${i}-name`}
                              block
                              maxLength={30}
                              value={pick(p.name, p.Name) || ''}
                              placeholder="Enter item/service name"
                              validate={pricingTitleError}
                              onSave={(value) => edit.patchProduct(i, { productName: value.trim().slice(0, 30) })}
                            />
                          </h3>
                          <InlineText
                            draftId={`product-${i}-description`}
                            block
                            multiline
                            maxLength={400}
                            className="text-sm text-textmain"
                            value={pick(p.description, p.Description) || ''}
                            placeholder="Add short details (features, usage, service info)"
                            validate={pricingDescriptionError}
                            onSave={(value) => edit.patchProduct(i, { productDescription: value.trim().slice(0, 400) })}
                          />
                          <ProductPriceLine kind={kind} selling={selling} unit={unit} />
                          <ProductCta text={cta} />
                        </>
                      ) : (
                        <>
                          {category ? <p className="text-xs uppercase tracking-wider text-primary mb-1">{category}</p> : null}
                          <h3 className="text-heading font-semibold">{pick(p.name, p.Name)}</h3>
                          <p className="text-sm text-textmain line-clamp-2">{pick(p.description, p.Description)}</p>
                          <ProductPriceLine kind={kind} selling={selling} unit={unit} />
                          <ProductCta text={cta} />
                        </>
                      )}
                    </div>
                  )
                })}
              </div>
            )}
            </EditSaveBar>
          </section>
        )}

        {videos.length > 0 && (
          <section id="mw-videos" className="mw-video-gallery mw-section-padding bg-cardbg/20" style={edit ? { position: 'relative', scrollMarginTop: BANNER_OFFSET } : undefined}>
            <h2 className="mw-section-title">Videos</h2>
            {edit ? (
              <span style={{ position: 'absolute', top: 12, right: 12 }}>
                <MiniIcon label="Edit videos" icon="fa-pen" onClick={() => edit.openDialog({ kind: 'videos' })} />
              </span>
            ) : null}
            <div className="mw-grid-videos">
              {videos.map((v, idx) =>
                v.playMode === 'iframe' ? (
                  <button
                    type="button"
                    key={idx}
                    className="mw-video-item mw-card aspect-video relative group cursor-pointer overflow-hidden block w-full"
                    onClick={() => setVideoModal(v.embedUrl)}
                  >
                    <img src={v.thumb} className="w-full h-full object-cover opacity-60" alt={v.title} />
                    <div className="absolute inset-0 flex items-center justify-center">
                      <div className="w-12 h-12 bg-primary/90 text-bgbase rounded-full flex items-center justify-center text-xl shadow-lg">
                        <i className="fas fa-play ml-1" />
                      </div>
                    </div>
                  </button>
                ) : (
                  <a
                    key={idx}
                    href={v.watchUrl}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="mw-video-item mw-card aspect-video relative group cursor-pointer overflow-hidden block no-underline"
                  >
                    <img src={v.thumb} className="w-full h-full object-cover opacity-60" alt={v.title} />
                    <div className="absolute inset-0 flex items-center justify-center">
                      <div className="w-12 h-12 bg-primary/90 text-bgbase rounded-full flex items-center justify-center text-xl shadow-lg">
                        <i className="fas fa-external-link-alt ml-0.5" />
                      </div>
                    </div>
                  </a>
                ),
              )}
            </div>
          </section>
        )}

        {gallery.length > 0 && (
          <section id="mw-gallery" className="mw-image-gallery mw-section-padding" style={edit ? { position: 'relative', scrollMarginTop: BANNER_OFFSET } : undefined}>
            <h2 className="mw-section-title">Image Gallery</h2>
            {edit ? (
              <span style={{ position: 'absolute', top: 12, right: 12 }}>
                <MiniIcon
                  label="Edit gallery"
                  icon="fa-pen"
                  onClick={() => setOpenSection((current) => (current === 'gallery' ? null : 'gallery'))}
                />
              </span>
            ) : null}
            {edit && openSection === 'gallery' ? (
              <div style={{ display: 'flex', justifyContent: 'center', marginBottom: 16 }}>
                {(edit.galleryCount ?? gallery.length) >= 10 ? (
                  <p className="text-sm text-textmain text-center" style={{ margin: 0 }}>You can add up to 10 gallery images.</p>
                ) : (
                  <button type="button" style={sectionActionStyle} onClick={() => edit.openDialog({ kind: 'gallery-add' })}>
                    Add image
                  </button>
                )}
              </div>
            ) : null}
            <div className="mw-grid-gallery">
              {gallery.map((src, i) =>
                edit && openSection === 'gallery' ? (
                  <div key={i} style={{ position: 'relative' }}>
                    <button
                      type="button"
                      className="aspect-square rounded-lg overflow-hidden border border-white/5 mw-gallery-item cursor-pointer group w-full"
                      onClick={() => setGalleryIdx(i)}
                    >
                      <img
                        src={src}
                        className="w-full h-full object-cover group-hover:scale-110 transition duration-300"
                        alt="Gallery"
                        onError={(e) => {
                          ;(e.target as HTMLImageElement).src = defaultImg
                        }}
                      />
                    </button>
                    <span style={{ position: 'absolute', top: 8, right: 8, zIndex: 2 }}>
                      <MiniIcon label="Delete image" icon="fa-trash" tone="danger" onClick={() => void edit.removeGallery(i)} />
                    </span>
                  </div>
                ) : (
                  <button
                    type="button"
                    key={i}
                    className="aspect-square rounded-lg overflow-hidden border border-white/5 mw-gallery-item cursor-pointer group"
                    onClick={() => setGalleryIdx(i)}
                  >
                    <img
                      src={src}
                      className="w-full h-full object-cover group-hover:scale-110 transition duration-300"
                      alt="Gallery"
                      onError={(e) => {
                        ;(e.target as HTMLImageElement).src = defaultImg
                      }}
                    />
                  </button>
                ),
              )}
            </div>
          </section>
        )}

        {paymentQrs.length > 0 && (
          <section id="mw-pay" className="mw-payment-qr mw-section-padding" style={edit ? { position: 'relative', scrollMarginTop: BANNER_OFFSET } : undefined}>
            <h2 className="mw-section-title">Payment Details</h2>
            {edit ? (
              <span style={{ position: 'absolute', top: 12, right: 12 }}>
                <MiniIcon label="Edit payment details" icon="fa-pen" onClick={() => edit.openDialog({ kind: 'payment' })} />
              </span>
            ) : null}
            <div className="max-w-2xl mx-auto">
              <div className={`grid grid-cols-1 sm:grid-cols-2 ${paymentQrs.length >= 3 ? 'md:grid-cols-3' : ''} gap-6`}>
                {paymentQrs.map((qr, i) => (
                  <div key={i} className="mw-card p-6 text-center">
                    <h3 className="text-heading font-medium mb-4 uppercase tracking-widest text-sm">
                      {pick(qr.label, qr.Label)}
                    </h3>
                    {pick(qr.imageDataUrl, qr.ImageDataUrl) ? (
                      <div className="bg-white p-2 rounded-lg aspect-square w-40 mx-auto overflow-hidden">
                        <img
                          src={pick(qr.imageDataUrl, qr.ImageDataUrl)}
                          alt={`${pick(qr.label, qr.Label)} QR`}
                          className="w-full h-full object-cover"
                        />
                      </div>
                    ) : null}
                    {pick(qr.upi, qr.Upi) ? (
                      <p className="text-heading font-medium text-sm mt-3 break-all">{pick(qr.upi, qr.Upi)}</p>
                    ) : null}
                  </div>
                ))}
              </div>
            </div>
          </section>
        )}

        <section id="mw-hours" className="mw-business-info mw-section-padding bg-cardbg/20" style={edit ? { position: 'relative', scrollMarginTop: BANNER_OFFSET } : undefined}>
          <EditSaveBar
            enabled={Boolean(edit)}
            editing={openSection === 'business'}
            editLabel="Edit business details"
            onEdit={() => setOpenSection((current) => (current === 'business' ? null : 'business'))}
          >
          <h2 className="mw-section-title">Business Hours</h2>
          {edit ? (
            <div style={{ display: 'flex', justifyContent: 'center', marginBottom: 16 }}>
              <button type="button" style={sectionActionStyle} onClick={() => edit.openDialog({ kind: 'company' })}>
                Business profile
              </button>
            </div>
          ) : null}
          <div className="max-w-lg mx-auto">
            {edit && openSection === 'business' ? (
              <HoursField
                draftId="business-hours"
                active
                raw={edit.company.businessHours}
                rows={hours.map((bh) => ({ days: pick(bh.days, bh.Days) || '', hours: pick(bh.hours, bh.Hours) || '' }))}
                onSave={(value) => edit.patchCompany({ businessHours: value })}
              />
            ) : (
              <div className="space-y-3 text-sm md:text-base border-t border-b border-white/10 py-6">
                {hours.map((bh, i) => (
                  <div key={i} className="flex justify-between items-center">
                    <span className="text-textmain">{pick(bh.days, bh.Days)}</span>
                    <span className="text-heading font-medium">{pick(bh.hours, bh.Hours)}</span>
                  </div>
                ))}
              </div>
            )}
            <div className="mt-8 text-center space-y-2 text-sm text-textmain">
              <p>
                <span className="text-heading">{heroName}</span>
              </p>
              {fullAddress || (edit && openSection === 'business') ? (
                <p>
                  Address: {fullAddress || 'Add address'}
                  {edit && openSection === 'business' ? (
                    <button type="button" style={{ ...sectionActionStyle, marginLeft: 8 }} onClick={() => edit.openDialog({ kind: 'address' })}>
                      Edit address
                    </button>
                  ) : null}
                </p>
              ) : null}
              {(edit && openSection === 'business') || gst ? (
                <p>
                  GST No:{' '}
                  {edit && openSection === 'business' ? (
                    <InlineText
                      draftId="business-gst"
                      value={edit.company.gstNumber}
                      placeholder="Enter GST number (optional)"
                      className="text-heading"
                      maxLength={15}
                      normalize={gstText}
                      validate={gstError}
                      onSave={(value) => edit.patchCompany({ gstNumber: gstText(value) })}
                    />
                  ) : (
                    <span className="text-heading">{gst}</span>
                  )}
                </p>
              ) : null}
              {edit && openSection === 'business' ? (
                <div className="text-primary flex items-center justify-center gap-2 mt-2">
                  <i className="fas fa-globe" />
                  <InlineText
                    draftId="business-website"
                    value={edit.company.website}
                    placeholder={SAMPLE_WEBSITE}
                    onSave={(value) => edit.patchCompany({ website: value })}
                  />
                </div>
              ) : website ? (
                <a
                  href={absHttp(website)}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="text-primary hover:underline flex items-center justify-center gap-2 mt-2"
                >
                  <i className="fas fa-globe" /> {website.replace(/^https?:\/\//i, '')}
                </a>
              ) : null}
            </div>
          </div>
          </EditSaveBar>
        </section>

        <footer className="mw-footer mw-section-padding text-center border-t border-white/5 mt-8">
          <p className="text-xs text-textmain mb-2">
            &copy; {new Date().getFullYear()} {heroName}. All rights reserved.
          </p>
          <p className="text-[10px] uppercase tracking-widest text-textmain/50">
            Powered by <span className="text-primary">ShareBizz</span>
            {demo ? ' · Demo' : ''}
          </p>
        </footer>
      </div>

      <nav className="mw-sticky-nav">
        <a href="#mw-hero" className="mw-nav-item active" data-section="mw-hero">
          <i className="fas fa-home mw-nav-icon" />
          <span>Home</span>
        </a>
        {services.length > 0 ? (
          <a href="#mw-services" className="mw-nav-item" data-section="mw-services">
            <i className="fas fa-concierge-bell mw-nav-icon" />
            <span>Serv</span>
          </a>
        ) : null}
        {offers.length > 0 ? (
          <a href="#mw-offers" className="mw-nav-item" data-section="mw-offers">
            <i className="fas fa-tags mw-nav-icon" />
            <span>Offers</span>
          </a>
        ) : null}
        {products.length > 0 ? (
          <a href="#mw-products" className="mw-nav-item" data-section="mw-products">
            <i className="fas fa-store mw-nav-icon" />
            <span>Pricing</span>
          </a>
        ) : null}
        {gallery.length > 0 ? (
          <a href="#mw-gallery" className="mw-nav-item" data-section="mw-gallery">
            <i className="fas fa-images mw-nav-icon" />
            <span>Gallery</span>
          </a>
        ) : null}
        {paymentQrs.length > 0 ? (
          <a href="#mw-pay" className="mw-nav-item hidden sm:flex" data-section="mw-pay">
            <i className="fas fa-qrcode mw-nav-icon" />
            <span>Pay</span>
          </a>
        ) : null}
      </nav>

      {whatsapp ? (
        <a
          href={`https://wa.me/${whatsapp.replace(/\D/g, '')}`}
          className="mw-whatsapp-float"
          target="_blank"
          rel="noopener noreferrer"
          aria-label="WhatsApp"
        >
          <i className="fab fa-whatsapp" />
        </a>
      ) : null}

      {videoModal ? (
        <div className="mw-video-modal open" aria-hidden="false">
          <button type="button" className="mw-video-modal-close" aria-label="Close" onClick={() => setVideoModal(null)}>
            <i className="fas fa-times" />
          </button>
          <div className="mw-video-modal-content">
            <iframe src={videoModal} title="Video" allow="autoplay; encrypted-media" allowFullScreen />
          </div>
        </div>
      ) : null}

      {galleryIdx !== null && gallery[galleryIdx] ? (
        <div className="mw-gallery-modal open" role="dialog" aria-modal="true">
          <button type="button" className="mw-gallery-modal-backdrop" aria-label="Close" onClick={() => setGalleryIdx(null)} />
          <div className="mw-gallery-modal-panel">
            <button type="button" className="mw-gallery-modal-close" onClick={() => setGalleryIdx(null)}>
              <i className="fas fa-times" />
            </button>
            <button
              type="button"
              className="mw-gallery-modal-prev"
              onClick={() => setGalleryIdx((galleryIdx - 1 + gallery.length) % gallery.length)}
            >
              <i className="fas fa-chevron-left" />
            </button>
            <button
              type="button"
              className="mw-gallery-modal-next"
              onClick={() => setGalleryIdx((galleryIdx + 1) % gallery.length)}
            >
              <i className="fas fa-chevron-right" />
            </button>
            <div className="mw-gallery-modal-stage">
              <img src={gallery[galleryIdx]} alt="Gallery" className="mw-gallery-modal-img" />
            </div>
            <div className="mw-gallery-modal-counter">
              {galleryIdx + 1} / {gallery.length}
            </div>
          </div>
        </div>
      ) : null}
    </>
  )
}
