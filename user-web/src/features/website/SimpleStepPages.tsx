import { useEffect, useState } from 'react'
import { Link, useOutletContext } from 'react-router-dom'
import { ApiError } from '@/shared/api/client'
import { stepPath } from '@/features/website/cardContext'
import { pickVal, websiteApi } from '@/features/website/websiteApi'
import type { WebsiteOutletContext } from '@/features/website/WebsiteBuilderLayout'

export function VideosPage() {
  const { cardId, email } = useOutletContext<WebsiteOutletContext>()
  const [urls, setUrls] = useState<string[]>(Array(10).fill(''))
  const [msg, setMsg] = useState<string | null>(null)
  const [err, setErr] = useState<string | null>(null)

  useEffect(() => {
    if (!cardId) return
    ;(async () => {
      try {
        const raw = await websiteApi.getVideos(cardId, email)
        const list = ((raw.urls ?? raw.Urls) as string[]) || []
        const next = Array(10).fill('')
        list.slice(0, 10).forEach((u, i) => {
          next[i] = u || ''
        })
        setUrls(next)
      } catch (e) {
        setErr(e instanceof Error ? e.message : 'Failed to load')
      }
    })()
  }, [cardId, email])

  async function save(e: React.FormEvent) {
    e.preventDefault()
    if (!cardId) return
    try {
      const res = await websiteApi.putVideos(cardId, email, urls.map((u) => u.trim() || null))
      setMsg(res.message || 'Saved')
    } catch (ex) {
      setErr(ex instanceof ApiError ? ex.message : 'Save failed')
    }
  }

  return (
    <div className="card">
      <div className="card-body">
        <h3 className="mb-3">YouTube Videos</h3>
        {err && <div className="alert alert-danger">{err}</div>}
        {msg && <div className="alert alert-success">{msg}</div>}
        <form onSubmit={save} className="row g-2">
          {urls.map((u, i) => (
            <div className="col-md-6" key={i}>
              <input
                className="form-control"
                placeholder={`YouTube URL ${i + 1}`}
                value={u}
                onChange={(e) => setUrls((arr) => arr.map((x, idx) => (idx === i ? e.target.value : x)))}
              />
            </div>
          ))}
          <div className="col-12 d-flex gap-2 mt-2">
            <Link className="btn btn-outline-secondary" to={stepPath('/website/products', cardId)}>
              Back
            </Link>
            <button type="submit" className="btn btn-primary">
              Save
            </button>
            <Link className="btn btn-outline-primary" to={stepPath('/website/image-gallery', cardId)}>
              Next
            </Link>
          </div>
        </form>
      </div>
    </div>
  )
}

export function PaymentDetailsPage() {
  const { cardId, email } = useOutletContext<WebsiteOutletContext>()
  const [form, setForm] = useState({
    paytm: '',
    googlePay: '',
    phonePay: '',
    accountNo: '',
    ifsc: '',
    acName: '',
    bankName: '',
    acType: '',
  })
  const [msg, setMsg] = useState<string | null>(null)
  const [err, setErr] = useState<string | null>(null)

  useEffect(() => {
    if (!cardId) return
    ;(async () => {
      try {
        const raw = await websiteApi.getPayment(cardId, email)
        setForm({
          paytm: String(pickVal(raw, 'paytm', 'Paytm') ?? ''),
          googlePay: String(pickVal(raw, 'googlePay', 'GooglePay') ?? ''),
          phonePay: String(pickVal(raw, 'phonePay', 'PhonePay') ?? ''),
          accountNo: String(pickVal(raw, 'accountNo', 'AccountNo') ?? ''),
          ifsc: String(pickVal(raw, 'ifsc', 'Ifsc') ?? ''),
          acName: String(pickVal(raw, 'acName', 'AcName') ?? ''),
          bankName: String(pickVal(raw, 'bankName', 'BankName') ?? ''),
          acType: String(pickVal(raw, 'acType', 'AcType') ?? ''),
        })
      } catch (e) {
        setErr(e instanceof Error ? e.message : 'Failed to load')
      }
    })()
  }, [cardId, email])

  async function save(e: React.FormEvent) {
    e.preventDefault()
    if (!cardId) return
    try {
      const res = await websiteApi.putPayment(cardId, email, form)
      setMsg(res.message || 'Saved')
    } catch (ex) {
      setErr(ex instanceof ApiError ? ex.message : 'Save failed')
    }
  }

  return (
    <div className="card">
      <div className="card-body">
        <h3 className="mb-3">Payment Details</h3>
        {err && <div className="alert alert-danger">{err}</div>}
        {msg && <div className="alert alert-success">{msg}</div>}
        <form onSubmit={save} className="row g-3">
          {(
            [
              ['googlePay', 'Google Pay'],
              ['phonePay', 'PhonePe'],
              ['paytm', 'Paytm'],
              ['bankName', 'Bank Name'],
              ['acName', 'Account Name'],
              ['accountNo', 'Account Number'],
              ['ifsc', 'IFSC'],
              ['acType', 'Account Type / GST label'],
            ] as const
          ).map(([k, label]) => (
            <div className="col-md-6" key={k}>
              <label className="form-label">{label}</label>
              <input className="form-control" value={form[k]} onChange={(e) => setForm({ ...form, [k]: e.target.value })} />
            </div>
          ))}
          <div className="col-12 d-flex gap-2">
            <Link className="btn btn-outline-secondary" to={stepPath('/website/image-gallery', cardId)}>
              Back
            </Link>
            <button type="submit" className="btn btn-primary">
              Save
            </button>
            <Link className="btn btn-outline-primary" to={stepPath('/website/social-links', cardId)}>
              Next
            </Link>
          </div>
        </form>
      </div>
    </div>
  )
}

export function SocialLinksPage() {
  const { cardId, email } = useOutletContext<WebsiteOutletContext>()
  const [form, setForm] = useState({
    fb: '',
    twitter: '',
    instagram: '',
    linkedin: '',
    youtube: '',
    pinterest: '',
  })
  const [msg, setMsg] = useState<string | null>(null)
  const [err, setErr] = useState<string | null>(null)

  useEffect(() => {
    if (!cardId) return
    ;(async () => {
      try {
        const raw = await websiteApi.getSocial(cardId, email)
        setForm({
          fb: String(pickVal(raw, 'fb', 'Fb') ?? ''),
          twitter: String(pickVal(raw, 'twitter', 'Twitter') ?? ''),
          instagram: String(pickVal(raw, 'instagram', 'Instagram') ?? ''),
          linkedin: String(pickVal(raw, 'linkedin', 'Linkedin') ?? ''),
          youtube: String(pickVal(raw, 'youtube', 'Youtube') ?? ''),
          pinterest: String(pickVal(raw, 'pinterest', 'Pinterest') ?? ''),
        })
      } catch (e) {
        setErr(e instanceof Error ? e.message : 'Failed to load')
      }
    })()
  }, [cardId, email])

  async function save(e: React.FormEvent) {
    e.preventDefault()
    if (!cardId) return
    try {
      const res = await websiteApi.putSocial(cardId, email, form)
      setMsg(res.message || 'Saved')
    } catch (ex) {
      setErr(ex instanceof ApiError ? ex.message : 'Save failed')
    }
  }

  return (
    <div className="card">
      <div className="card-body">
        <h3 className="mb-3">Social Links</h3>
        {err && <div className="alert alert-danger">{err}</div>}
        {msg && <div className="alert alert-success">{msg}</div>}
        <form onSubmit={save} className="row g-3">
          {(
            [
              ['fb', 'Facebook'],
              ['twitter', 'Twitter / X'],
              ['instagram', 'Instagram'],
              ['linkedin', 'LinkedIn'],
              ['youtube', 'YouTube'],
              ['pinterest', 'Pinterest'],
            ] as const
          ).map(([k, label]) => (
            <div className="col-md-6" key={k}>
              <label className="form-label">{label}</label>
              <input className="form-control" value={form[k]} onChange={(e) => setForm({ ...form, [k]: e.target.value })} />
            </div>
          ))}
          <div className="col-12 d-flex gap-2">
            <Link className="btn btn-outline-secondary" to={stepPath('/website/payment-details', cardId)}>
              Back
            </Link>
            <button type="submit" className="btn btn-primary">
              Save
            </button>
            <Link className="btn btn-success" to="/dashboard">
              Done — Dashboard
            </Link>
          </div>
        </form>
      </div>
    </div>
  )
}
