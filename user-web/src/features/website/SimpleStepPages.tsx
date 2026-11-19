import { useEffect, useState } from 'react'
import { useOutletContext } from 'react-router-dom'
import { ApiError } from '@/shared/api/client'
import { stepPath } from '@/features/website/cardContext'
import { normalizeSocialLink, SOCIAL_LINK_ERROR, SOCIAL_LINK_PLACEHOLDER, socialLinkError, UPI_ERROR, UPI_PLACEHOLDER, upiError, VIDEO_LINK_ERROR, VIDEO_LINK_LIMIT, VIDEO_LINK_PLACEHOLDER, videoLinkError } from '@/features/website/profileFieldRules'
import { pickVal, websiteApi } from '@/features/website/websiteApi'
import type { WebsiteOutletContext } from '@/features/website/WebsiteBuilderLayout'
import { PortalPageHeader } from '@/components/layout/PortalPageHeader'
import { MwAlert, MwField, MwInput, MwStepActions } from '@/components/form/MwControls'

export function VideosPage() {
  const { cardId, email } = useOutletContext<WebsiteOutletContext>()
  const [urls, setUrls] = useState<string[]>(() => Array(VIDEO_LINK_LIMIT).fill(''))
  const [slotErrors, setSlotErrors] = useState<(string | null)[]>(() => Array(VIDEO_LINK_LIMIT).fill(null))
  const [msg, setMsg] = useState<string | null>(null)
  const [err, setErr] = useState<string | null>(null)

  useEffect(() => {
    if (!cardId) return
    ;(async () => {
      try {
        const raw = await websiteApi.getVideos(cardId, email)
        const list = ((raw.urls ?? raw.Urls) as string[]) || []
        const next = Array(VIDEO_LINK_LIMIT).fill('')
        list.slice(0, VIDEO_LINK_LIMIT).forEach((u, i) => {
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
    const nextErrors = urls.map((url) => videoLinkError(url))
    setSlotErrors(nextErrors)
    if (nextErrors.some(Boolean)) {
      setErr(VIDEO_LINK_ERROR)
      setMsg(null)
      return
    }
    try {
      setErr(null)
      const res = await websiteApi.putVideos(cardId, email, urls.map((u) => u.trim() || null))
      setMsg(res.message || 'Saved')
    } catch (ex) {
      setErr(ex instanceof ApiError ? ex.message : 'Save failed')
    }
  }

  return (
    <>
      <PortalPageHeader title="Videos" />
      <div className="card mb-4 mw-card">
      <div className="card-body mw-card-body">
        {err ? <MwAlert tone="danger">{err}</MwAlert> : null}
        {msg ? <MwAlert tone="success">{msg}</MwAlert> : null}
        <form onSubmit={save} className="mw-form">
          <div className="mw-form-grid-2">
            {urls.map((u, i) => (
              <MwField label={`Video ${i + 1}`} key={i} htmlFor={`video-${i}`}>
                <MwInput
                  id={`video-${i}`}
                  placeholder={VIDEO_LINK_PLACEHOLDER}
                  value={u}
                  onChange={(e) => {
                    const value = e.target.value
                    setUrls((arr) => arr.map((x, idx) => (idx === i ? value : x)))
                    setSlotErrors((current) =>
                      current.map((item, index) => (index === i ? (item && value.trim() ? videoLinkError(value) : null) : item)),
                    )
                  }}
                />
                {slotErrors[i] ? <span style={{ display: 'block', color: '#b42318', fontWeight: 500, marginTop: 4 }}>{slotErrors[i]}</span> : null}
              </MwField>
            ))}
          </div>
          <MwStepActions
            back={stepPath('/website/products', cardId)}
            next={stepPath('/website/image-gallery', cardId)}
            saveLabel="Save"
          />
        </form>
      </div>
    </div>
    </>
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
    const format = upiError(form.googlePay) || upiError(form.phonePay) || upiError(form.paytm)
    if (format) {
      setErr(format)
      setMsg(null)
      return
    }
    try {
      setErr(null)
      const res = await websiteApi.putPayment(cardId, email, form)
      setMsg(res.message || 'Saved')
    } catch (ex) {
      setErr(ex instanceof ApiError ? ex.message : 'Save failed')
    }
  }

  return (
    <>
      <PortalPageHeader title="Payment Details" />
      <div className="card mb-4 mw-card">
      <div className="card-body mw-card-body">
        {err ? <MwAlert tone="danger">{err}</MwAlert> : null}
        {msg ? <MwAlert tone="success">{msg}</MwAlert> : null}
        <form onSubmit={save} className="mw-form">
          <div className="mw-form-grid-2">
            {(
              [
                ['googlePay', 'Google Pay'],
                ['phonePay', 'PhonePe'],
                ['paytm', 'Paytm'],
                ['bankName', 'Bank Name'],
                ['acName', 'Account Name'],
                ['accountNo', 'Account Number'],
                ['ifsc', 'IFSC'],
                ['acType', 'Account Type'],
              ] as const
            ).map(([k, label]) => (
              <MwField label={label} key={k} htmlFor={`pay-${k}`}>
                <MwInput
                  id={`pay-${k}`}
                  placeholder={k === 'googlePay' || k === 'phonePay' || k === 'paytm' ? UPI_PLACEHOLDER : undefined}
                  value={form[k]}
                  onChange={(e) => setForm({ ...form, [k]: e.target.value })}
                />
                {k === 'googlePay' || k === 'phonePay' || k === 'paytm' ? (
                  upiError(form[k]) && form[k].trim() ? <span style={{ display: 'block', color: '#b42318', fontWeight: 500, marginTop: 4 }}>{UPI_ERROR}</span> : null
                ) : null}
              </MwField>
            ))}
          </div>
          <MwStepActions
            back={stepPath('/website/image-gallery', cardId)}
            next={stepPath('/website/social-links', cardId)}
            saveLabel="Save"
          />
        </form>
      </div>
    </div>
    </>
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
    google: '',
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
          google: String(pickVal(raw, 'google', 'Google') ?? ''),
        })
      } catch (e) {
        setErr(e instanceof Error ? e.message : 'Failed to load')
      }
    })()
  }, [cardId, email])

  async function save(e: React.FormEvent) {
    e.preventDefault()
    if (!cardId) return
    const keys = ['fb', 'twitter', 'instagram', 'linkedin', 'youtube', 'pinterest', 'google'] as const
    if (keys.some((key) => socialLinkError(form[key]))) {
      setErr(SOCIAL_LINK_ERROR)
      setMsg(null)
      return
    }
    try {
      setErr(null)
      const next = { ...form }
      for (const key of keys) next[key] = normalizeSocialLink(form[key])
      const res = await websiteApi.putSocial(cardId, email, next)
      setMsg(res.message || 'Saved')
    } catch (ex) {
      setErr(ex instanceof ApiError ? ex.message : 'Save failed')
    }
  }

  return (
    <>
      <PortalPageHeader title="Social Links" />
      <div className="card mb-4 mw-card">
      <div className="card-body mw-card-body">
        {err ? <MwAlert tone="danger">{err}</MwAlert> : null}
        {msg ? <MwAlert tone="success">{msg}</MwAlert> : null}
        <form onSubmit={save} className="mw-form">
          <div className="mw-form-grid-2">
            {(
              [
                ['fb', 'Facebook'],
                ['twitter', 'Twitter / X'],
                ['instagram', 'Instagram'],
                ['linkedin', 'LinkedIn'],
                ['youtube', 'YouTube'],
                ['pinterest', 'Pinterest'],
                ['google', 'Google'],
              ] as const
            ).map(([k, label]) => (
              <MwField label={label} key={k} htmlFor={`social-${k}`}>
                <MwInput
                  id={`social-${k}`}
                  placeholder={SOCIAL_LINK_PLACEHOLDER}
                  value={form[k]}
                  onChange={(e) => setForm({ ...form, [k]: e.target.value })}
                />
                {socialLinkError(form[k]) && form[k].trim() ? (
                  <span style={{ display: 'block', color: '#b42318', fontWeight: 500, marginTop: 4 }}>{SOCIAL_LINK_ERROR}</span>
                ) : null}
              </MwField>
            ))}
          </div>
          <MwStepActions back={stepPath('/website/payment-details', cardId)} next="/dashboard" nextLabel="Dashboard" saveLabel="Save" />
        </form>
      </div>
    </div>
    </>
  )
}
