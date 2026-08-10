import { useEffect, useState } from 'react'
import { Link, useOutletContext } from 'react-router-dom'
import { ApiError } from '@/shared/api/client'
import { stepPath } from '@/features/website/cardContext'
import { websiteApi } from '@/features/website/websiteApi'
import type { WebsiteOutletContext } from '@/features/website/WebsiteBuilderLayout'
import { TablePagination, useClientPagination } from '@/components/table'

function asList(raw: unknown) {
  return Array.isArray(raw) ? raw : []
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

export function ServicesPage() {
  const { cardId, email } = useOutletContext<WebsiteOutletContext>()
  const [items, setItems] = useState<ReturnType<typeof row>[]>([])
  const [name, setName] = useState('')
  const [desc, setDesc] = useState('')
  const [image, setImage] = useState('')
  const [err, setErr] = useState<string | null>(null)

  async function load() {
    if (!cardId) return
    const raw = await websiteApi.listServices(cardId, email)
    setItems(asList(raw).map((x) => row((x ?? {}) as Record<string, unknown>)))
  }

  useEffect(() => {
    void load().catch((e) => setErr(e instanceof Error ? e.message : 'Load failed'))
  }, [cardId, email])

  async function add(e: React.FormEvent) {
    e.preventDefault()
    if (!cardId) return
    try {
      await websiteApi.createService(cardId, email, {
        productName: name,
        productDescription: desc,
        productImage: image || null,
      })
      setName('')
      setDesc('')
      setImage('')
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
      <form onSubmit={add} className="row g-2 mb-3">
        <div className="col-md-4">
          <input className="form-control" placeholder="Service name*" required value={name} onChange={(e) => setName(e.target.value)} />
        </div>
        <div className="col-md-4">
          <input className="form-control" placeholder="Description" value={desc} onChange={(e) => setDesc(e.target.value)} />
        </div>
        <div className="col-md-3">
          <input className="form-control" placeholder="Image filename" value={image} onChange={(e) => setImage(e.target.value)} />
        </div>
        <div className="col-md-1">
          <button className="btn btn-primary w-100" type="submit">
            Add
          </button>
        </div>
      </form>
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
    try {
      await websiteApi.createProduct(cardId, email, {
        ...form,
        mrp: Number(form.mrp),
        sellingPrice: Number(form.sellingPrice),
        productImage: form.productImage || null,
      })
      setForm({ productName: '', productDescription: '', productImage: '', mrp: '0', sellingPrice: '0', priceType: 'Fixed', pricingUnit: '', ctaText: 'Buy Now' })
      await load()
    } catch (ex) {
      setErr(ex instanceof ApiError ? ex.message : 'Save failed')
    }
  }

  return (
    <CrudShell title="Details & Pricing" back={stepPath('/website/special-offers', cardId)} next={stepPath('/website/videos', cardId)} err={err}>
      <form onSubmit={add} className="row g-2 mb-3">
        <div className="col-md-3">
          <input className="form-control" placeholder="Product*" required value={form.productName} onChange={(e) => setForm({ ...form, productName: e.target.value })} />
        </div>
        <div className="col-md-2">
          <input className="form-control" placeholder="MRP" value={form.mrp} onChange={(e) => setForm({ ...form, mrp: e.target.value })} />
        </div>
        <div className="col-md-2">
          <input className="form-control" placeholder="Price" value={form.sellingPrice} onChange={(e) => setForm({ ...form, sellingPrice: e.target.value })} />
        </div>
        <div className="col-md-3">
          <input className="form-control" placeholder="Image filename" value={form.productImage} onChange={(e) => setForm({ ...form, productImage: e.target.value })} />
        </div>
        <div className="col-md-2">
          <button className="btn btn-primary w-100" type="submit">
            Add
          </button>
        </div>
        <div className="col-12">
          <input className="form-control" placeholder="Description" value={form.productDescription} onChange={(e) => setForm({ ...form, productDescription: e.target.value })} />
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
      await load()
    } catch (ex) {
      setErr(ex instanceof ApiError ? ex.message : 'Save failed')
    }
  }

  return (
    <CrudShell title="Special Offers" back={stepPath('/website/services', cardId)} next={stepPath('/website/products', cardId)} err={err}>
      <form onSubmit={add} className="row g-2 mb-3">
        <div className="col-md-4">
          <input className="form-control" placeholder="Title*" required value={form.offerTitle} onChange={(e) => setForm({ ...form, offerTitle: e.target.value })} />
        </div>
        <div className="col-md-2">
          <input className="form-control" placeholder="Discount %" value={form.discountPercentage} onChange={(e) => setForm({ ...form, discountPercentage: e.target.value })} />
        </div>
        <div className="col-md-3">
          <input className="form-control" placeholder="Image filename" value={form.offerImage} onChange={(e) => setForm({ ...form, offerImage: e.target.value })} />
        </div>
        <div className="col-md-2">
          <button className="btn btn-primary w-100" type="submit">
            Add
          </button>
        </div>
        <div className="col-12">
          <input className="form-control" placeholder="Description" value={form.offerDescription} onChange={(e) => setForm({ ...form, offerDescription: e.target.value })} />
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
  const [filename, setFilename] = useState('')
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
    try {
      await websiteApi.createGallery(cardId, email, { galleryImage: filename })
      setFilename('')
      await load()
    } catch (ex) {
      setErr(ex instanceof ApiError ? ex.message : 'Save failed')
    }
  }

  return (
    <CrudShell title="Image Gallery" back={stepPath('/website/videos', cardId)} next={stepPath('/website/payment-details', cardId)} err={err}>
      <form onSubmit={add} className="row g-2 mb-3">
        <div className="col-md-8">
          <input className="form-control" placeholder="Gallery image filename*" required value={filename} onChange={(e) => setFilename(e.target.value)} />
        </div>
        <div className="col-md-4">
          <button className="btn btn-primary w-100" type="submit">
            Add Image
          </button>
        </div>
      </form>
      <ItemTable
        rows={items.map((i) => ({ id: i.id, title: i.galleryImage || `Image #${i.id}`, extra: '', img: i.galleryImageUrl }))}
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
    <div className="card">
      <div className="card-body">
        <h3 className="mb-3">{title}</h3>
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
