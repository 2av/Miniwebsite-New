import { useCallback, useEffect, useMemo, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { apiGet } from '@/shared/api/client'
import { assetUrl, publicUrl } from '@/lib/utils'

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
  ctaText?: string
  CtaText?: string
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
  if (document.getElementById(id)) return
  const link = document.createElement('link')
  link.id = id
  link.rel = 'stylesheet'
  link.href = href
  document.head.appendChild(link)
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

function absHttp(url: string) {
  const u = url.trim()
  if (!u) return ''
  return /^https?:\/\//i.test(u) ? u : `https://${u}`
}

/** Public digi-card viewer — mirrors n.php sections + theme CSS. */
export function PublicCardPage({ demo = false }: { demo?: boolean }) {
  const { slug = '' } = useParams()
  const [viewer, setViewer] = useState<PublicCardViewer | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [stylesReady, setStylesReady] = useState(false)
  const [videoModal, setVideoModal] = useState<string | null>(null)
  const [galleryIdx, setGalleryIdx] = useState<number | null>(null)
  const [waShare, setWaShare] = useState('')

  useEffect(() => {
    let cancelled = false
    ;(async () => {
      try {
        const data = await apiGet<PublicCardViewer>(`/api/v1/public/cards/${encodeURIComponent(slug)}`)
        if (!cancelled) setViewer(data)
      } catch (e) {
        if (!cancelled) setError(e instanceof Error ? e.message : 'Failed to load card')
      }
    })()
    return () => {
      cancelled = true
    }
  }, [slug])

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
  const location = pick(card.location, card.Location) || ''
  const direction = pick(card.googleDirection, card.GoogleDirection) || ''
  const website = pick(card.website, card.Website) || ''
  const about = pick(card.about, card.About) || ''
  const category = pick(card.primaryCategoryTitle, card.PrimaryCategoryTitle) || ''
  const cover = pick(card.heroCoverUrl, card.HeroCoverUrl) || defaultImg
  const logo = pick(card.logoUrl, card.LogoUrl) || defaultImg
  const gst = pick(card.gstNumber, card.GstNumber) || ''
  const fullAddress = pick(card.fullAddress, card.FullAddress) || location
  const qrSrc = `https://api.qrserver.com/v1/create-qr-code/?size=150x150&data=${encodeURIComponent(shareUrl)}`

  return (
    <>
      <div className="app-container">
        <section id="mw-hero" className="mw-hero relative">
          <div className="h-64 md:h-80 w-full overflow-hidden relative">
            <img
              src={cover}
              alt="Cover"
              className="w-full h-full object-cover opacity-60"
              onError={(e) => {
                ;(e.target as HTMLImageElement).src = defaultImg
              }}
            />
            <div className="absolute inset-0 bg-gradient-to-t from-bgbase to-transparent" />
          </div>
          <div className="mw-section-padding relative -mt-24 text-center z-10 flex flex-col items-center">
            <div className="w-32 h-32 rounded-full border-4 border-bgbase shadow-card overflow-hidden mb-4">
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
          <section className="mw-social-links pb-6 flex justify-center gap-5">
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
          <section className="mw-business-intro mw-section-padding text-center">
            <p className="text-sm md:text-base leading-relaxed max-w-3xl mx-auto whitespace-pre-line">{about}</p>
          </section>
        ) : null}

        {(email || phone || location || direction) && (
          <section className="mw-action-grid mw-section-padding">
            <h2 className="mw-section-title">Contact</h2>
            <div className="grid grid-cols-2 gap-4 max-w-4xl mx-auto">
              {email ? (
                <div className="mw-card p-4 flex flex-col gap-2">
                  <span className="mw-contact-icon inline-flex w-10 h-10 items-center justify-center rounded-theme text-primary">
                    <i className="fas fa-envelope text-xl" />
                  </span>
                  <h3 className="text-xs uppercase tracking-wider text-textmain mt-2">Email Address</h3>
                  <p className="text-heading font-medium text-sm truncate">{email}</p>
                </div>
              ) : null}
              {phone ? (
                <div className="mw-card p-4 flex flex-col gap-2">
                  <span className="mw-contact-icon inline-flex w-10 h-10 items-center justify-center rounded-theme text-primary">
                    <i className="fas fa-phone-alt text-xl" />
                  </span>
                  <h3 className="text-xs uppercase tracking-wider text-textmain mt-2">Phone Number</h3>
                  <p className="text-heading font-medium text-sm">{phone}</p>
                </div>
              ) : null}
              {location ? (
                <div className="mw-card p-4 flex flex-col gap-2">
                  <span className="mw-contact-icon inline-flex w-10 h-10 items-center justify-center rounded-theme text-primary">
                    <i className="fas fa-map-marker-alt text-xl" />
                  </span>
                  <h3 className="text-xs uppercase tracking-wider text-textmain mt-2">Location</h3>
                  <p className="text-heading font-medium text-sm">{location}</p>
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
          </section>
        )}

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
          <section id="mw-services" className="mw-services mw-section-padding bg-cardbg/30">
            <h2 className="mw-section-title">Our Services</h2>
            <div className="mw-grid-services">
              {services.map((svc, i) => (
                <div key={i} className="mw-card mw-offer-card mw-service-card bg-cardbg rounded-theme overflow-hidden relative">
                  <div className="mw-service-image-wrap">
                    <img
                      src={pick(svc.imageUrl, svc.ImageUrl) || defaultImg}
                      alt={pick(svc.name, svc.Name) || ''}
                      onError={(e) => {
                        ;(e.target as HTMLImageElement).src = defaultImg
                      }}
                    />
                  </div>
                  <div className="p-5">
                    <h3 className="text-heading font-semibold text-lg mb-1">{pick(svc.name, svc.Name)}</h3>
                    <p className="mw-service-desc-preview text-sm text-textmain line-clamp-3">
                      {pick(svc.description, svc.Description) || 'Contact us for details.'}
                    </p>
                  </div>
                </div>
              ))}
            </div>
          </section>
        )}

        {offers.length > 0 && (
          <section id="mw-offers" className="mw-offers mw-section-padding">
            <h2 className="mw-section-title">Special Offers</h2>
            <div className="mw-grid-offers">
              {offers.map((o, i) => (
                <div key={i} className="mw-card mw-offer-card bg-cardbg rounded-theme overflow-hidden">
                  <img
                    src={pick(o.imageUrl, o.ImageUrl) || defaultImg}
                    alt={pick(o.title, o.Title) || ''}
                    className="w-full aspect-video object-cover"
                    onError={(e) => {
                      ;(e.target as HTMLImageElement).src = defaultImg
                    }}
                  />
                  <div className="p-5">
                    {pick(o.badge, o.Badge) ? (
                      <span className="text-xs uppercase tracking-wider text-primary">{pick(o.badge, o.Badge)}</span>
                    ) : null}
                    <h3 className="text-heading font-semibold text-lg">{pick(o.title, o.Title)}</h3>
                    <p className="text-sm text-textmain mt-1">{pick(o.description, o.Description)}</p>
                    {(pick(o.discountPercentage, o.DiscountPercentage) || 0) > 0 ? (
                      <p className="text-primary font-semibold mt-2">{pick(o.discountPercentage, o.DiscountPercentage)}% OFF</p>
                    ) : null}
                  </div>
                </div>
              ))}
            </div>
          </section>
        )}

        {products.length > 0 && (
          <section id="mw-products" className="mw-products mw-section-padding bg-cardbg/20">
            <h2 className="mw-section-title">Products &amp; Pricing</h2>
            <div className="mw-grid-products">
              {products.map((p, i) => (
                <div key={i} className="mw-card p-4">
                  <img
                    src={pick(p.imageUrl, p.ImageUrl) || defaultImg}
                    alt={pick(p.name, p.Name) || ''}
                    className="w-full aspect-square object-cover rounded-theme mb-3"
                    onError={(e) => {
                      ;(e.target as HTMLImageElement).src = defaultImg
                    }}
                  />
                  <h3 className="text-heading font-semibold">{pick(p.name, p.Name)}</h3>
                  <p className="text-sm text-textmain line-clamp-2">{pick(p.description, p.Description)}</p>
                  <p className="text-heading font-bold mt-2">
                    ₹{pick(p.sellingPrice, p.SellingPrice) ?? 0}
                    {(pick(p.mrp, p.Mrp) || 0) > (pick(p.sellingPrice, p.SellingPrice) || 0) ? (
                      <span className="text-textmain text-sm line-through ml-2 font-normal">₹{pick(p.mrp, p.Mrp)}</span>
                    ) : null}
                  </p>
                </div>
              ))}
            </div>
          </section>
        )}

        {videos.length > 0 && (
          <section className="mw-video-gallery mw-section-padding bg-cardbg/20">
            <h2 className="mw-section-title">Videos</h2>
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
          <section id="mw-gallery" className="mw-image-gallery mw-section-padding">
            <h2 className="mw-section-title">Image Gallery</h2>
            <div className="mw-grid-gallery">
              {gallery.map((src, i) => (
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
              ))}
            </div>
          </section>
        )}

        {paymentQrs.length > 0 && (
          <section id="mw-pay" className="mw-payment-qr mw-section-padding">
            <h2 className="mw-section-title">QR Code</h2>
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
                          className="w-full h-full object-contain"
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

        <section className="mw-business-info mw-section-padding bg-cardbg/20">
          <h2 className="mw-section-title">Business Hours</h2>
          <div className="max-w-lg mx-auto">
            <div className="space-y-3 text-sm md:text-base border-t border-b border-white/10 py-6">
              {hours.map((bh, i) => (
                <div key={i} className="flex justify-between items-center">
                  <span className="text-textmain">{pick(bh.days, bh.Days)}</span>
                  <span className="text-heading font-medium">{pick(bh.hours, bh.Hours)}</span>
                </div>
              ))}
            </div>
            <div className="mt-8 text-center space-y-2 text-sm text-textmain">
              <p>
                <span className="text-heading">{heroName}</span>
              </p>
              {fullAddress ? <p>Address: {fullAddress}</p> : null}
              {gst ? (
                <p>
                  GST No: <span className="text-heading">{gst}</span>
                </p>
              ) : null}
              {website ? (
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
