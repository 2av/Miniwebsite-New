import { useEffect, useRef, useState } from 'react'
import { Link, useOutletContext } from 'react-router-dom'
import { ApiError } from '@/shared/api/client'
import { stepPath } from '@/features/website/cardContext'
import { websiteApi } from '@/features/website/websiteApi'
import type { WebsiteOutletContext } from '@/features/website/WebsiteBuilderLayout'
import { TablePagination, useClientPagination } from '@/components/table'
import { PortalPageHeader } from '@/components/layout/PortalPageHeader'

function asList(raw: unknown) {
  return Array.isArray(raw) ? raw : []
}

function moneyAmount(value: string): number | null {
  const text = value.trim().replace(/,/g, '')
  if (!text) return 0
  const amount = Number(text)
  return Number.isFinite(amount) ? amount : null
}

function row(o: Record<string, unknown>) {
  return {
    id: Number(o.id ?? o.Id),
    productName: String(o.productName ?? o.ProductName ?? ''),
    productDescription: String(o.productDescription ?? o.ProductDescription ?? ''),
    productImage: String(o.productImage ?? o.ProductImage ?? ''),
    productImageUrl: String(o.productImageUrl ?? o.ProductImageUrl ?? ''),
    displayOrder: Number(o.displayOrder ?? o.DisplayOrder ?? 0),
    mrp: Number(o.mrp ?? o.Mrp ?? 0),
    sellingPrice: Number(o.sellingPrice ?? o.SellingPrice ?? 0),
    priceType: String(o.priceType ?? o.PriceType ?? ''),
    pricingUnit: String(o.pricingUnit ?? o.PricingUnit ?? ''),
    ctaText: String(o.ctaText ?? o.CtaText ?? ''),
    offerTitle: String(o.offerTitle ?? o.OfferTitle ?? ''),
    offerDescription: String(o.offerDescription ?? o.OfferDescription ?? ''),
    offerImageUrl: String(o.offerImageUrl ?? o.OfferImageUrl ?? ''),
    badge: String(o.badge ?? o.Badge ?? ''),
    discountPercentage: Number(o.discountPercentage ?? o.DiscountPercentage ?? 0),
    status: String(o.status ?? o.Status ?? 'Active'),
    galleryImage: String(o.galleryImage ?? o.GalleryImage ?? ''),
    galleryImageUrl: String(o.galleryImageUrl ?? o.GalleryImageUrl ?? ''),
  }
}

function WebsiteImageBox({
  cardId,
  email,
  kind,
  imageKey,
  imageUrl,
  onChange,
}: {
  cardId: number | null
  email: string
  kind: 'product' | 'offer' | 'gallery'
  imageKey: string
  imageUrl: string
  onChange: (key: string, url: string) => void
}) {
  const [uploading, setUploading] = useState(false)
  const [uploadPercent, setUploadPercent] = useState(0)
  const [imageError, setImageError] = useState<string | null>(null)

  async function onPick(file: File | null) {
    if (!file || !cardId) return
    const allowed = ['image/jpeg', 'image/jpg', 'image/png', 'image/gif', 'image/webp']
    const extOk = /\.(jpe?g|png|gif|webp)$/i.test(file.name)
    if (!allowed.includes(file.type) && !(file.type === '' && extOk) && !file.type.startsWith('image/')) {
      setImageError('Use a JPG, PNG, GIF, or WebP image.')
      return
    }
    if (file.size > 2_500_000) {
      setImageError('Image is too large (max 2.5MB).')
      return
    }
    const previous = imageKey
    setImageError(null)
    setUploading(true)
    setUploadPercent(0)
    try {
      const res = await websiteApi.uploadCardImage(cardId, email, kind, file, setUploadPercent)
      const data = (res.data ?? {}) as Record<string, unknown>
      const key = String(data.imageKey ?? data.ImageKey ?? '')
      const url = String(data.imageUrl ?? data.ImageUrl ?? '')
      setUploadPercent(100)
      onChange(key, url ? `${url}${url.includes('?') ? '&' : '?'}t=${Date.now()}` : '')
      if (previous) void websiteApi.deleteCardImage(cardId, email, previous)
    } catch (ex) {
      setImageError(ex instanceof ApiError ? ex.message : 'Upload failed')
    } finally {
      setUploading(false)
    }
  }

  return (
    <>
      <label className={`company-image-box service-image-box${uploading ? ' is-busy' : ''}`}>
        {uploading ? (
          <div className="company-image-progress">
            <span>Uploading {uploadPercent}%</span>
            <div className="company-image-track" aria-hidden>
              <div className="company-image-fill" style={{ width: `${uploadPercent}%` }} />
            </div>
          </div>
        ) : imageUrl ? (
          <img src={imageUrl} alt="" />
        ) : imageError ? (
          <p className="company-image-note">{imageError}</p>
        ) : (
          <span>Choose image</span>
        )}
        {imageUrl && !uploading ? (
          <button
            type="button"
            className="company-image-remove"
            aria-label="Remove image"
            onClick={(e) => {
              e.preventDefault()
              e.stopPropagation()
              const key = imageKey
              onChange('', '')
              setImageError(null)
              if (cardId && key) void websiteApi.deleteCardImage(cardId, email, key)
            }}
          >
            <i className="fa fa-times" aria-hidden />
          </button>
        ) : null}
        <input
          type="file"
          accept=".jpg,.jpeg,.png,.gif,.webp,image/*"
          className="company-image-input"
          disabled={uploading}
          onChange={(e) => {
            const picked = e.target.files?.[0] ?? null
            e.target.value = ''
            void onPick(picked)
          }}
        />
      </label>
      <style>{`
        .service-image-box.company-image-box { width: 120px; height: 120px; margin: 0; }
        .company-image-box {
          position: relative; border: 2px solid darkgray; overflow: hidden; display: flex;
          align-items: center; justify-content: center; cursor: pointer; background: #fff;
        }
        .company-image-box.is-busy { cursor: default; }
        .company-image-box span { font-size: 13px; font-weight: 500; color: #666; text-align: center; }
        .company-image-box img { max-width: 100%; max-height: 100%; object-fit: contain; }
        .company-image-note { margin: 0; padding: 0 8px; font-size: 11px; line-height: 1.35; text-align: center; color: #b42318; }
        .company-image-remove {
          position: absolute; top: 6px; right: 6px; z-index: 2; width: 22px; height: 22px; padding: 0;
          border: 0; border-radius: 50%; background: rgba(15, 23, 42, 0.72); color: #fff;
          display: flex; align-items: center; justify-content: center; cursor: pointer; font-size: 12px; line-height: 1;
        }
        .company-image-progress { display: flex; flex-direction: column; align-items: center; gap: 6px; width: 100%; padding: 8px; }
        .company-image-progress span { font-size: 11px; font-weight: 600; color: #334155; }
        .company-image-track { width: 80%; height: 4px; border-radius: 999px; background: #e2e8f0; overflow: hidden; }
        .company-image-fill { height: 100%; background: #007bff; }
        .company-image-input {
          position: absolute !important; width: 1px !important; height: 1px !important; padding: 0 !important;
          margin: -1px !important; overflow: hidden !important; clip: rect(0, 0, 0, 0) !important;
          white-space: nowrap !important; border: 0 !important; opacity: 0 !important;
        }
      `}</style>
    </>
  )
}

export function ServicesPage() {
  const { cardId, email } = useOutletContext<WebsiteOutletContext>()
  const [items, setItems] = useState<ReturnType<typeof row>[]>([])
  const [name, setName] = useState('')
  const [desc, setDesc] = useState('')
  const [imageKey, setImageKey] = useState('')
  const [imageUrl, setImageUrl] = useState('')
  const [uploading, setUploading] = useState(false)
  const [uploadPercent, setUploadPercent] = useState(0)
  const [imageError, setImageError] = useState<string | null>(null)
  const [err, setErr] = useState<string | null>(null)
  const imageRef = useRef<HTMLInputElement>(null)

  async function load() {
    if (!cardId) return
    const raw = await websiteApi.listServices(cardId, email)
    setItems(asList(raw).map((x) => row((x ?? {}) as Record<string, unknown>)))
  }

  useEffect(() => {
    void load().catch((e) => setErr(e instanceof Error ? e.message : 'Load failed'))
  }, [cardId, email])

  async function onPickServiceImage(file: File | null) {
    if (!file || !cardId) return
    const allowed = ['image/jpeg', 'image/jpg', 'image/png', 'image/gif', 'image/webp']
    const extOk = /\.(jpe?g|png|gif|webp)$/i.test(file.name)
    if (!allowed.includes(file.type) && !(file.type === '' && extOk) && !file.type.startsWith('image/')) {
      setImageError('Use a JPG, PNG, GIF, or WebP image.')
      return
    }
    if (file.size > 2_500_000) {
      setImageError('Image is too large (max 2.5MB).')
      return
    }
    const previous = imageKey
    setImageError(null)
    setUploading(true)
    setUploadPercent(0)
    try {
      const res = await websiteApi.uploadServiceImage(cardId, email, file, setUploadPercent)
      const data = (res.data ?? {}) as Record<string, unknown>
      const key = String(data.imageKey ?? data.ImageKey ?? '')
      const url = String(data.imageUrl ?? data.ImageUrl ?? '')
      setUploadPercent(100)
      setImageKey(key)
      setImageUrl(url ? `${url}${url.includes('?') ? '&' : '?'}t=${Date.now()}` : '')
      if (previous) void websiteApi.deleteServiceImage(cardId, email, previous)
    } catch (ex) {
      setImageError(ex instanceof ApiError ? ex.message : 'Upload failed')
    } finally {
      setUploading(false)
    }
  }

  async function add(e: React.FormEvent) {
    e.preventDefault()
    if (!cardId) return
    try {
      await websiteApi.createService(cardId, email, {
        productName: name,
        productDescription: desc,
        productImage: imageKey || null,
      })
      setName('')
      setDesc('')
      setImageKey('')
      setImageUrl('')
      setImageError(null)
      await load()
    } catch (ex) {
      setErr(ex instanceof ApiError ? ex.message : 'Save failed')
    }
  }

  return (
    <CrudShell
      title="Services"
      back={stepPath('/website/company-details', cardId)}
      next={stepPath('/website/special-offers', cardId)}
      err={err}
    >
      <form onSubmit={add} className="row g-2 mb-3 align-items-start">
        <div className="col-md-4">
          <input className="form-control" placeholder="Service name*" required value={name} onChange={(e) => setName(e.target.value)} />
        </div>
        <div className="col-md-4">
          <input className="form-control" placeholder="Description" value={desc} onChange={(e) => setDesc(e.target.value)} />
        </div>
        <div className="col-md-3">
          <label className={`company-image-box service-image-box${uploading ? ' is-busy' : ''}`}>
            {uploading ? (
              <div className="company-image-progress">
                <span>Uploading {uploadPercent}%</span>
                <div className="company-image-track" aria-hidden>
                  <div className="company-image-fill" style={{ width: `${uploadPercent}%` }} />
                </div>
              </div>
            ) : imageUrl ? (
              <img src={imageUrl} alt="Service" />
            ) : imageError ? (
              <p className="company-image-note">{imageError}</p>
            ) : (
              <span>Choose image</span>
            )}
            {imageUrl && !uploading ? (
              <button
                type="button"
                className="company-image-remove"
                aria-label="Remove service image"
                onClick={(e) => {
                  e.preventDefault()
                  e.stopPropagation()
                  const key = imageKey
                  setImageKey('')
                  setImageUrl('')
                  setImageError(null)
                  if (cardId && key) void websiteApi.deleteServiceImage(cardId, email, key)
                }}
              >
                <i className="fa fa-times" aria-hidden />
              </button>
            ) : null}
            <input
              ref={imageRef}
              type="file"
              accept=".jpg,.jpeg,.png,.gif,.webp,image/*"
              className="company-image-input"
              disabled={uploading}
              onChange={(e) => {
                const picked = e.target.files?.[0] ?? null
                e.target.value = ''
                void onPickServiceImage(picked)
              }}
            />
          </label>
        </div>
        <div className="col-md-1">
          <button className="btn btn-primary w-100" type="submit" disabled={uploading}>
            Add
          </button>
        </div>
      </form>
      <style>{`
        .service-image-box.company-image-box {
          width: 120px;
          height: 120px;
          margin: 0;
        }
        .company-image-box {
          position: relative;
          border: 2px solid darkgray;
          overflow: hidden;
          display: flex;
          align-items: center;
          justify-content: center;
          cursor: pointer;
          background: #fff;
        }
        .company-image-box.is-busy { cursor: default; }
        .company-image-box span { font-size: 13px; font-weight: 500; color: #666; text-align: center; }
        .company-image-box img { max-width: 100%; max-height: 100%; object-fit: contain; }
        .company-image-note { margin: 0; padding: 0 8px; font-size: 11px; line-height: 1.35; text-align: center; color: #b42318; }
        .company-image-remove {
          position: absolute; top: 6px; right: 6px; z-index: 2; width: 22px; height: 22px; padding: 0;
          border: 0; border-radius: 50%; background: rgba(15, 23, 42, 0.72); color: #fff;
          display: flex; align-items: center; justify-content: center; cursor: pointer; font-size: 12px; line-height: 1;
        }
        .company-image-progress { display: flex; flex-direction: column; align-items: center; gap: 6px; width: 100%; padding: 8px; }
        .company-image-progress span { font-size: 11px; font-weight: 600; color: #334155; }
        .company-image-track { width: 80%; height: 4px; border-radius: 999px; background: #e2e8f0; overflow: hidden; }
        .company-image-fill { height: 100%; background: #007bff; }
        .company-image-input {
          position: absolute !important; width: 1px !important; height: 1px !important; padding: 0 !important;
          margin: -1px !important; overflow: hidden !important; clip: rect(0, 0, 0, 0) !important;
          white-space: nowrap !important; border: 0 !important; opacity: 0 !important;
        }
      `}</style>
      <ItemTable
        rows={items.map((i) => ({ id: i.id, title: i.productName, extra: i.productDescription, img: i.productImageUrl }))}
        onDelete={async (id) => {
          if (!cardId) return
          await websiteApi.deleteService(cardId, id, email)
          await load()
        }}
      />
    </CrudShell>
  )
}

export function ProductsPage() {
  const { cardId, email } = useOutletContext<WebsiteOutletContext>()
  const [items, setItems] = useState<ReturnType<typeof row>[]>([])
  const [form, setForm] = useState({ productName: '', productDescription: '', productImage: '', mrp: '0', sellingPrice: '0', priceType: 'Fixed', pricingUnit: '', ctaText: 'Buy Now' })
  const [imageUrl, setImageUrl] = useState('')
  const [err, setErr] = useState<string | null>(null)

  async function load() {
    if (!cardId) return
    const raw = await websiteApi.listProducts(cardId, email)
    setItems(asList(raw).map((x) => row((x ?? {}) as Record<string, unknown>)))
  }

  useEffect(() => {
    void load().catch((e) => setErr(e instanceof Error ? e.message : 'Load failed'))
  }, [cardId, email])

  async function add(e: React.FormEvent) {
    e.preventDefault()
    if (!cardId) return
    const mrp = moneyAmount(form.mrp)
    const sellingPrice = moneyAmount(form.sellingPrice)
    if (mrp == null || sellingPrice == null) {
      setErr('Enter a valid MRP and price. Use numbers only.')
      return
    }
    try {
      setErr(null)
      await websiteApi.createProduct(cardId, email, {
        ...form,
        mrp,
        sellingPrice,
        productImage: form.productImage || null,
      })
      setForm({ productName: '', productDescription: '', productImage: '', mrp: '0', sellingPrice: '0', priceType: 'Fixed', pricingUnit: '', ctaText: 'Buy Now' })
      setImageUrl('')
      await load()
    } catch (ex) {
      setErr(ex instanceof ApiError ? ex.message : 'Save failed')
    }
  }

  return (
    <CrudShell title="Details & Pricing" back={stepPath('/website/special-offers', cardId)} next={stepPath('/website/videos', cardId)} err={err}>
      <form onSubmit={add} className="row g-2 mb-3">
        <div className="col-md-3">
          <input className="form-control" placeholder="Product*" required value={form.productName} onChange={(e) => setForm((current) => ({ ...current, productName: e.target.value }))} />
        </div>
        <div className="col-md-2">
          <input className="form-control" placeholder="MRP" value={form.mrp} onChange={(e) => setForm((current) => ({ ...current, mrp: e.target.value }))} />
        </div>
        <div className="col-md-2">
          <input className="form-control" placeholder="Price" value={form.sellingPrice} onChange={(e) => setForm((current) => ({ ...current, sellingPrice: e.target.value }))} />
        </div>
        <div className="col-md-3">
          <WebsiteImageBox
            cardId={cardId}
            email={email}
            kind="product"
            imageKey={form.productImage}
            imageUrl={imageUrl}
            onChange={(key, url) => {
              setForm((current) => ({ ...current, productImage: key }))
              setImageUrl(url)
            }}
          />
        </div>
        <div className="col-md-2">
          <button className="btn btn-primary w-100" type="submit">
            Add
          </button>
        </div>
        <div className="col-12">
          <input className="form-control" placeholder="Description" value={form.productDescription} onChange={(e) => setForm((current) => ({ ...current, productDescription: e.target.value }))} />
        </div>
      </form>
      <ItemTable
        rows={items.map((i) => ({
          id: i.id,
          title: i.productName,
          extra: `₹${i.sellingPrice} (MRP ₹${i.mrp})`,
          img: i.productImageUrl,
        }))}
        onDelete={async (id) => {
          if (!cardId) return
          await websiteApi.deleteProduct(cardId, id, email)
          await load()
        }}
      />
    </CrudShell>
  )
}

export function SpecialOffersPage() {
  const { cardId, email } = useOutletContext<WebsiteOutletContext>()
  const [items, setItems] = useState<ReturnType<typeof row>[]>([])
  const [form, setForm] = useState({ offerTitle: '', offerDescription: '', offerImage: '', badge: 'Offer', discountPercentage: '0', status: 'Active' })
  const [imageUrl, setImageUrl] = useState('')
  const [err, setErr] = useState<string | null>(null)

  async function load() {
    if (!cardId) return
    const raw = await websiteApi.listOffers(cardId, email)
    setItems(asList(raw).map((x) => row((x ?? {}) as Record<string, unknown>)))
  }

  useEffect(() => {
    void load().catch((e) => setErr(e instanceof Error ? e.message : 'Load failed'))
  }, [cardId, email])

  async function add(e: React.FormEvent) {
    e.preventDefault()
    if (!cardId) return
    try {
      await websiteApi.createOffer(cardId, email, {
        ...form,
        discountPercentage: Number(form.discountPercentage),
        offerImage: form.offerImage || null,
      })
      setForm({ offerTitle: '', offerDescription: '', offerImage: '', badge: 'Offer', discountPercentage: '0', status: 'Active' })
      setImageUrl('')
      await load()
    } catch (ex) {
      setErr(ex instanceof ApiError ? ex.message : 'Save failed')
    }
  }

  return (
    <CrudShell title="Special Offers" back={stepPath('/website/services', cardId)} next={stepPath('/website/products', cardId)} err={err}>
      <form onSubmit={add} className="row g-2 mb-3">
        <div className="col-md-4">
          <input className="form-control" placeholder="Title*" required value={form.offerTitle} onChange={(e) => setForm((current) => ({ ...current, offerTitle: e.target.value }))} />
        </div>
        <div className="col-md-2">
          <input className="form-control" placeholder="Discount %" value={form.discountPercentage} onChange={(e) => setForm((current) => ({ ...current, discountPercentage: e.target.value }))} />
        </div>
        <div className="col-md-3">
          <WebsiteImageBox
            cardId={cardId}
            email={email}
            kind="offer"
            imageKey={form.offerImage}
            imageUrl={imageUrl}
            onChange={(key, url) => {
              setForm((current) => ({ ...current, offerImage: key }))
              setImageUrl(url)
            }}
          />
        </div>
        <div className="col-md-2">
          <button className="btn btn-primary w-100" type="submit">
            Add
          </button>
        </div>
        <div className="col-12">
          <input className="form-control" placeholder="Description" value={form.offerDescription} onChange={(e) => setForm((current) => ({ ...current, offerDescription: e.target.value }))} />
        </div>
      </form>
      <ItemTable
        rows={items.map((i) => ({ id: i.id, title: i.offerTitle, extra: `${i.discountPercentage}% · ${i.status}`, img: i.offerImageUrl }))}
        onDelete={async (id) => {
          if (!cardId) return
          await websiteApi.deleteOffer(cardId, id, email)
          await load()
        }}
      />
    </CrudShell>
  )
}

export function ImageGalleryPage() {
  const { cardId, email } = useOutletContext<WebsiteOutletContext>()
  const [items, setItems] = useState<ReturnType<typeof row>[]>([])
  const [imageKey, setImageKey] = useState('')
  const [imageUrl, setImageUrl] = useState('')
  const [err, setErr] = useState<string | null>(null)

  async function load() {
    if (!cardId) return
    const raw = await websiteApi.listGallery(cardId, email)
    setItems(asList(raw).map((x) => row((x ?? {}) as Record<string, unknown>)))
  }

  useEffect(() => {
    void load().catch((e) => setErr(e instanceof Error ? e.message : 'Load failed'))
  }, [cardId, email])

  async function add(e: React.FormEvent) {
    e.preventDefault()
    if (!cardId) return
    if (!imageKey) {
      setErr('Choose an image first.')
      return
    }
    try {
      setErr(null)
      await websiteApi.createGallery(cardId, email, { galleryImage: imageKey })
      setImageKey('')
      setImageUrl('')
      await load()
    } catch (ex) {
      setErr(ex instanceof ApiError ? ex.message : 'Save failed')
    }
  }

  return (
    <CrudShell title="Image Gallery" back={stepPath('/website/videos', cardId)} next={stepPath('/website/payment-details', cardId)} err={err}>
      <form onSubmit={add} className="row g-2 mb-3 align-items-start">
        <div className="col-md-8">
          <WebsiteImageBox
            cardId={cardId}
            email={email}
            kind="gallery"
            imageKey={imageKey}
            imageUrl={imageUrl}
            onChange={(key, url) => {
              setImageKey(key)
              setImageUrl(url)
              setErr(null)
            }}
          />
        </div>
        <div className="col-md-4">
          <button className="btn btn-primary" type="submit">
            Add Image
          </button>
        </div>
      </form>
      <ItemTable
        rows={items.map((i) => ({ id: i.id, title: `Image #${i.id}`, extra: '', img: i.galleryImageUrl }))}
        onDelete={async (id) => {
          if (!cardId) return
          await websiteApi.deleteGallery(cardId, id, email)
          await load()
        }}
      />
    </CrudShell>
  )
}

function CrudShell({
  title,
  back,
  next,
  err,
  children,
}: {
  title: string
  back: string
  next: string
  err: string | null
  children: React.ReactNode
}) {
  return (
    <>
      <PortalPageHeader title={title} />
      <div className="card">
      <div className="card-body">
        {err && <div className="alert alert-danger">{err}</div>}
        {children}
        <div className="d-flex gap-2 mt-3">
          <Link className="btn btn-outline-secondary" to={back}>
            Back
          </Link>
          <Link className="btn btn-outline-primary" to={next}>
            Next
          </Link>
        </div>
      </div>
    </div>
    </>
  )
}

function ItemTable({
  rows,
  onDelete,
}: {
  rows: Array<{ id: number; title: string; extra: string; img?: string }>
  onDelete: (id: number) => Promise<void>
}) {
  const pager = useClientPagination(rows.length)
  const pagedRows = pager.slice(rows)

  return (
    <div className="table-responsive">
      <table className="table table-bordered">
        <thead>
          <tr>
            <th>Preview</th>
            <th>Title</th>
            <th>Details</th>
            <th />
          </tr>
        </thead>
        <tbody>
          {rows.length === 0 ? (
            <tr>
              <td colSpan={4} className="text-center">
                No items yet.
              </td>
            </tr>
          ) : (
            pagedRows.map((r) => (
              <tr key={r.id}>
                <td style={{ width: 80 }}>
                  {r.img ? <img src={r.img} alt="" style={{ width: 56, height: 56, objectFit: 'cover' }} /> : '-'}
                </td>
                <td>{r.title}</td>
                <td>{r.extra}</td>
                <td>
                  <button type="button" className="btn btn-sm btn-danger" onClick={() => void onDelete(r.id)}>
                    Delete
                  </button>
                </td>
              </tr>
            ))
          )}
        </tbody>
      </table>
      {pager.showPager ? (
        <TablePagination
          total={rows.length}
          page={pager.page}
          pageSize={pager.pageSize}
          onPageChange={pager.setPage}
          onPageSizeChange={pager.setPageSize}
        />
      ) : null}
    </div>
  )
}
