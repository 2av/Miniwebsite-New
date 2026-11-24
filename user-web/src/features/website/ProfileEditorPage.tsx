import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { useOutletContext } from 'react-router-dom'
import { ApiError } from '@/shared/api/client'
import { pickVal, websiteApi } from '@/features/website/websiteApi'
import type { WebsiteOutletContext } from '@/features/website/WebsiteBuilderLayout'
import { PublicCardPage } from '@/features/public-card/PublicCardPage'
import {
  ProfileEditProvider,
  type CompanyForm,
  type DialogRequest,
  type OwnedGallery,
  type OwnedOffer,
  type OwnedProduct,
  type OwnedService,
  type ProfileEditApi,
} from '@/features/website/profileEditContext'
import { CompanyProfileDialog } from '@/features/website/CompanyProfileDialog'
import { EditorActions, EditorDialog, EditorField, MiniIcon } from '@/features/website/profileEditorUi'
import { ABOUT_PLACEHOLDER, aboutError, normalizeSocialLink, pinDigits, pincodeError, SOCIAL_LINK_ERROR, SOCIAL_LINK_PLACEHOLDER, socialLinkError, UPI_ERROR, UPI_PLACEHOLDER, upiError, VIDEO_LINK_ERROR, VIDEO_LINK_LIMIT, VIDEO_LINK_PLACEHOLDER, videoLinkError } from '@/features/website/profileFieldRules'

const COMPANY_KEYS = [
  'firstName',
  'lastName',
  'positionPrimary',
  'positionSecondary',
  'position',
  'contact',
  'contact2',
  'whatsapp',
  'gstNumber',
  'address',
  'address2',
  'city',
  'state',
  'pincode',
  'country',
  'email',
  'website',
  'location',
  'compEstDate',
  'aboutUs',
  'businessHours',
  'businessProfileType',
  'businessType',
  'businessOperationArea',
  'businessOperationLocations',
] as const

type SocialForm = {
  fb: string
  twitter: string
  instagram: string
  linkedin: string
  youtube: string
  pinterest: string
  google: string
}

type PaymentForm = {
  paytm: string
  googlePay: string
  phonePay: string
  paytmQr: string
  googlePayQr: string
  phonePayQr: string
  paytmQrUrl: string
  googlePayQrUrl: string
  phonePayQrUrl: string
  accountNo: string
  ifsc: string
  acName: string
  bankName: string
  acType: string
}

type OwnerData = {
  slug: string
  company: CompanyForm
  services: OwnedService[]
  products: OwnedProduct[]
  offers: OwnedOffer[]
  gallery: OwnedGallery[]
  videos: string[]
  social: SocialForm
  payment: PaymentForm
  logoUrl: string
  heroUrl: string
  appointmentBookingEnabled: boolean
}

function textOf(value: unknown) {
  return value == null ? '' : String(value)
}

function field(raw: Record<string, unknown>, key: string) {
  const pascal = key.charAt(0).toUpperCase() + key.slice(1)
  return textOf(pickVal(raw, key, pascal))
}

function boolField(raw: Record<string, unknown>, key: string) {
  const pascal = key.charAt(0).toUpperCase() + key.slice(1)
  const value = pickVal(raw, key, pascal)
  return value === true || value === 1 || value === 'true' || value === '1'
}

function dateField(raw: Record<string, unknown>, key: string) {
  const pascal = key.charAt(0).toUpperCase() + key.slice(1)
  const value = pickVal(raw, key, pascal)
  if (value == null || value === '') return null
  return typeof value === 'string' ? value : null
}

function intOrNull(value: unknown) {
  if (value == null || value === '') return null
  const n = Number(value)
  return Number.isFinite(n) ? n : null
}

function asRecords(raw: unknown) {
  return Array.isArray(raw) ? raw.map((item) => (item ?? {}) as Record<string, unknown>) : []
}

function companyFrom(raw: Record<string, unknown>): CompanyForm {
  const next = {} as CompanyForm
  for (const key of COMPANY_KEYS) next[key] = field(raw, key)
  return next
}

function readService(raw: Record<string, unknown>): OwnedService {
  return {
    id: Number(raw.id ?? raw.Id),
    productName: field(raw, 'productName'),
    productDescription: field(raw, 'productDescription'),
    productImage: field(raw, 'productImage'),
    productCategory: intOrNull(raw.productCategory ?? raw.ProductCategory),
    categorySource: field(raw, 'categorySource'),
    displayOrder: Number(raw.displayOrder ?? raw.DisplayOrder ?? 0) || 0,
  }
}

function readProduct(raw: Record<string, unknown>): OwnedProduct {
  return {
    ...readService(raw),
    mrp: Number(raw.mrp ?? raw.Mrp ?? 0) || 0,
    sellingPrice: Number(raw.sellingPrice ?? raw.SellingPrice ?? 0) || 0,
    priceType: field(raw, 'priceType') || 'Fixed',
    pricingUnit: field(raw, 'pricingUnit'),
    ctaText: field(raw, 'ctaText') || 'Buy Now',
  }
}

function readOffer(raw: Record<string, unknown>): OwnedOffer {
  return {
    id: Number(raw.id ?? raw.Id),
    offerTitle: field(raw, 'offerTitle'),
    offerDescription: field(raw, 'offerDescription'),
    offerImage: field(raw, 'offerImage'),
    badge: field(raw, 'badge') || 'Offer',
    discountPercentage: Number(raw.discountPercentage ?? raw.DiscountPercentage ?? 0) || 0,
    status: field(raw, 'status') || 'Active',
    displayOrder: Number(raw.displayOrder ?? raw.DisplayOrder ?? 0) || 0,
    startDate: dateField(raw, 'startDate'),
    startTime: dateField(raw, 'startTime'),
    endDate: dateField(raw, 'endDate'),
    endTime: dateField(raw, 'endTime'),
  }
}

function readGallery(raw: Record<string, unknown>): OwnedGallery {
  return {
    id: Number(raw.id ?? raw.Id),
    galleryImage: field(raw, 'galleryImage'),
    galleryImageUrl: field(raw, 'galleryImageUrl'),
    displayOrder: Number(raw.displayOrder ?? raw.DisplayOrder ?? 0) || 0,
  }
}

function withName<T extends { id: number; productName: string }>(items: T[]) {
  return items.filter((item) => item.id > 0 && item.productName.trim().length > 0)
}

function visibleOffers(items: OwnedOffer[]) {
  return items.filter((item) => {
    if (item.id <= 0) return false
    const status = item.status.trim()
    if (!/^active$/i.test(status) && !/^inactive$/i.test(status)) return false
    if (!item.endDate) return true
    const end = new Date(`${item.endDate}T${item.endTime || '23:59:59'}`)
    if (Number.isNaN(end.getTime())) return true
    return end.getTime() >= Date.now()
  })
}

function visibleGallery(items: OwnedGallery[]) {
  return items.filter((item) => item.id > 0 && item.galleryImageUrl.trim().length > 0)
}

function messageOf(error: unknown) {
  if (error instanceof ApiError || error instanceof Error) return error.message
  return 'Save failed'
}

type ThemeChoice = { number: number; name: string; cssValue: string; previewImageUrl: string | null }

function ThemeDrawer({
  open,
  cardId,
  email,
  onClose,
  onApplied,
}: {
  open: boolean
  cardId: number
  email: string
  onClose: () => void
  onApplied: () => Promise<void>
}) {
  const [themes, setThemes] = useState<ThemeChoice[]>([])
  const [selected, setSelected] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState<string | null>(null)

  useEffect(() => {
    if (!open) return
    let cancelled = false
    ;(async () => {
      try {
        const [catalog, current] = await Promise.all([websiteApi.listThemes(), websiteApi.getTheme(cardId, email)])
        if (cancelled) return
        const list = Array.isArray(catalog) ? catalog : []
        setThemes(
          list.map((item) => {
            const row = (item ?? {}) as Record<string, unknown>
            return {
              number: Number(row.number ?? row.Number ?? 0),
              name: String(row.name ?? row.Name ?? 'Theme'),
              cssValue: String(row.cssValue ?? row.CssValue ?? ''),
              previewImageUrl: (row.previewImageUrl ?? row.PreviewImageUrl ?? null) as string | null,
            }
          }),
        )
        setSelected(String(pickVal(current, 'dCss', 'DCss') ?? ''))
        setError(null)
      } catch (e) {
        if (!cancelled) setError(messageOf(e))
      }
    })()
    return () => {
      cancelled = true
    }
  }, [open, cardId, email])

  useEffect(() => {
    if (!open) return
    const onKey = (event: KeyboardEvent) => {
      if (event.key === 'Escape') onClose()
    }
    document.body.style.overflow = 'hidden'
    window.addEventListener('keydown', onKey)
    return () => {
      document.body.style.overflow = ''
      window.removeEventListener('keydown', onKey)
    }
  }, [open, onClose])

  async function choose(cssValue: string) {
    if (!cssValue || busy) return
    setSelected(cssValue)
    setBusy(cssValue)
    setError(null)
    try {
      await websiteApi.putTheme(cardId, email, cssValue)
      await onApplied()
    } catch (e) {
      setError(messageOf(e))
    } finally {
      setBusy(null)
    }
  }

  if (!open) return null

  return (
    <>
      <button type="button" aria-label="Close themes" onClick={onClose} style={themeBackdropStyle} />
      <aside role="dialog" aria-modal="true" aria-label="Change theme" style={themeDrawerStyle}>
        <div style={{ height: 4, background: 'linear-gradient(90deg, #6C2EFF, #FF38BD, #FF8A00)' }} />
        <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 12, padding: '16px 18px', borderBottom: '1px solid #e5e7eb' }}>
          <h2 style={{ margin: 0, fontSize: 18, fontWeight: 650, color: '#1f2937' }}>Change theme</h2>
          <button type="button" aria-label="Close" onClick={onClose} style={themeCloseStyle}>
            <i className="fas fa-times" />
          </button>
        </div>
        <p style={{ margin: '16px 18px 12px', color: '#64748b', fontSize: 14, lineHeight: 1.5 }}>
          Pick a theme. The profile updates on this page.
        </p>
        {error ? <p style={{ margin: '0 18px 12px', color: '#b42318', fontSize: 13 }}>{error}</p> : null}
        <div style={{ overflowY: 'auto', padding: '0 18px 24px', display: 'grid', gap: 12 }}>
          {themes.map((theme) => {
            const active = selected === theme.cssValue
            return (
              <button
                key={theme.number || theme.cssValue}
                type="button"
                disabled={busy !== null}
                onClick={() => void choose(theme.cssValue)}
                style={{
                  display: 'block',
                  width: '100%',
                  textAlign: 'left',
                  padding: 8,
                  borderRadius: 16,
                  border: active ? '2px solid #6C2EFF' : '1px solid #e5e7eb',
                  background: active ? '#f3f0ff' : '#fff',
                  color: '#1f2937',
                  cursor: busy ? 'wait' : 'pointer',
                  fontFamily: 'inherit',
                }}
              >
                {theme.previewImageUrl ? (
                  <img
                    src={theme.previewImageUrl}
                    alt=""
                    style={{ width: '100%', height: 132, objectFit: 'cover', borderRadius: 12, display: 'block' }}
                  />
                ) : (
                  <div
                    style={{
                      height: 132,
                      borderRadius: 12,
                      display: 'grid',
                      placeItems: 'center',
                      background: '#f8fafc',
                      color: '#6C2EFF',
                      fontSize: 28,
                      fontWeight: 700,
                    }}
                  >
                    {theme.number}
                  </div>
                )}
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '10px 6px 4px' }}>
                  <span style={{ fontWeight: 700 }}>{theme.name}</span>
                  <span style={{ fontSize: 12, fontWeight: 650, color: active ? '#6C2EFF' : '#64748b' }}>
                    {busy === theme.cssValue ? 'Applying…' : active ? 'Selected' : 'Select'}
                  </span>
                </div>
              </button>
            )
          })}
        </div>
      </aside>
    </>
  )
}

const themeBackdropStyle = {
  position: 'fixed',
  inset: 0,
  zIndex: 88,
  border: 'none',
  padding: 0,
  background: 'rgba(0, 0, 128, 0.45)',
  cursor: 'pointer',
} as const

const themeDrawerStyle = {
  position: 'fixed',
  top: 0,
  left: 0,
  bottom: 0,
  width: 'min(380px, 90vw)',
  zIndex: 89,
  display: 'flex',
  flexDirection: 'column',
  background: '#fff',
  color: '#1f2937',
  boxShadow: '16px 0 40px rgba(0, 0, 20, 0.18)',
  fontFamily: 'Poppins, Inter, sans-serif',
} as const

const themeCloseStyle = {
  width: 36,
  height: 36,
  borderRadius: 10,
  border: '1px solid #e5e7eb',
  background: '#fff',
  color: '#1f2937',
  cursor: 'pointer',
} as const

function imageProblem(file: File) {
  const allowed = ['image/jpeg', 'image/jpg', 'image/png', 'image/gif', 'image/webp']
  const extOk = /\.(jpe?g|png|gif|webp)$/i.test(file.name)
  if (!allowed.includes(file.type) && !(file.type === '' && extOk) && !file.type.startsWith('image/')) {
    return 'Use a JPG, PNG, GIF, or WebP image.'
  }
  if (file.size > 2_500_000) return 'Image is too large (max 2.5MB).'
  return null
}

function imageKeyFrom(data: Record<string, unknown> | undefined) {
  return String(data?.imageKey ?? data?.ImageKey ?? '')
}

function squareImageProblem(file: File) {
  return new Promise<string | null>((resolve) => {
    const url = URL.createObjectURL(file)
    const image = new Image()
    image.onload = () => {
      URL.revokeObjectURL(url)
      resolve(image.naturalWidth > 0 && image.naturalWidth === image.naturalHeight ? null : 'QR image must be square (1:1).')
    }
    image.onerror = () => {
      URL.revokeObjectURL(url)
      resolve('Use a JPG, PNG, GIF, or WebP image.')
    }
    image.src = url
  })
}

export function ProfileEditorPage() {
  const { cardId, email } = useOutletContext<WebsiteOutletContext>()
  const [data, setData] = useState<OwnerData | null>(null)
  const [revision, setRevision] = useState(0)
  const [error, setError] = useState<string | null>(null)
  const [notice, setNotice] = useState<string | null>(null)
  const [dialog, setDialog] = useState<DialogRequest | null>(null)
  const [themeOpen, setThemeOpen] = useState(false)
  const dataRef = useRef<OwnerData | null>(null)
  const chain = useRef(Promise.resolve())
  dataRef.current = data

  const load = useCallback(async () => {
    if (!cardId) return
    const [business, companyRaw, serviceRaw, productRaw, offerRaw, galleryRaw, videoRaw, socialRaw, paymentRaw] =
      await Promise.all([
        websiteApi.getBusiness(cardId, email),
        websiteApi.getCompany(cardId, email),
        websiteApi.listServices(cardId, email),
        websiteApi.listProducts(cardId, email),
        websiteApi.listOffers(cardId, email),
        websiteApi.listGallery(cardId, email),
        websiteApi.getVideos(cardId, email),
        websiteApi.getSocial(cardId, email),
        websiteApi.getPayment(cardId, email),
      ])
    const urls = ((videoRaw.urls ?? videoRaw.Urls) as unknown[]) || []
    const next: OwnerData = {
      slug: field(business, 'slug') || String(cardId),
      company: companyFrom(companyRaw),
      services: asRecords(serviceRaw).map(readService),
      products: asRecords(productRaw).map(readProduct),
      offers: asRecords(offerRaw).map(readOffer),
      gallery: asRecords(galleryRaw).map(readGallery),
      videos: urls.map((url) => textOf(url)),
      social: {
        fb: field(socialRaw, 'fb'),
        twitter: field(socialRaw, 'twitter'),
        instagram: field(socialRaw, 'instagram'),
        linkedin: field(socialRaw, 'linkedin'),
        youtube: field(socialRaw, 'youtube'),
        pinterest: field(socialRaw, 'pinterest'),
        google: field(socialRaw, 'google'),
      },
      payment: {
        paytm: field(paymentRaw, 'paytm'),
        googlePay: field(paymentRaw, 'googlePay'),
        phonePay: field(paymentRaw, 'phonePay'),
        paytmQr: field(paymentRaw, 'paytmQr'),
        googlePayQr: field(paymentRaw, 'googlePayQr'),
        phonePayQr: field(paymentRaw, 'phonePayQr'),
        paytmQrUrl: field(paymentRaw, 'paytmQrUrl'),
        googlePayQrUrl: field(paymentRaw, 'googlePayQrUrl'),
        phonePayQrUrl: field(paymentRaw, 'phonePayQrUrl'),
        accountNo: field(paymentRaw, 'accountNo'),
        ifsc: field(paymentRaw, 'ifsc'),
        acName: field(paymentRaw, 'acName'),
        bankName: field(paymentRaw, 'bankName'),
        acType: field(paymentRaw, 'acType'),
      },
      logoUrl: field(companyRaw, 'logoUrl'),
      heroUrl: field(companyRaw, 'heroImageUrl'),
      appointmentBookingEnabled: boolField(companyRaw, 'appointmentBookingEnabled'),
    }
    dataRef.current = next
    setData(next)
  }, [cardId, email])

  useEffect(() => {
    if (!cardId) return
    let cancelled = false
    ;(async () => {
      try {
        await load()
      } catch (e) {
        if (!cancelled) setError(messageOf(e))
      }
    })()
    return () => {
      cancelled = true
    }
  }, [cardId, load])

  useEffect(() => {
    if (!notice) return
    const timer = window.setTimeout(() => setNotice(null), 3200)
    return () => window.clearTimeout(timer)
  }, [notice])

  function runExclusive<T>(task: () => Promise<T>) {
    const run = chain.current.then(task, task)
    chain.current = run.then(
      () => undefined,
      () => undefined,
    )
    return run
  }

  async function refresh() {
    await load()
    setRevision((n) => n + 1)
  }

  const api = useMemo<ProfileEditApi | null>(() => {
    if (!cardId || !data) return null
    const missing = (): never => {
      throw new Error('Could not match this item. Reload the page.')
    }
    return {
      revision,
      company: data.company,
      offerCount: data.offers.length,
      productCount: data.products.length,
      galleryCount: data.gallery.length,
      openDialog: setDialog,
      openThemeDrawer: () => setThemeOpen(true),
      uploadCompanyImage: (kind, file, onProgress) =>
        runExclusive(async () => {
          await websiteApi.uploadCompanyImage(cardId, email, kind, file, onProgress)
          await refresh()
        }),
      patchCompany: (patch) =>
        runExclusive(async () => {
          const current = dataRef.current
          if (!current) return
          const next = { ...current.company, ...patch }
          await websiteApi.putCompany(cardId, email, next)
          await refresh()
        }),
      patchService: (index, patch) =>
        runExclusive(async () => {
          const row = withName(dataRef.current?.services || [])[index]
          if (!row) missing()
          await websiteApi.updateService(cardId, row.id, email, {
            productName: patch.productName ?? row.productName,
            productDescription: patch.productDescription ?? row.productDescription,
            productImage: row.productImage || null,
            productCategory: row.productCategory,
            categorySource: row.categorySource || null,
            displayOrder: row.displayOrder,
          })
          await refresh()
        }),
      patchProduct: (index, patch) =>
        runExclusive(async () => {
          const row = withName(dataRef.current?.products || [])[index]
          if (!row) missing()
          await websiteApi.updateProduct(cardId, row.id, email, {
            productName: patch.productName ?? row.productName,
            productDescription: patch.productDescription ?? row.productDescription,
            productImage: row.productImage || null,
            productCategory: row.productCategory,
            categorySource: row.categorySource || null,
            mrp: patch.mrp ?? row.mrp,
            sellingPrice: patch.sellingPrice ?? row.sellingPrice,
            priceType: row.priceType || null,
            pricingUnit: row.pricingUnit,
            ctaText: row.ctaText,
            displayOrder: row.displayOrder,
          })
          await refresh()
        }),
      patchOffer: (index, patch) =>
        runExclusive(async () => {
          const row = visibleOffers(dataRef.current?.offers || [])[index]
          if (!row) missing()
          await websiteApi.updateOffer(cardId, row.id, email, {
            offerTitle: patch.offerTitle ?? row.offerTitle,
            offerDescription: patch.offerDescription ?? row.offerDescription,
            offerImage: row.offerImage || null,
            badge: (patch.badge ?? row.badge) || null,
            discountPercentage: patch.discountPercentage ?? row.discountPercentage,
            status: row.status,
            displayOrder: row.displayOrder,
            startDate: row.startDate,
            startTime: row.startTime,
            endDate: row.endDate,
            endTime: row.endTime,
          })
          await refresh()
        }),
      removeService: async (index) => {
        const row = withName(dataRef.current?.services || [])[index]
        if (!row) return setNotice('Could not match this service.')
        if (!window.confirm(`Delete ${row.productName}?`)) return
        try {
          await websiteApi.deleteService(cardId, row.id, email)
          await refresh()
        } catch (e) {
          setNotice(messageOf(e))
        }
      },
      removeProduct: async (index) => {
        const row = withName(dataRef.current?.products || [])[index]
        if (!row) return setNotice('Could not match this product.')
        if (!window.confirm(`Delete ${row.productName}?`)) return
        try {
          await websiteApi.deleteProduct(cardId, row.id, email)
          await refresh()
        } catch (e) {
          setNotice(messageOf(e))
        }
      },
      removeOffer: async (index) => {
        const row = visibleOffers(dataRef.current?.offers || [])[index]
        if (!row) return setNotice('Could not match this offer.')
        if (!window.confirm(`Delete ${row.offerTitle}?`)) return
        try {
          await websiteApi.deleteOffer(cardId, row.id, email)
          await refresh()
        } catch (e) {
          setNotice(messageOf(e))
        }
      },
      removeGallery: async (index) => {
        const row = visibleGallery(dataRef.current?.gallery || [])[index]
        if (!row) return setNotice('Could not match this image.')
        if (!window.confirm('Delete this image?')) return
        try {
          await websiteApi.deleteGallery(cardId, row.id, email)
          await refresh()
        } catch (e) {
          setNotice(messageOf(e))
        }
      },
    }
    // refresh closes over load, which is stable for cardId/email
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [cardId, email, data, revision])

  if (!cardId) return null
  if (error) {
    return (
      <div style={{ minHeight: '100vh', display: 'grid', placeItems: 'center', padding: 24, fontFamily: 'Poppins, Inter, sans-serif' }}>
        <p>{error}</p>
      </div>
    )
  }
  if (!data || !api) {
    return (
      <div style={{ minHeight: '100vh', display: 'grid', placeItems: 'center', padding: 24, color: '#64748b' }}>
        Loading profile…
      </div>
    )
  }

  return (
    <ProfileEditProvider value={api}>
      <PublicCardPage forcedSlug={data.slug} />
      <ThemeDrawer
        open={themeOpen}
        cardId={cardId}
        email={email}
        onClose={() => setThemeOpen(false)}
        onApplied={refresh}
      />
      {notice ? (
        <div
          role="status"
          style={{
            position: 'fixed',
            bottom: 24,
            left: '50%',
            transform: 'translateX(-50%)',
            zIndex: 70,
            background: '#111827',
            color: '#fff',
            borderRadius: 10,
            padding: '10px 14px',
            fontFamily: 'Poppins, Inter, sans-serif',
            fontSize: 14,
          }}
        >
          {notice}
        </div>
      ) : null}
      {dialog ? (
        <EditorDialogs
          request={dialog}
          cardId={cardId}
          email={email}
          data={data}
          onClose={() => setDialog(null)}
          onSaved={async () => {
            setDialog(null)
            await refresh()
          }}
          onRefresh={refresh}
          onError={(message) => setNotice(message)}
        />
      ) : null}
    </ProfileEditProvider>
  )
}

function EditorDialogs({
  request,
  cardId,
  email,
  data,
  onClose,
  onSaved,
  onRefresh,
  onError,
}: {
  request: DialogRequest
  cardId: number
  email: string
  data: OwnerData
  onClose: () => void
  onSaved: () => Promise<void>
  onRefresh: () => Promise<void>
  onError: (message: string) => void
}) {
  const [busy, setBusy] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)

  async function run(task: () => Promise<void>, close = true) {
    setBusy(true)
    setFormError(null)
    try {
      await task()
      if (close) await onSaved()
      else await onRefresh()
    } catch (e) {
      const message = messageOf(e)
      setFormError(message)
      onError(message)
    } finally {
      setBusy(false)
    }
  }

  if (request.kind === 'images') {
    return (
      <HomeImagesDialog
        heroUrl={data.heroUrl}
        logoUrl={data.logoUrl}
        busy={busy}
        error={formError}
        onClose={onClose}
        onInvalid={setFormError}
        onUpload={async (kind, file, onProgress) => {
          let savedUrl = ''
          await run(async () => {
            const uploaded = await websiteApi.uploadCompanyImage(cardId, email, kind, file, onProgress)
            const key = kind === 'hero' ? 'heroImageUrl' : 'logoUrl'
            const pascal = kind === 'hero' ? 'HeroImageUrl' : 'LogoUrl'
            savedUrl = String(pickVal(uploaded.data ?? {}, key, pascal) ?? '')
            if (!savedUrl) throw new Error('Upload did not return an image.')
          }, false)
          if (!savedUrl) throw new Error('Upload failed.')
          return savedUrl
        }}
        onRemove={(kind) => run(() => websiteApi.deleteCompanyImage(cardId, email, kind).then(() => undefined), false)}
      />
    )
  }

  if (request.kind === 'logo' || request.kind === 'hero') {
    return (
      <EditorDialog title={request.kind === 'logo' ? 'Logo' : 'Cover image'} onClose={onClose}>
        {formError ? <p style={{ color: '#b42318' }}>{formError}</p> : null}
        <EditorField label="Upload image">
          <input
            type="file"
            accept=".jpg,.jpeg,.png,.gif,.webp,image/*"
            disabled={busy}
            onChange={(e) => {
              const file = e.target.files?.[0]
              e.target.value = ''
              if (!file) return
              const problem = imageProblem(file)
              if (problem) {
                setFormError(problem)
                return
              }
              void run(() => websiteApi.uploadCompanyImage(cardId, email, request.kind as 'logo' | 'hero', file).then(() => undefined))
            }}
          />
        </EditorField>
        <EditorActions>
          <button
            type="button"
            className="mw-editor-ghost"
            disabled={busy}
            onClick={() => void run(() => websiteApi.deleteCompanyImage(cardId, email, request.kind as 'logo' | 'hero').then(() => undefined))}
          >
            Remove
          </button>
        </EditorActions>
      </EditorDialog>
    )
  }

  if (request.kind === 'service-image' || request.kind === 'product-image' || request.kind === 'offer-image') {
    const index = request.index
    return (
      <EditorDialog title="Change image" onClose={onClose}>
        {formError ? <p style={{ color: '#b42318' }}>{formError}</p> : null}
        <EditorField label="Upload image">
          <input
            type="file"
            accept=".jpg,.jpeg,.png,.gif,.webp,image/*"
            disabled={busy}
            onChange={(e) => {
              const file = e.target.files?.[0]
              e.target.value = ''
              if (!file) return
              const problem = imageProblem(file)
              if (problem) {
                setFormError(problem)
                return
              }
              void run(async () => {
                if (request.kind === 'service-image') {
                  const row = withName(data.services)[index]
                  if (!row) throw new Error('Could not match this service.')
                  const uploaded = await websiteApi.uploadServiceImage(cardId, email, file)
                  const key = imageKeyFrom(uploaded.data)
                  if (!key) throw new Error('Upload did not return an image.')
                  await websiteApi.updateService(cardId, row.id, email, {
                    productName: row.productName,
                    productDescription: row.productDescription,
                    productImage: key,
                    productCategory: row.productCategory,
                    categorySource: row.categorySource || null,
                    displayOrder: row.displayOrder,
                  })
                  return
                }
                if (request.kind === 'product-image') {
                  const row = withName(data.products)[index]
                  if (!row) throw new Error('Could not match this product.')
                  const uploaded = await websiteApi.uploadCardImage(cardId, email, 'product', file)
                  const key = imageKeyFrom(uploaded.data)
                  if (!key) throw new Error('Upload did not return an image.')
                  await websiteApi.updateProduct(cardId, row.id, email, {
                    productName: row.productName,
                    productDescription: row.productDescription,
                    productImage: key,
                    productCategory: row.productCategory,
                    categorySource: row.categorySource || null,
                    mrp: row.mrp,
                    sellingPrice: row.sellingPrice,
                    priceType: row.priceType || null,
                    pricingUnit: row.pricingUnit,
                    ctaText: row.ctaText,
                    displayOrder: row.displayOrder,
                  })
                  return
                }
                const row = visibleOffers(data.offers)[index]
                if (!row) throw new Error('Could not match this offer.')
                const uploaded = await websiteApi.uploadCardImage(cardId, email, 'offer', file)
                const key = imageKeyFrom(uploaded.data)
                if (!key) throw new Error('Upload did not return an image.')
                await websiteApi.updateOffer(cardId, row.id, email, {
                  offerTitle: row.offerTitle,
                  offerDescription: row.offerDescription,
                  offerImage: key,
                  badge: row.badge || null,
                  discountPercentage: row.discountPercentage,
                  status: row.status,
                  displayOrder: row.displayOrder,
                  startDate: row.startDate,
                  startTime: row.startTime,
                  endDate: row.endDate,
                  endTime: row.endTime,
                })
              })
            }}
          />
        </EditorField>
      </EditorDialog>
    )
  }

  if (request.kind === 'address') {
    return <AddressDialog company={data.company} busy={busy} error={formError} onClose={onClose} onSubmit={(patch) => void run(() => websiteApi.putCompany(cardId, email, { ...data.company, ...patch }).then(() => undefined))} />
  }

  if (request.kind === 'company') {
    return (
      <CompanyProfileDialog
        company={data.company}
        busy={busy}
        error={formError}
        onClose={onClose}
        onSubmit={(next) => void run(() => websiteApi.putCompany(cardId, email, next).then(() => undefined))}
      />
    )
  }

  if (request.kind === 'service-add') {
    return (
      <ItemAddDialog
        title="Add service"
        busy={busy}
        error={formError}
        onClose={onClose}
        fields={['image', 'name', 'description']}
        imageLabel="Service Image"
        nameLabel="Service Name"
        namePlaceholder="Enter service name (e.g. Haircut, AC Repair)"
        nameMax={30}
        nameRequiredMessage="Service name is required."
        descriptionLabel="Service Description"
        descriptionPlaceholder="Describe your service in short (features, benefits, pricing idea)"
        descriptionMax={400}
        onSubmit={(values) =>
          void run(async () => {
            if (withName(data.services).length >= 10) throw new Error('You can add up to 10 services.')
            let productImage: string | null = null
            if (values.file) {
              const uploaded = await websiteApi.uploadServiceImage(cardId, email, values.file)
              productImage = imageKeyFrom(uploaded.data) || null
            }
            await websiteApi.createService(cardId, email, {
              productName: values.name,
              productDescription: values.description,
              productImage,
            })
          })
        }
      />
    )
  }

  if (request.kind === 'product-add') {
    return (
      <PricingItemDialog
        busy={busy}
        error={formError}
        primaryCategory={data.company.positionPrimary}
        onClose={onClose}
        onSubmit={(values) =>
          void run(async () => {
            if (data.products.length >= 60) throw new Error('You can add up to 60 products or services.')
            let productImage: string | null = null
            if (values.file) {
              const uploaded = await websiteApi.uploadCardImage(cardId, email, 'product', values.file)
              productImage = imageKeyFrom(uploaded.data) || null
            }
            const onRequest = values.priceType === 'Price on Request'
            await websiteApi.createProduct(cardId, email, {
              productName: values.title,
              productDescription: values.description,
              productImage,
              productCategory: null,
              categorySource: values.category,
              mrp: 0,
              sellingPrice: onRequest ? 0 : Number(values.price),
              priceType: values.priceType,
              pricingUnit: values.pricingUnit,
              ctaText: values.cta,
            })
          })
        }
      />
    )
  }

  if (request.kind === 'offer-add') {
    return (
      <OfferAddDialog
        busy={busy}
        error={formError}
        onClose={onClose}
        onSubmit={(values) =>
          void run(async () => {
            if (data.offers.length >= 5) throw new Error('You can add up to 5 special offers.')
            let offerImage: string | null = null
            if (values.file) {
              const uploaded = await websiteApi.uploadCardImage(cardId, email, 'offer', values.file)
              offerImage = imageKeyFrom(uploaded.data) || null
            }
            await websiteApi.createOffer(cardId, email, {
              offerTitle: values.title,
              offerDescription: values.description,
              offerImage,
              badge: values.badge || null,
              discountPercentage: 0,
              status: 'Active',
              startDate: values.startDate || null,
              startTime: values.startTime || null,
              endDate: values.endDate || null,
              endTime: values.endTime || null,
            })
          })
        }
      />
    )
  }

  if (request.kind === 'gallery-add') {
    return (
      <GalleryDialog
        busy={busy}
        error={formError}
        onClose={onClose}
        onSubmit={(file) =>
          void run(async () => {
            if (data.gallery.length >= 10) throw new Error('You can add up to 10 gallery images.')
            const uploaded = await websiteApi.uploadCardImage(cardId, email, 'gallery', file)
            const key = imageKeyFrom(uploaded.data)
            if (!key) throw new Error('Upload did not return an image.')
            await websiteApi.createGallery(cardId, email, { galleryImage: key })
          })
        }
      />
    )
  }

  if (request.kind === 'videos') {
    return (
      <VideosDialog
        urls={data.videos}
        busy={busy}
        error={formError}
        onClose={onClose}
        onSubmit={(urls) => void run(() => websiteApi.putVideos(cardId, email, urls).then(() => undefined))}
      />
    )
  }

  if (request.kind === 'about') {
    return (
      <AboutDialog
        about={data.company.aboutUs}
        busy={busy}
        error={formError}
        onClose={onClose}
        onSubmit={(aboutUs) => void run(() => websiteApi.putCompany(cardId, email, { ...data.company, aboutUs }).then(() => undefined))}
      />
    )
  }

  if (request.kind === 'appointment') {
    return (
      <AppointmentDialog
        enabled={data.appointmentBookingEnabled}
        busy={busy}
        error={formError}
        onClose={onClose}
        onSubmit={(enabled) => void run(() => websiteApi.putAppointmentBooking(cardId, email, enabled).then(() => undefined))}
      />
    )
  }

  if (request.kind === 'social') {
    return (
      <SocialLinksDialog
        social={data.social}
        primaryCategory={data.company.positionPrimary}
        busy={busy}
        error={formError}
        onClose={onClose}
        onSubmit={(next) => void run(() => websiteApi.putSocial(cardId, email, next).then(() => undefined))}
      />
    )
  }

  return (
    <PaymentDialog
      payment={data.payment}
      busy={busy}
      error={formError}
      onClose={onClose}
      onSubmit={(slots) =>
        void run(async () => {
          const body: Record<string, unknown> = {
            paytm: data.payment.paytm,
            googlePay: data.payment.googlePay,
            phonePay: data.payment.phonePay,
            paytmQr: data.payment.paytmQr,
            googlePayQr: data.payment.googlePayQr,
            phonePayQr: data.payment.phonePayQr,
            accountNo: data.payment.accountNo,
            ifsc: data.payment.ifsc,
            acName: data.payment.acName,
            bankName: data.payment.bankName,
            acType: data.payment.acType,
          }
          for (const slot of slots) {
            const format = upiError(slot.upi)
            if (format) throw new Error(format)
            let imageKey = slot.removed ? '' : slot.imageKey
            if (slot.file) {
              const problem = imageProblem(slot.file) || (await squareImageProblem(slot.file))
              if (problem) throw new Error(problem)
              const uploaded = await websiteApi.uploadCardImage(cardId, email, 'payment', slot.file)
              imageKey = imageKeyFrom(uploaded.data)
              if (!imageKey) throw new Error('Upload did not return an image.')
            }
            if (imageKey && !slot.upi.trim()) throw new Error('Enter UPI ID')
            body[slot.upiKey] = slot.upi.trim()
            body[slot.qrKey] = imageKey
          }
          await websiteApi.putPayment(cardId, email, body)
        })
      }
    />
  )
}

function HomeImagesDialog({
  heroUrl,
  logoUrl,
  busy,
  error,
  onClose,
  onInvalid,
  onUpload,
  onRemove,
}: {
  heroUrl: string
  logoUrl: string
  busy: boolean
  error: string | null
  onClose: () => void
  onInvalid: (message: string) => void
  onUpload: (kind: 'hero' | 'logo', file: File, onProgress: (percent: number) => void) => Promise<string>
  onRemove: (kind: 'hero' | 'logo') => Promise<void>
}) {
  const [active, setActive] = useState<'hero' | 'logo' | null>(null)
  const [percent, setPercent] = useState(0)
  const [shown, setShown] = useState({ hero: heroUrl, logo: logoUrl })
  const held = useRef({ hero: false, logo: false })

  useEffect(() => {
    setShown((current) => ({
      hero: held.current.hero ? current.hero : heroUrl,
      logo: held.current.logo ? current.logo : logoUrl,
    }))
  }, [heroUrl, logoUrl])

  async function pick(kind: 'hero' | 'logo', file: File | null) {
    if (!file || busy) return
    const problem = imageProblem(file)
    if (problem) {
      onInvalid(problem)
      return
    }
    const localUrl = URL.createObjectURL(file)
    held.current = { hero: true, logo: true }
    setShown((current) => ({ ...current, [kind]: localUrl }))
    setActive(kind)
    setPercent(0)
    try {
      const savedUrl = await onUpload(kind, file, setPercent)
      setPercent(100)
      setShown((current) => ({ ...current, [kind]: savedUrl }))
      await new Promise((resolve) => window.setTimeout(resolve, 250))
    } catch {
      held.current[kind] = false
      setShown((current) => ({
        ...current,
        [kind]: kind === 'hero' ? heroUrl : logoUrl,
      }))
    } finally {
      window.setTimeout(() => URL.revokeObjectURL(localUrl), 1000)
      setActive(null)
    }
  }

  return (
    <EditorDialog title="Profile images" onClose={onClose} busy={busy || active !== null}>
      {error ? <p style={{ color: '#b42318', marginTop: 0 }}>{error}</p> : null}
      <HomeImageSlot
        label="Cover image"
        hint="This is the large image at the top of the profile."
        src={shown.hero}
        tall
        busy={busy}
        uploading={active === 'hero'}
        percent={percent}
        onFile={(file) => void pick('hero', file)}
        onRemove={() => {
          held.current.hero = false
          return onRemove('hero')
        }}
      />
      <HomeImageSlot
        label="Logo"
        hint="This is the round image on the profile."
        src={shown.logo}
        busy={busy}
        uploading={active === 'logo'}
        percent={percent}
        onFile={(file) => void pick('logo', file)}
        onRemove={() => {
          held.current.logo = false
          return onRemove('logo')
        }}
      />
    </EditorDialog>
  )
}

function HomeImageSlot({
  label,
  hint,
  src,
  tall,
  busy,
  uploading,
  percent,
  onFile,
  onRemove,
}: {
  label: string
  hint: string
  src: string
  tall?: boolean
  busy: boolean
  uploading: boolean
  percent: number
  onFile: (file: File) => void
  onRemove: () => void
}) {
  return (
    <div style={{ marginBottom: 18 }}>
      <div style={{ fontWeight: 700, marginBottom: 4 }}>{label}</div>
      <p style={{ margin: '0 0 8px', color: '#64748b', fontSize: 13 }}>{hint}</p>
      <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
      <label
        style={{
          position: 'relative',
          display: 'grid',
          placeItems: 'center',
          flex: 1,
          minWidth: 0,
          height: tall ? 160 : 120,
          borderRadius: 14,
          overflow: 'hidden',
          border: '1px dashed #cbd5e1',
          background: '#f8fafc',
          cursor: busy ? 'wait' : 'pointer',
        }}
      >
        {src ? (
          <img src={src} alt="" style={{ width: '100%', height: '100%', objectFit: 'cover' }} />
        ) : (
          <span style={{ color: '#64748b', fontWeight: 600 }}>Choose image</span>
        )}
        {uploading ? (
          <div
            style={{
              position: 'absolute',
              inset: 0,
              background: 'rgba(15, 23, 42, 0.68)',
              color: '#fff',
              display: 'grid',
              placeItems: 'center',
              padding: 16,
            }}
          >
            <div style={{ width: '100%', maxWidth: 240, textAlign: 'center' }}>
              <div style={{ fontWeight: 700, marginBottom: 8 }}>Uploading {percent}%</div>
              <div style={{ height: 8, borderRadius: 99, background: 'rgba(255,255,255,0.28)', overflow: 'hidden' }}>
                <div style={{ width: `${percent}%`, height: '100%', background: '#e2c27a', transition: 'width 0.15s linear' }} />
              </div>
            </div>
          </div>
        ) : null}
        <input
          type="file"
          accept=".jpg,.jpeg,.png,.gif,.webp,image/*"
          disabled={busy}
          style={{ display: 'none' }}
          onChange={(event) => {
            const file = event.target.files?.[0] ?? null
            event.target.value = ''
            if (file) onFile(file)
          }}
        />
      </label>
      {src ? (
        <MiniIcon
          label={`Delete ${label.toLowerCase()}`}
          icon="fa-trash"
          tone="danger"
          onClick={() => {
            if (busy || uploading) return
            onRemove()
          }}
        />
      ) : null}
      </div>
    </div>
  )
}

function AddressDialog({
  company,
  busy,
  error,
  onClose,
  onSubmit,
}: {
  company: CompanyForm
  busy: boolean
  error: string | null
  onClose: () => void
  onSubmit: (patch: Partial<CompanyForm>) => void
}) {
  const [form, setForm] = useState({
    address: company.address,
    address2: company.address2,
    city: company.city,
    state: company.state,
    pincode: company.pincode,
    country: company.country,
    location: company.location,
  })
  const [localError, setLocalError] = useState<string | null>(null)
  return (
    <EditorDialog title="Address" onClose={onClose}>
      <form
        onSubmit={(e) => {
          e.preventDefault()
          const pinProblem = pincodeError(form.pincode)
          if (pinProblem) {
            setLocalError(pinProblem)
            return
          }
          setLocalError(null)
          onSubmit({ ...form, pincode: pinDigits(form.pincode) })
        }}
      >
        {error || localError ? <p style={{ color: '#b42318' }}>{error || localError}</p> : null}
        {(
          [
            ['address', 'Address Line 1'],
            ['address2', 'Area/Locality'],
            ['country', 'Country'],
            ['state', 'State'],
            ['city', 'City'],
            ['pincode', 'Pin code'],
            ['location', 'Google map link'],
          ] as const
        ).map(([key, label]) => (
          <EditorField label={label} key={key}>
            <input
              inputMode={key === 'pincode' ? 'numeric' : undefined}
              placeholder={key === 'pincode' ? '6 digit pin code' : undefined}
              value={form[key]}
              onChange={(e) => setForm({ ...form, [key]: key === 'pincode' ? pinDigits(e.target.value) : e.target.value })}
            />
          </EditorField>
        ))}
        <EditorActions>
          <button className="mw-editor-primary" type="submit" disabled={busy}>
            {busy ? 'Saving...' : 'Save'}
          </button>
        </EditorActions>
      </form>
    </EditorDialog>
  )
}

function ItemAddDialog({
  title,
  fields,
  busy,
  error,
  onClose,
  onSubmit,
  nameLabel = 'Name',
  namePlaceholder,
  nameMax,
  nameRequiredMessage = 'Enter a name.',
  descriptionLabel = 'Description',
  descriptionPlaceholder,
  descriptionMax,
  imageLabel = 'Image',
}: {
  title: string
  fields: Array<'name' | 'description' | 'priceType' | 'price' | 'mrp' | 'discount' | 'image'>
  busy: boolean
  error: string | null
  onClose: () => void
  onSubmit: (values: { name: string; description: string; price: number; mrp: number; discount: number; priceType: string; file: File | null }) => void
  nameLabel?: string
  namePlaceholder?: string
  nameMax?: number
  nameRequiredMessage?: string
  descriptionLabel?: string
  descriptionPlaceholder?: string
  descriptionMax?: number
  imageLabel?: string
}) {
  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [priceType, setPriceType] = useState('Fixed')
  const [price, setPrice] = useState('0')
  const [mrp, setMrp] = useState('0')
  const [discount, setDiscount] = useState('0')
  const [file, setFile] = useState<File | null>(null)
  const [localError, setLocalError] = useState<string | null>(null)
  const onRequest = fields.includes('priceType') && priceType === 'on_request'

  return (
    <EditorDialog title={title} onClose={onClose}>
      <form
        onSubmit={(e) => {
          e.preventDefault()
          if (!name.trim()) {
            setLocalError(nameRequiredMessage)
            return
          }
          if (nameMax && name.trim().length > nameMax) {
            setLocalError(`${nameLabel} can be at most ${nameMax} characters.`)
            return
          }
          if (descriptionMax && description.trim().length > descriptionMax) {
            setLocalError(`${descriptionLabel} can be at most ${descriptionMax} characters.`)
            return
          }
          const priceN = Number(price.trim().replace(/,/g, ''))
          const mrpN = Number(mrp.trim().replace(/,/g, ''))
          const discountN = Number(discount.trim().replace(/,/g, ''))
          if (fields.includes('price') && !onRequest && !Number.isFinite(priceN)) {
            setLocalError('Enter a valid price.')
            return
          }
          if (fields.includes('mrp') && !onRequest && !Number.isFinite(mrpN)) {
            setLocalError('Enter a valid MRP.')
            return
          }
          if (fields.includes('discount') && !Number.isFinite(discountN)) {
            setLocalError('Enter a valid discount.')
            return
          }
          if (file) {
            const problem = imageProblem(file)
            if (problem) {
              setLocalError(problem)
              return
            }
          }
          setLocalError(null)
          onSubmit({
            name: name.trim(),
            description: description.trim(),
            price: Number.isFinite(priceN) ? priceN : 0,
            mrp: Number.isFinite(mrpN) ? mrpN : 0,
            discount: Number.isFinite(discountN) ? Math.round(discountN) : 0,
            priceType,
            file,
          })
        }}
      >
        {error || localError ? <p style={{ color: '#b42318' }}>{error || localError}</p> : null}
        {fields[0] === 'image' ? (
          <EditorField label={imageLabel}>
            <input
              type="file"
              accept=".jpg,.jpeg,.png,.gif,.webp,image/*"
              onChange={(e) => {
                setFile(e.target.files?.[0] ?? null)
                e.target.value = ''
              }}
            />
            {file ? <span style={{ fontWeight: 500 }}>{file.name}</span> : null}
          </EditorField>
        ) : null}
        {fields.includes('name') ? (
          <EditorField label={nameLabel}>
            <input
              required
              maxLength={nameMax}
              placeholder={namePlaceholder}
              value={name}
              onChange={(e) => setName(nameMax ? e.target.value.slice(0, nameMax) : e.target.value)}
            />
            {nameMax ? <span style={{ display: 'block', marginTop: 4, color: '#64748b', fontWeight: 500 }}>{name.trim().length}/{nameMax}</span> : null}
          </EditorField>
        ) : null}
        {fields.includes('description') ? (
          <EditorField label={descriptionLabel}>
            <textarea
              rows={4}
              maxLength={descriptionMax}
              placeholder={descriptionPlaceholder}
              value={description}
              onChange={(e) => setDescription(descriptionMax ? e.target.value.slice(0, descriptionMax) : e.target.value)}
            />
            {descriptionMax ? <span style={{ display: 'block', marginTop: 4, color: '#64748b', fontWeight: 500 }}>{description.trim().length}/{descriptionMax}</span> : null}
          </EditorField>
        ) : null}
        {fields.includes('priceType') ? (
          <EditorField label="Price type">
            <select value={priceType} onChange={(e) => setPriceType(e.target.value)}>
              <option value="Fixed">Fixed</option>
              <option value="on_request">On Request</option>
            </select>
          </EditorField>
        ) : null}
        {fields.includes('price') && !onRequest ? (
          <EditorField label="Price">
            <input inputMode="decimal" value={price} onChange={(e) => setPrice(e.target.value)} />
          </EditorField>
        ) : null}
        {fields.includes('mrp') && !onRequest ? (
          <EditorField label="MRP">
            <input inputMode="decimal" value={mrp} onChange={(e) => setMrp(e.target.value)} />
          </EditorField>
        ) : null}
        {fields.includes('discount') ? (
          <EditorField label="Discount %">
            <input inputMode="numeric" value={discount} onChange={(e) => setDiscount(e.target.value)} />
          </EditorField>
        ) : null}
        {fields.includes('image') && fields[0] !== 'image' ? (
          <EditorField label={imageLabel}>
            <input
              type="file"
              accept=".jpg,.jpeg,.png,.gif,.webp,image/*"
              onChange={(e) => {
                setFile(e.target.files?.[0] ?? null)
                e.target.value = ''
              }}
            />
            {file ? <span style={{ fontWeight: 500 }}>{file.name}</span> : null}
          </EditorField>
        ) : null}
        <EditorActions>
          <button className="mw-editor-primary" type="submit" disabled={busy}>
            {busy ? 'Saving...' : 'Save'}
          </button>
        </EditorActions>
      </form>
    </EditorDialog>
  )
}

const PRICE_TYPES = ['Fixed Price', 'Starting From', 'Price on Request'] as const
const PRICING_UNITS = ['Per item', 'Per hour', 'Per minute', 'Per day', 'Per visit', 'Per service', 'Per kg/Unit', 'Per Project']
const CTA_OPTIONS = ['Enquire', 'Get Details', 'Contact Now', 'Book Now', 'Schedule', 'Get Quote', 'Order Now', 'Buy Now', 'Know More']
const DIGITAL_CATEGORIES = ['SEO Service', 'Social Media', 'Website Design', 'Content Writing', 'Online Ads', 'Branding']

function PricingItemDialog({
  busy,
  error,
  primaryCategory,
  onClose,
  onSubmit,
}: {
  busy: boolean
  error: string | null
  primaryCategory: string
  onClose: () => void
  onSubmit: (values: {
    category: string
    title: string
    description: string
    priceType: string
    price: string
    pricingUnit: string
    cta: string
    file: File | null
  }) => void
}) {
  const [category, setCategory] = useState('')
  const [title, setTitle] = useState('')
  const [description, setDescription] = useState('')
  const [priceType, setPriceType] = useState('Fixed Price')
  const [price, setPrice] = useState('')
  const [priceError, setPriceError] = useState<string | null>(null)
  const [unitChoice, setUnitChoice] = useState('')
  const [customUnit, setCustomUnit] = useState('')
  const [ctaChoice, setCtaChoice] = useState('')
  const [customCta, setCustomCta] = useState('')
  const [ctaError, setCtaError] = useState<string | null>(null)
  const [file, setFile] = useState<File | null>(null)
  const [localError, setLocalError] = useState<string | null>(null)
  const onRequest = priceType === 'Price on Request'
  const categoryName = /^\d+$/.test(primaryCategory.trim()) ? '' : primaryCategory.trim()
  const suggestions = categoryName === 'Digital Marketing' ? DIGITAL_CATEGORIES : categoryName ? [categoryName] : []

  return (
    <EditorDialog title="Details & Pricing" onClose={onClose} busy={busy}>
      <form
        onSubmit={(event) => {
          event.preventDefault()
          const unit = unitChoice === 'custom' ? customUnit.trim() : unitChoice
          const cta = ctaChoice === 'custom' ? customCta.trim() : ctaChoice
          if (!category.trim()) {
            setLocalError('Product/Service category is required.')
            return
          }
          if (category.trim().length > 30) {
            setLocalError('Category can be at most 30 characters.')
            return
          }
          if (!title.trim()) {
            setLocalError('Title is required.')
            return
          }
          if (title.trim().length > 30) {
            setLocalError('Title can be at most 30 characters.')
            return
          }
          if (description.trim().length > 400) {
            setLocalError('Description can be at most 400 characters.')
            return
          }
          if (!onRequest) {
            if (!price) {
              setLocalError('Enter price')
              return
            }
            if (!/^\d{1,7}$/.test(price)) {
              setLocalError('Only numbers allowed')
              return
            }
          }
          if (!unit) {
            setLocalError('Pricing unit is required.')
            return
          }
          if (unit.length > 20) {
            setLocalError('Pricing unit can be at most 20 characters.')
            return
          }
          if (!cta) {
            setLocalError('Select button text.')
            return
          }
          if (cta.length > 20) {
            setLocalError('Button text can be at most 20 characters.')
            return
          }
          if (!/^[A-Za-z0-9 &]+$/.test(cta)) {
            setLocalError('Button text can only use letters, numbers, spaces, and &.')
            return
          }
          if (file) {
            const problem = imageProblem(file)
            if (problem) {
              setLocalError(problem)
              return
            }
          }
          setLocalError(null)
          onSubmit({
            category: category.trim(),
            title: title.trim(),
            description: description.trim(),
            priceType,
            price,
            pricingUnit: unit,
            cta,
            file,
          })
        }}
      >
        {error || localError ? <p style={{ color: '#b42318' }}>{error || localError}</p> : null}
        <EditorField label="Image">
          <input
            type="file"
            accept=".jpg,.jpeg,.png,.gif,.webp,image/*"
            onChange={(e) => {
              setFile(e.target.files?.[0] ?? null)
              e.target.value = ''
            }}
          />
          {file ? <span style={{ fontWeight: 500 }}>{file.name}</span> : null}
        </EditorField>
        <EditorField label="Product/Service Category">
          <input
            list="pricing-category-suggestions"
            maxLength={30}
            value={category}
            onChange={(e) => setCategory(e.target.value.slice(0, 30))}
          />
          <datalist id="pricing-category-suggestions">
            {suggestions.map((item) => (
              <option key={item} value={item} />
            ))}
          </datalist>
          <span style={{ display: 'block', marginTop: 4, color: '#64748b', fontWeight: 500 }}>
            {categoryName ? `Based on your primary category: ${categoryName}` : 'Choose a primary business category in Business profile first.'} {category.trim().length}/30
          </span>
        </EditorField>
        <EditorField label="Title">
          <input
            required
            maxLength={30}
            placeholder="Enter item/service name"
            value={title}
            onChange={(e) => setTitle(e.target.value.slice(0, 30))}
          />
          <span style={{ display: 'block', marginTop: 4, color: '#64748b', fontWeight: 500 }}>{title.trim().length}/30</span>
        </EditorField>
        <EditorField label="Description">
          <textarea
            rows={4}
            maxLength={400}
            placeholder="Add short details (features, usage, service info)"
            value={description}
            onChange={(e) => setDescription(e.target.value.slice(0, 400))}
          />
          <span style={{ display: 'block', marginTop: 4, color: '#64748b', fontWeight: 500 }}>{description.trim().length}/400</span>
        </EditorField>
        <EditorField label="Price Type">
          <select value={priceType} onChange={(e) => setPriceType(e.target.value)}>
            {PRICE_TYPES.map((item) => (
              <option key={item} value={item}>
                {item}
              </option>
            ))}
          </select>
        </EditorField>
        {onRequest ? null : (
          <EditorField label="Price">
            <input
              inputMode="numeric"
              placeholder="Enter price"
              value={price}
              onChange={(e) => {
                if (/\D/.test(e.target.value)) setPriceError('Only numbers allowed')
                else setPriceError(null)
                setPrice(e.target.value.replace(/\D/g, '').slice(0, 7))
              }}
            />
            {priceError ? <span style={{ display: 'block', color: '#b42318', fontWeight: 500, marginTop: 4 }}>{priceError}</span> : null}
          </EditorField>
        )}
        <EditorField label="Pricing Unit">
          <select value={unitChoice} onChange={(e) => setUnitChoice(e.target.value)}>
            <option value="">Select pricing unit</option>
            {PRICING_UNITS.map((item) => (
              <option key={item} value={item}>
                {item}
              </option>
            ))}
            <option value="custom">Custom</option>
          </select>
          {unitChoice === 'custom' ? (
            <input
              style={{ marginTop: 8 }}
              maxLength={20}
              placeholder="e.g. Per Room, Per Person"
              value={customUnit}
              onChange={(e) => setCustomUnit(e.target.value.slice(0, 20))}
            />
          ) : null}
        </EditorField>
        <EditorField label="Button Text">
          <select value={ctaChoice} onChange={(e) => setCtaChoice(e.target.value)}>
            <option value="">Select button text (e.g. Enquire, Book Now)</option>
            {CTA_OPTIONS.map((item) => (
              <option key={item} value={item}>
                {item}
              </option>
            ))}
            <option value="custom">Custom CTA</option>
          </select>
          {ctaChoice === 'custom' ? (
            <>
              <input
                style={{ marginTop: 8 }}
                maxLength={20}
                placeholder="Write your own button text"
                value={customCta}
                onChange={(e) => {
                  if (/[^A-Za-z0-9 &]/.test(e.target.value)) setCtaError('Button text can only use letters, numbers, spaces, and &.')
                  else setCtaError(null)
                  setCustomCta(e.target.value.replace(/[^A-Za-z0-9 &]/g, '').slice(0, 20))
                }}
              />
              {ctaError ? <span style={{ display: 'block', color: '#b42318', fontWeight: 500, marginTop: 4 }}>{ctaError}</span> : null}
            </>
          ) : null}
        </EditorField>
        <EditorActions>
          <button className="mw-editor-primary" type="submit" disabled={busy}>
            {busy ? 'Saving...' : 'Save'}
          </button>
        </EditorActions>
      </form>
    </EditorDialog>
  )
}

const OFFER_BADGES = ['Buy 1 Get 1', 'Best Deal', 'Mega Deal', 'Limited Time Offer', 'Festive Offer', 'Flat 15% Off']

function OfferAddDialog({
  busy,
  error,
  onClose,
  onSubmit,
}: {
  busy: boolean
  error: string | null
  onClose: () => void
  onSubmit: (values: {
    title: string
    badge: string
    description: string
    startDate: string
    startTime: string
    endDate: string
    endTime: string
    file: File | null
  }) => void
}) {
  const [title, setTitle] = useState('')
  const [badge, setBadge] = useState('')
  const [description, setDescription] = useState('')
  const [startDate, setStartDate] = useState('')
  const [startTime, setStartTime] = useState('')
  const [endDate, setEndDate] = useState('')
  const [endTime, setEndTime] = useState('')
  const [file, setFile] = useState<File | null>(null)
  const [localError, setLocalError] = useState<string | null>(null)

  return (
    <EditorDialog title="Add special offer" onClose={onClose} busy={busy}>
      <form
        onSubmit={(event) => {
          event.preventDefault()
          if (!title.trim()) {
            setLocalError('Offer title is required.')
            return
          }
          if (badge.trim().length > 20) {
            setLocalError('Offer badge can be at most 20 characters.')
            return
          }
          if (description.trim().length > 500) {
            setLocalError('Offer description can be at most 500 characters.')
            return
          }
          if (startDate && endDate) {
            const start = new Date(`${startDate}T${startTime || '00:00'}`)
            const end = new Date(`${endDate}T${endTime || '23:59'}`)
            if (end.getTime() < start.getTime()) {
              setLocalError('End date and time must be after the start.')
              return
            }
          }
          if (file) {
            const problem = imageProblem(file)
            if (problem) {
              setLocalError(problem)
              return
            }
          }
          setLocalError(null)
          onSubmit({
            title: title.trim(),
            badge: badge.trim(),
            description: description.trim(),
            startDate,
            startTime,
            endDate,
            endTime,
            file,
          })
        }}
      >
        {error || localError ? <p style={{ color: '#b42318' }}>{error || localError}</p> : null}
        <EditorField label="Image">
          <input
            type="file"
            accept=".jpg,.jpeg,.png,.gif,.webp,image/*"
            onChange={(e) => {
              setFile(e.target.files?.[0] ?? null)
              e.target.value = ''
            }}
          />
          {file ? <span style={{ fontWeight: 500 }}>{file.name}</span> : null}
        </EditorField>
        <EditorField label="Offer Title">
          <input required value={title} onChange={(e) => setTitle(e.target.value)} />
        </EditorField>
        <EditorField label="Offer Badge">
          <input
            list="offer-badge-suggestions"
            maxLength={20}
            placeholder="e.g. Flat 20% Off"
            value={badge}
            onChange={(e) => setBadge(e.target.value.slice(0, 20))}
          />
          <datalist id="offer-badge-suggestions">
            {OFFER_BADGES.map((item) => (
              <option key={item} value={item} />
            ))}
          </datalist>
          <span style={{ display: 'block', marginTop: 4, color: '#64748b', fontWeight: 500 }}>{badge.trim().length}/20</span>
        </EditorField>
        <EditorField label="Offer Description">
          <textarea
            rows={4}
            maxLength={500}
            placeholder="Explain the offer clearly (validity, benefits, conditions)"
            value={description}
            onChange={(e) => setDescription(e.target.value.slice(0, 500))}
          />
          <span style={{ display: 'block', marginTop: 4, color: '#64748b', fontWeight: 500 }}>{description.trim().length}/500</span>
        </EditorField>
        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
          <EditorField label="Start Date">
            <input type="date" value={startDate} onChange={(e) => setStartDate(e.target.value)} />
          </EditorField>
          <EditorField label="Start Time">
            <input type="time" value={startTime} onChange={(e) => setStartTime(e.target.value)} />
          </EditorField>
          <EditorField label="End Date">
            <input type="date" value={endDate} onChange={(e) => setEndDate(e.target.value)} />
          </EditorField>
          <EditorField label="End Time">
            <input type="time" value={endTime} onChange={(e) => setEndTime(e.target.value)} />
          </EditorField>
        </div>
        <EditorActions>
          <button className="mw-editor-primary" type="submit" disabled={busy}>
            {busy ? 'Saving...' : 'Save'}
          </button>
        </EditorActions>
      </form>
    </EditorDialog>
  )
}

function GalleryDialog({
  busy,
  error,
  onClose,
  onSubmit,
}: {
  busy: boolean
  error: string | null
  onClose: () => void
  onSubmit: (file: File) => void
}) {
  const [file, setFile] = useState<File | null>(null)
  const [localError, setLocalError] = useState<string | null>(null)
  return (
    <EditorDialog title="Add gallery image" onClose={onClose}>
      <form
        onSubmit={(e) => {
          e.preventDefault()
          if (!file) {
            setLocalError('Choose an image.')
            return
          }
          const problem = imageProblem(file)
          if (problem) {
            setLocalError(problem)
            return
          }
          onSubmit(file)
        }}
      >
        {error || localError ? <p style={{ color: '#b42318' }}>{error || localError}</p> : null}
        <p style={{ margin: '0 0 12px', color: '#64748b', fontWeight: 500 }}>Add a general image related to your business. You can add up to 10.</p>
        <EditorField label="Image">
          <input
            type="file"
            accept=".jpg,.jpeg,.png,.gif,.webp,image/*"
            onChange={(e) => {
              setFile(e.target.files?.[0] ?? null)
            }}
          />
        </EditorField>
        <EditorActions>
          <button className="mw-editor-primary" type="submit" disabled={busy}>
            {busy ? 'Saving...' : 'Save'}
          </button>
        </EditorActions>
      </form>
    </EditorDialog>
  )
}

function VideosDialog({
  urls,
  busy,
  error,
  onClose,
  onSubmit,
}: {
  urls: string[]
  busy: boolean
  error: string | null
  onClose: () => void
  onSubmit: (urls: (string | null)[]) => void
}) {
  const [slots, setSlots] = useState(() => Array.from({ length: VIDEO_LINK_LIMIT }, (_, i) => urls[i] || ''))
  const [slotErrors, setSlotErrors] = useState<(string | null)[]>(() => Array.from({ length: VIDEO_LINK_LIMIT }, () => null))
  const [localError, setLocalError] = useState<string | null>(null)
  const filled = slots.filter((url) => url.trim()).length
  return (
    <EditorDialog title="Videos" onClose={onClose} busy={busy}>
      <form
        onSubmit={(e) => {
          e.preventDefault()
          const nextErrors = slots.map((url) => videoLinkError(url))
          setSlotErrors(nextErrors)
          if (nextErrors.some(Boolean)) {
            setLocalError(VIDEO_LINK_ERROR)
            return
          }
          if (filled > VIDEO_LINK_LIMIT) {
            setLocalError('You can add up to 20 video links.')
            return
          }
          setLocalError(null)
          onSubmit(slots.map((url) => url.trim() || null))
        }}
      >
        {error || localError ? <p style={{ color: '#b42318' }}>{error || localError}</p> : null}
        <p style={{ margin: '0 0 12px', color: '#64748b', fontWeight: 500 }}>{filled}/20 links</p>
        {slots.map((url, i) => (
          <EditorField label={`Video ${i + 1}`} key={i}>
            <input
              placeholder={VIDEO_LINK_PLACEHOLDER}
              value={url}
              onChange={(e) => {
                const value = e.target.value
                setSlots((current) => current.map((item, index) => (index === i ? value : item)))
                setSlotErrors((current) =>
                  current.map((item, index) => (index === i ? (item && value.trim() ? videoLinkError(value) : null) : item)),
                )
              }}
            />
            {slotErrors[i] ? <span style={{ display: 'block', color: '#b42318', fontWeight: 500, marginTop: 4 }}>{slotErrors[i]}</span> : null}
          </EditorField>
        ))}
        <EditorActions>
          <button className="mw-editor-primary" type="submit" disabled={busy}>
            {busy ? 'Saving...' : 'Save'}
          </button>
        </EditorActions>
      </form>
    </EditorDialog>
  )
}

const PAYMENT_SLOTS = [
  { label: 'Google Pay', upiKey: 'googlePay', qrKey: 'googlePayQr', urlKey: 'googlePayQrUrl' },
  { label: 'PhonePe', upiKey: 'phonePay', qrKey: 'phonePayQr', urlKey: 'phonePayQrUrl' },
  { label: 'Paytm', upiKey: 'paytm', qrKey: 'paytmQr', urlKey: 'paytmQrUrl' },
] as const

function PaymentDialog({
  payment,
  busy,
  error,
  onClose,
  onSubmit,
}: {
  payment: PaymentForm
  busy: boolean
  error: string | null
  onClose: () => void
  onSubmit: (slots: Array<{ upiKey: string; qrKey: string; upi: string; imageKey: string; file: File | null; removed: boolean }>) => void
}) {
  const [upis, setUpis] = useState(() => PAYMENT_SLOTS.map((slot) => payment[slot.upiKey]))
  const [files, setFiles] = useState<(File | null)[]>(() => PAYMENT_SLOTS.map(() => null))
  const [removed, setRemoved] = useState(() => PAYMENT_SLOTS.map(() => false))
  const [previews, setPreviews] = useState(() => PAYMENT_SLOTS.map((slot) => payment[slot.urlKey]))
  const [slotErrors, setSlotErrors] = useState<(string | null)[]>(() => PAYMENT_SLOTS.map(() => null))
  const [localError, setLocalError] = useState<string | null>(null)

  return (
    <EditorDialog title="Payment Details" onClose={onClose} busy={busy}>
      <form
        onSubmit={(event) => {
          event.preventDefault()
          const nextErrors = upis.map((upi, index) => {
            const format = upiError(upi)
            if (format) return format
            const hasImage = Boolean(files[index]) || (!removed[index] && payment[PAYMENT_SLOTS[index].qrKey])
            if (hasImage && !upi.trim()) return 'Enter UPI ID'
            return null
          })
          setSlotErrors(nextErrors)
          if (nextErrors.some(Boolean)) {
            setLocalError(nextErrors.find(Boolean) || UPI_ERROR)
            return
          }
          setLocalError(null)
          onSubmit(
            PAYMENT_SLOTS.map((slot, index) => ({
              upiKey: slot.upiKey,
              qrKey: slot.qrKey,
              upi: upis[index],
              imageKey: removed[index] ? '' : payment[slot.qrKey],
              file: files[index],
              removed: removed[index],
            })),
          )
        }}
      >
        {error || localError ? <p style={{ color: '#b42318' }}>{error || localError}</p> : null}
        <p style={{ margin: '0 0 12px', color: '#64748b', fontWeight: 500 }}>Add up to 3 square payment QR codes, each with its UPI ID.</p>
        {PAYMENT_SLOTS.map((slot, index) => (
          <EditorField label={slot.label} key={slot.upiKey}>
            {previews[index] ? (
              <img src={previews[index]} alt={`${slot.label} QR`} style={{ width: 120, height: 120, objectFit: 'cover', borderRadius: 8, display: 'block', marginBottom: 8 }} />
            ) : null}
            <input
              type="file"
              accept=".jpg,.jpeg,.png,.gif,.webp,image/*"
              onChange={(e) => {
                const file = e.target.files?.[0] ?? null
                e.target.value = ''
                if (!file) return
                void (async () => {
                  const problem = imageProblem(file) || (await squareImageProblem(file))
                  if (problem) {
                    setSlotErrors((current) => current.map((item, itemIndex) => (itemIndex === index ? problem : item)))
                    return
                  }
                  setFiles((current) => current.map((item, itemIndex) => (itemIndex === index ? file : item)))
                  setRemoved((current) => current.map((item, itemIndex) => (itemIndex === index ? false : item)))
                  setPreviews((current) => current.map((item, itemIndex) => (itemIndex === index ? URL.createObjectURL(file) : item)))
                  setSlotErrors((current) => current.map((item, itemIndex) => (itemIndex === index ? null : item)))
                })()
              }}
            />
            {previews[index] ? (
              <button
                type="button"
                className="mw-editor-ghost"
                style={{ marginTop: 8 }}
                onClick={() => {
                  setFiles((current) => current.map((item, itemIndex) => (itemIndex === index ? null : item)))
                  setRemoved((current) => current.map((item, itemIndex) => (itemIndex === index ? true : item)))
                  setPreviews((current) => current.map((item, itemIndex) => (itemIndex === index ? '' : item)))
                }}
              >
                Remove QR
              </button>
            ) : null}
            <input
              style={{ marginTop: 8 }}
              placeholder={UPI_PLACEHOLDER}
              value={upis[index]}
              onChange={(e) => {
                const value = e.target.value
                setUpis((current) => current.map((item, itemIndex) => (itemIndex === index ? value : item)))
                setSlotErrors((current) => current.map((item, itemIndex) => (itemIndex === index ? (item ? upiError(value) : null) : item)))
              }}
            />
            {slotErrors[index] ? <span style={{ display: 'block', color: '#b42318', fontWeight: 500, marginTop: 4 }}>{slotErrors[index]}</span> : null}
          </EditorField>
        ))}
        <EditorActions>
          <button className="mw-editor-primary" type="submit" disabled={busy}>
            {busy ? 'Saving...' : 'Save'}
          </button>
        </EditorActions>
      </form>
    </EditorDialog>
  )
}

function AboutDialog({
  about,
  busy,
  error,
  onClose,
  onSubmit,
}: {
  about: string
  busy: boolean
  error: string | null
  onClose: () => void
  onSubmit: (aboutUs: string) => void
}) {
  const [value, setValue] = useState(about)
  const [localError, setLocalError] = useState<string | null>(null)

  return (
    <EditorDialog title="About" onClose={onClose} busy={busy}>
      <form
        onSubmit={(event) => {
          event.preventDefault()
          const problem = aboutError(value)
          setLocalError(problem)
          if (problem) return
          onSubmit(value.trim())
        }}
      >
        {error || localError ? <p style={{ color: '#b42318' }}>{error || localError}</p> : null}
        <EditorField label="About us">
          <textarea
            rows={6}
            maxLength={500}
            value={value}
            placeholder={ABOUT_PLACEHOLDER}
            onChange={(event) => {
              setValue(event.target.value)
              setLocalError(aboutError(event.target.value))
            }}
          />
        </EditorField>
        <p style={{ margin: '-4px 0 12px', color: '#64748b', fontSize: 12, fontWeight: 500 }}>{value.trim().length}/500</p>
        <EditorActions>
          <button className="mw-editor-primary" type="submit" disabled={busy}>
            {busy ? 'Saving…' : 'Save'}
          </button>
        </EditorActions>
      </form>
    </EditorDialog>
  )
}

function AppointmentDialog({
  enabled,
  busy,
  error,
  onClose,
  onSubmit,
}: {
  enabled: boolean
  busy: boolean
  error: string | null
  onClose: () => void
  onSubmit: (enabled: boolean) => void
}) {
  const [on, setOn] = useState(enabled)

  return (
    <EditorDialog title="Appointment Booking" onClose={onClose} busy={busy}>
      <form
        onSubmit={(event) => {
          event.preventDefault()
          onSubmit(on)
        }}
      >
        {error ? <p style={{ color: '#b42318', margin: '0 0 12px' }}>{error}</p> : null}
        <p style={{ margin: '0 0 16px', color: '#64748b', fontSize: 14, fontWeight: 400, lineHeight: 1.55 }}>
          Show a Book Appointment button on your profile. Visitors can request a time by WhatsApp, phone, or email.
        </p>
        <div
          style={{
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
            gap: 16,
            padding: '14px 16px',
            border: `1px solid ${on ? '#d9ccff' : '#e5e7eb'}`,
            borderRadius: 12,
            background: on ? '#f6f3ff' : '#f8fafc',
          }}
        >
          <div>
            <div style={{ fontWeight: 650, fontSize: 15, color: '#1f2937' }}>Enable appointment booking</div>
            <div style={{ marginTop: 4, fontSize: 13, fontWeight: 500, color: '#6b7280' }}>
              {on ? 'Shown on your profile' : 'Hidden from visitors'}
            </div>
          </div>
          <button
            type="button"
            role="switch"
            aria-checked={on}
            aria-label="Enable appointment booking"
            onClick={() => setOn((current) => !current)}
            style={{
              width: 46,
              height: 26,
              borderRadius: 999,
              border: 0,
              padding: 3,
              background: on ? '#6C2EFF' : '#d1d5db',
              cursor: 'pointer',
              flex: '0 0 auto',
            }}
          >
            <span
              style={{
                display: 'block',
                width: 20,
                height: 20,
                borderRadius: '50%',
                background: '#fff',
                transform: on ? 'translateX(20px)' : 'none',
                transition: 'transform 0.15s ease',
              }}
            />
          </button>
        </div>
        <EditorActions>
          <button className="mw-editor-primary" type="submit" disabled={busy}>
            {busy ? 'Saving…' : 'Save'}
          </button>
        </EditorActions>
      </form>
    </EditorDialog>
  )
}

const SOCIAL_FIELDS = [
  ['fb', 'Facebook'],
  ['instagram', 'Instagram'],
  ['linkedin', 'LinkedIn'],
  ['twitter', 'Twitter / X'],
  ['youtube', 'YouTube'],
  ['pinterest', 'Pinterest'],
  ['google', 'Google'],
] as const

function SocialLinksDialog({
  social,
  primaryCategory,
  busy,
  error,
  onClose,
  onSubmit,
}: {
  social: SocialForm
  primaryCategory: string
  busy: boolean
  error: string | null
  onClose: () => void
  onSubmit: (next: SocialForm) => void
}) {
  const [form, setForm] = useState(social)
  const [fieldErrors, setFieldErrors] = useState<Partial<Record<keyof SocialForm, string | null>>>({})
  const [localError, setLocalError] = useState<string | null>(null)
  const categoryName = /^\d+$/.test(primaryCategory.trim()) ? '' : primaryCategory.trim()

  return (
    <EditorDialog title="Social Links" onClose={onClose} busy={busy}>
      <form
        onSubmit={(event) => {
          event.preventDefault()
          const nextErrors: Partial<Record<keyof SocialForm, string | null>> = {}
          for (const [key] of SOCIAL_FIELDS) nextErrors[key] = socialLinkError(form[key])
          setFieldErrors(nextErrors)
          if (Object.values(nextErrors).some(Boolean)) {
            setLocalError(SOCIAL_LINK_ERROR)
            return
          }
          setLocalError(null)
          const next = { ...form }
          for (const [key] of SOCIAL_FIELDS) next[key] = normalizeSocialLink(form[key])
          onSubmit(next)
        }}
      >
        {error || localError ? <p style={{ color: '#b42318' }}>{error || localError}</p> : null}
        {SOCIAL_FIELDS.map(([key, label]) => (
          <EditorField label={label} key={key}>
            <input
              placeholder={SOCIAL_LINK_PLACEHOLDER}
              value={form[key]}
              onChange={(e) => {
                const value = e.target.value
                setForm({ ...form, [key]: value })
                setFieldErrors((current) => ({ ...current, [key]: current[key] ? socialLinkError(value) : null }))
              }}
            />
            {key === 'google' ? (
              <span style={{ display: 'block', marginTop: 4, color: '#64748b', fontWeight: 500 }}>
                {categoryName ? `Google link for your ${categoryName} business` : 'Paste a Google Business, Maps, or review link'}
              </span>
            ) : null}
            {fieldErrors[key] ? <span style={{ display: 'block', color: '#b42318', fontWeight: 500, marginTop: 4 }}>{fieldErrors[key]}</span> : null}
          </EditorField>
        ))}
        <EditorActions>
          <button className="mw-editor-primary" type="submit" disabled={busy}>
            {busy ? 'Saving...' : 'Save'}
          </button>
        </EditorActions>
      </form>
    </EditorDialog>
  )
}
