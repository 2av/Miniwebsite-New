import { useEffect, useRef, useState } from 'react'
import { apiGet, apiSend, apiUploadForm, ApiError } from '@/shared/api/client'
import { useAuthSession } from '@/shared/auth/useAuthSession'
import { siteUrl } from '@/lib/utils'
import { PortalPageHeader } from '@/components/layout/PortalPageHeader'

type VerificationData = {
  status: string
  message?: string | null
  canSubmit: boolean
  agreementPaid: boolean
  isLocked: boolean
  panCardUrl?: string | null
  aadhaarFrontUrl?: string | null
  aadhaarBackUrl?: string | null
  gstCertificateUrl?: string | null
  agreementUrl?: string | null
  gstNumber?: string | null
  bankName?: string | null
  accountHolder?: string | null
  accountNumber?: string | null
  ifscCode?: string | null
  upiId?: string | null
  upiName?: string | null
  electronicAcceptance: boolean
  adminRemarks?: string | null
  reviewedAtDisplay?: string | null
  reviewedBy?: string | null
}

type DocSlot = 'pan' | 'front' | 'back' | 'gst' | 'agreement'
type DocPreview = Partial<Record<DocSlot, string>>

type BankForm = {
  bankName: string
  accountHolder: string
  accountNumber: string
  ifscCode: string
  upiId: string
  upiName: string
}

function pick(o: Record<string, unknown>, camel: string, pascal: string) {
  return o[camel] ?? o[pascal]
}

function normalize(raw: unknown): VerificationData {
  const o = (raw ?? {}) as Record<string, unknown>
  return {
    status: String(pick(o, 'status', 'Status') ?? 'pending'),
    message: (pick(o, 'message', 'Message') as string | null) ?? null,
    canSubmit: Boolean(pick(o, 'canSubmit', 'CanSubmit')),
    agreementPaid: Boolean(pick(o, 'agreementPaid', 'AgreementPaid') ?? true),
    isLocked: Boolean(pick(o, 'isLocked', 'IsLocked')),
    panCardUrl: (pick(o, 'panCardUrl', 'PanCardUrl') as string | null) ?? null,
    aadhaarFrontUrl: (pick(o, 'aadhaarFrontUrl', 'AadhaarFrontUrl') as string | null) ?? null,
    aadhaarBackUrl: (pick(o, 'aadhaarBackUrl', 'AadhaarBackUrl') as string | null) ?? null,
    gstCertificateUrl: (pick(o, 'gstCertificateUrl', 'GstCertificateUrl') as string | null) ?? null,
    agreementUrl: (pick(o, 'agreementUrl', 'AgreementUrl') as string | null) ?? null,
    gstNumber: (pick(o, 'gstNumber', 'GstNumber') as string | null) ?? null,
    bankName: (pick(o, 'bankName', 'BankName') as string | null) ?? null,
    accountHolder: (pick(o, 'accountHolder', 'AccountHolder') as string | null) ?? null,
    accountNumber: (pick(o, 'accountNumber', 'AccountNumber') as string | null) ?? null,
    ifscCode: (pick(o, 'ifscCode', 'IfscCode') as string | null) ?? null,
    upiId: (pick(o, 'upiId', 'UpiId') as string | null) ?? null,
    upiName: (pick(o, 'upiName', 'UpiName') as string | null) ?? null,
    electronicAcceptance: Boolean(pick(o, 'electronicAcceptance', 'ElectronicAcceptance')),
    adminRemarks: (pick(o, 'adminRemarks', 'AdminRemarks') as string | null) ?? null,
    reviewedAtDisplay: (pick(o, 'reviewedAtDisplay', 'ReviewedAtDisplay') as string | null) ?? null,
    reviewedBy: (pick(o, 'reviewedBy', 'ReviewedBy') as string | null) ?? null,
  }
}

function statusBadge(status: string) {
  const s = status.toLowerCase()
  if (s === 'submitted' || s === 'pending review') {
    return (
      <span className="badge bg-warning text-dark">
        <i className="fa fa-clock" aria-hidden /> Pending Review
      </span>
    )
  }
  if (s === 'approved' || s === 'verified') {
    return (
      <span className="badge bg-success">
        <i className="fa fa-check-circle" aria-hidden /> Approved
      </span>
    )
  }
  if (s === 'rejected') {
    return (
      <span className="badge bg-danger">
        <i className="fa fa-times-circle" aria-hidden /> Rejected
      </span>
    )
  }
  if (s === 'resubmission' || s === 'resubmission required') {
    return (
      <span className="badge bg-warning text-dark">
        <i className="fa fa-refresh" aria-hidden /> Resubmission Required
      </span>
    )
  }
  return (
    <span className="badge bg-secondary">
      <i className="fa fa-hourglass-half" aria-hidden /> Not Submitted
    </span>
  )
}

export function VerificationPage() {
  const session = useAuthSession()
  const [data, setData] = useState<VerificationData | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [msg, setMsg] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)
  const [uploadingSlot, setUploadingSlot] = useState<DocSlot | null>(null)
  const [uploadPercent, setUploadPercent] = useState(0)
  const [removingSlot, setRemovingSlot] = useState<DocSlot | null>(null)
  const [slotError, setSlotError] = useState<Partial<Record<DocSlot, string>>>({})
  const [preview, setPreview] = useState<DocPreview>({})
  const [pendingB64, setPendingB64] = useState<DocPreview>({})
  const [lightbox, setLightbox] = useState<string | null>(null)
  const [gstNumber, setGstNumber] = useState('')
  const [accepted, setAccepted] = useState(false)
  const [bank, setBank] = useState<BankForm>({
    bankName: '',
    accountHolder: '',
    accountNumber: '',
    ifscCode: '',
    upiId: '',
    upiName: '',
  })

  const panRef = useRef<HTMLInputElement>(null)
  const frontRef = useRef<HTMLInputElement>(null)
  const backRef = useRef<HTMLInputElement>(null)
  const gstRef = useRef<HTMLInputElement>(null)
  const agreementRef = useRef<HTMLInputElement>(null)

  async function load() {
    if (!session?.email) return
    try {
      setError(null)
      const raw = await apiGet<unknown>('/api/v1/portal/verification', { email: session.email })
      const page = normalize(raw)
      setData(page)
      setPreview({
        pan: page.panCardUrl || undefined,
        front: page.aadhaarFrontUrl || undefined,
        back: page.aadhaarBackUrl || undefined,
        gst: page.gstCertificateUrl || undefined,
        agreement: page.agreementUrl || undefined,
      })
      setGstNumber(page.gstNumber || '')
      setAccepted(page.electronicAcceptance)
      setBank({
        bankName: page.bankName || '',
        accountHolder: page.accountHolder || '',
        accountNumber: page.accountNumber || '',
        ifscCode: page.ifscCode || '',
        upiId: page.upiId || '',
        upiName: page.upiName || '',
      })
      setPendingB64({})
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to load')
    }
  }

  useEffect(() => {
    void load()
  }, [session?.email])

  function showSlotError(slot: DocSlot, message: string | null) {
    setSlotError((prev) => {
      const next = { ...prev }
      if (message) next[slot] = message
      else delete next[slot]
      return next
    })
  }

  function applyDocuments(page: VerificationData) {
    setData(page)
    setPreview({
      pan: page.panCardUrl || undefined,
      front: page.aadhaarFrontUrl || undefined,
      back: page.aadhaarBackUrl || undefined,
      gst: page.gstCertificateUrl || undefined,
      agreement: page.agreementUrl || undefined,
    })
  }

  async function onPick(slot: DocSlot, file: File | null) {
    if (!file || !session?.email) return
    const allowed = ['image/jpeg', 'image/jpg', 'image/png', 'image/gif', 'image/webp']
    const extOk = /\.(jpe?g|png|gif|webp)$/i.test(file.name)
    if (!allowed.includes(file.type) && !(file.type === '' && extOk) && !file.type.startsWith('image/')) {
      showSlotError(slot, 'Use a JPG, PNG, GIF, or WebP image.')
      return
    }
    if (file.size > 2_500_000) {
      showSlotError(slot, 'Image is too large (max 2.5MB).')
      return
    }
    showSlotError(slot, null)
    setUploadingSlot(slot)
    setUploadPercent(0)
    setMsg(null)
    try {
      const form = new FormData()
      form.append('email', session.email)
      form.append('kind', slot)
      form.append('file', file)
      const res = await apiUploadForm<unknown>('/api/v1/portal/verification-document', form, setUploadPercent)
      setUploadPercent(100)
      applyDocuments(normalize(res.data))
      setMsg('Document uploaded.')
    } catch (e) {
      showSlotError(slot, e instanceof ApiError ? e.message : 'Upload failed')
    } finally {
      setUploadingSlot(null)
    }
  }

  async function removeSlot(slot: DocSlot) {
    if (!session?.email || removingSlot) return
    setRemovingSlot(slot)
    showSlotError(slot, null)
    try {
      const res = await apiSend<unknown>(
        'delete',
        `/api/v1/portal/verification-document?email=${encodeURIComponent(session.email)}&kind=${encodeURIComponent(slot)}`,
      )
      applyDocuments(normalize(res.data))
      setPendingB64((p) => {
        const next = { ...p }
        delete next[slot]
        return next
      })
    } catch (e) {
      showSlotError(slot, e instanceof ApiError ? e.message : 'Could not remove the image.')
    } finally {
      setRemovingSlot(null)
    }
  }

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (!session?.email || !data) return
    const panOk = pendingB64.pan || data.panCardUrl
    const frontOk = pendingB64.front || data.aadhaarFrontUrl
    const backOk = pendingB64.back || data.aadhaarBackUrl
    const gstOk = pendingB64.gst || data.gstCertificateUrl
    const agreementOk = pendingB64.agreement || data.agreementUrl
    if (!panOk || !frontOk || !backOk || !gstOk || !agreementOk) {
      setError('PAN, Aadhaar (front and back), GST certificate, and agreement are mandatory.')
      return
    }
    if (!/^[0-9A-Za-z]{15}$/.test(gstNumber.trim())) {
      setError('Enter a valid 15-character GSTIN.')
      return
    }
    if (!bank.bankName.trim() || !bank.accountHolder.trim() || !bank.accountNumber.trim() || !bank.ifscCode.trim() || !bank.upiId.trim() || !bank.upiName.trim()) {
      setError('Bank name, account holder, account number, IFSC, UPI ID, and UPI name are required.')
      return
    }
    if (!/^\d+$/.test(bank.accountNumber.trim())) {
      setError('Account number should contain only numbers.')
      return
    }
    if (bank.ifscCode.trim().length !== 11) {
      setError('IFSC code should be exactly 11 characters.')
      return
    }
    if (!bank.upiId.includes('@')) {
      setError('Please enter a valid UPI ID (e.g., user@paytm).')
      return
    }
    if (!accepted) {
      setError('Electronic acceptance is required.')
      return
    }
    setBusy(true)
    setMsg(null)
    setError(null)
    try {
      const res = await apiSend<VerificationData>('post', '/api/v1/portal/verification', {
        email: session.email,
        panCardBase64: pendingB64.pan || null,
        aadhaarFrontBase64: pendingB64.front || null,
        aadhaarBackBase64: pendingB64.back || null,
        gstCertificateBase64: pendingB64.gst || null,
        agreementBase64: pendingB64.agreement || null,
        gstNumber: gstNumber.trim().toUpperCase(),
        bankName: bank.bankName.trim(),
        accountHolder: bank.accountHolder.trim(),
        accountNumber: bank.accountNumber.trim(),
        ifscCode: bank.ifscCode.trim().toUpperCase(),
        upiId: bank.upiId.trim(),
        upiName: bank.upiName.trim(),
        electronicAcceptance: true,
      })
      setMsg(res.message || 'Documents submitted successfully.')
      await load()
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Submit failed')
    } finally {
      setBusy(false)
    }
  }

  const locked = Boolean(data?.isLocked)
  const showStatusCard = data && data.status !== 'pending'

  function DocCard({
    slot,
    label,
    title,
    inputRef,
  }: {
    slot: DocSlot
    label: string
    title: string
    inputRef: React.RefObject<HTMLInputElement | null>
  }) {
    const src = preview[slot]
    const note = slotError[slot]
    const busySlot = uploadingSlot === slot || removingSlot === slot
    return (
      <div className="col-md-4 mb-4">
        <div className="upload-container text-center">
          <div
            className="document-placeholder mx-auto"
            role="button"
            tabIndex={0}
            onClick={() => {
              if (busySlot) return
              if (src) setLightbox(src)
              else if (!locked) inputRef.current?.click()
            }}
            onKeyDown={(e) => {
              if (e.key === 'Enter' || e.key === ' ') {
                e.preventDefault()
                if (busySlot) return
                if (src) setLightbox(src)
                else if (!locked) inputRef.current?.click()
              }
            }}
          >
            {uploadingSlot === slot ? (
              <div className="doc-upload-progress">
                <span>Uploading {uploadPercent}%</span>
                <div className="doc-upload-track" aria-hidden>
                  <div className="doc-upload-fill" style={{ width: `${uploadPercent}%` }} />
                </div>
              </div>
            ) : src ? (
              <img src={src} alt={`${title} preview`} />
            ) : note ? (
              <p className="doc-box-note">{note}</p>
            ) : (
              <span>{label}</span>
            )}
            {src && note && uploadingSlot !== slot ? <p className="doc-box-note doc-box-note-overlay">{note}</p> : null}
            {src && !locked && !busySlot ? (
              <button
                type="button"
                className="doc-remove"
                aria-label={`Remove ${title}`}
                onClick={(e) => {
                  e.stopPropagation()
                  void removeSlot(slot)
                }}
              >
                <i className="fa fa-times" aria-hidden />
              </button>
            ) : null}
          </div>
          <p className="addlogo">{title}</p>
          {!locked ? (
            <div className="file-upload-visible">
              <label className={`choose-btn${busySlot ? ' disabled' : ''}`}>
                Choose File
                <input
                  ref={inputRef}
                  type="file"
                  accept=".jpg,.jpeg,.png,.gif,.webp,image/*"
                  className="verification-file-input"
                  disabled={busySlot}
                  onChange={(e) => {
                    const picked = e.target.files?.[0] ?? null
                    e.target.value = ''
                    void onPick(slot, picked)
                  }}
                />
              </label>
            </div>
          ) : null}
        </div>
      </div>
    )
  }

  return (
    <>
      <PortalPageHeader
        title="Document Verification"
        subtitle="Submit PAN, Aadhaar, GST details, agreement, bank and UPI details, and electronic acceptance."
      />

      {!data && !error ? <p className="mw-helper-text">Loading…</p> : null}
      {error ? (
        <div className="mw-alert mw-alert-danger mb-3" role="alert">
          <i className="fa fa-exclamation-circle mw-alert-icon" aria-hidden />
          <div className="mw-alert-body">{error}</div>
        </div>
      ) : null}
      {msg ? (
        <div className="mw-alert mw-alert-success mb-3" role="alert">
          <i className="fa fa-check-circle mw-alert-icon" aria-hidden />
          <div className="mw-alert-body">{msg}</div>
        </div>
      ) : null}

      {data ? (
        <div className="mw-card mb-4">
          <div className="mw-card-body">
            {!data.agreementPaid ? (
              <div className="alert alert-warning">
                Complete franchise agreement payment first.{' '}
                <a href={siteUrl('franchise_agreement.php')}>Continue to payment</a>
              </div>
            ) : null}

            <h2 className="heading mw-section-title mb-1">KYC documents</h2>
            <p className="text-muted mb-4 page_title_description">
              <strong className="text-danger">All items are mandatory.</strong>
            </p>

            <form id="verificationForm" onSubmit={onSubmit}>
              <div className="row">
                <DocCard slot="pan" label="PAN CARD" title="PAN" inputRef={panRef} />
                <DocCard slot="front" label="AADHAAR FRONT" title="Aadhaar front" inputRef={frontRef} />
                <DocCard slot="back" label="AADHAAR BACK" title="Aadhaar back" inputRef={backRef} />
                <DocCard slot="gst" label="GST CERTIFICATE" title="GST certificate" inputRef={gstRef} />
                <DocCard slot="agreement" label="AGREEMENT" title="Agreement" inputRef={agreementRef} />
              </div>

              <section className="mw-bank-details-section mb-4 pt-2">
                <h2 className="heading mw-section-title">GST details</h2>
                <div className="mw-form-grid-2">
                  <div className="mw-form-group">
                    <label className="mw-label" htmlFor="gstNumber">
                      GSTIN <span className="req">*</span>
                    </label>
                    <input
                      id="gstNumber"
                      className="form-control mw-input"
                      value={gstNumber}
                      maxLength={15}
                      placeholder="15-character GSTIN"
                      readOnly={locked}
                      onChange={(e) => setGstNumber(e.target.value.toUpperCase())}
                    />
                  </div>
                </div>
              </section>

              <section className="mw-bank-details-section mb-4 pt-2 border-t border-border">
                <h2 className="heading mw-section-title">Bank and UPI details</h2>
                <div className="mw-form-grid-2">
                  {(
                    [
                      ['bankName', 'Bank name', 'Enter bank name'],
                      ['accountHolder', 'Account holder name', 'Account holder name'],
                      ['accountNumber', 'Account number', 'Enter account number'],
                      ['ifscCode', 'IFSC code', '11-character IFSC'],
                      ['upiId', 'UPI ID', 'name@bank'],
                      ['upiName', 'UPI name', 'Name on UPI'],
                    ] as const
                  ).map(([key, label, placeholder]) => (
                    <div className="mw-form-group" key={key}>
                      <label className="mw-label" htmlFor={key}>
                        {label} <span className="req">*</span>
                      </label>
                      <input
                        id={key}
                        className="form-control mw-input"
                        placeholder={placeholder}
                        value={bank[key]}
                        readOnly={locked}
                        onChange={(e) => setBank((current) => ({ ...current, [key]: e.target.value }))}
                      />
                    </div>
                  ))}
                </div>
              </section>

              <section className="mb-4 pt-2 border-t border-border">
                <h2 className="heading mw-section-title">Electronic acceptance</h2>
                <label className="flex items-start gap-3 text-sm text-sb-ink">
                  <input
                    type="checkbox"
                    className="mt-1"
                    checked={accepted}
                    disabled={locked}
                    onChange={(e) => setAccepted(e.target.checked)}
                  />
                  <span>
                    I confirm that the PAN, Aadhaar, GST details, agreement, and bank/UPI details submitted here are
                    true, and I electronically accept them for ShareBizz verification.
                  </span>
                </label>
              </section>

              {!locked ? (
                <div className="text-center">
                  <button
                    type="submit"
                    name="submit_documents"
                    className="sb-btn-primary px-6"
                    disabled={busy || !data.agreementPaid}
                  >
                    {busy ? 'Submitting…' : 'Submit all KYC documents'}
                  </button>
                </div>
              ) : null}
            </form>

            {showStatusCard ? (
              <div className="mt-4">
                <div className="card">
                  <div className="card-header">
                    <h4 className="mb-0 verification_title" style={{ fontSize: 18 }}>
                      <i className="fa fa-info-circle" aria-hidden /> Verification Status
                    </h4>
                  </div>
                  <div className="card-body">
                    <div className="row">
                      <div className="col-md-6">
                        <div className="mb-3">
                          <strong>Status:</strong> {statusBadge(data.status)}
                        </div>
                        {data.reviewedAtDisplay ? (
                          <div className="mb-3">
                            <strong>Reviewed On:</strong>{' '}
                            <span className="text-muted">{data.reviewedAtDisplay}</span>
                          </div>
                        ) : null}
                        {data.reviewedBy ? (
                          <div className="mb-3">
                            <strong>Reviewed By:</strong>{' '}
                            <span className="text-muted">{data.reviewedBy}</span>
                          </div>
                        ) : null}
                      </div>
                      <div className="col-md-6">
                        {data.adminRemarks ? (
                          <div className="mb-3">
                            <strong>Admin Remarks:</strong>
                            <div className="mt-2 rounded border bg-light p-3">
                              <i className="fa fa-comment" aria-hidden /> {data.adminRemarks}
                            </div>
                          </div>
                        ) : null}
                      </div>
                    </div>

                    {data.status === 'submitted' ? (
                      <div className="mt-3 rounded border border-warning bg-warning p-3 text-dark">
                        <i className="fa fa-clock" aria-hidden />{' '}
                        <strong>
                          Thanks for sharing the Documents. Our team will verify it and confirm within 48 hrs.
                        </strong>
                      </div>
                    ) : null}
                    {data.status === 'approved' || data.status === 'verified' ? (
                      <div className="mt-3 rounded border border-success bg-success p-3 text-white">
                        <i className="fa fa-check-circle" aria-hidden />{' '}
                        <strong>Congratulations! Your documents have been approved successfully.</strong>
                      </div>
                    ) : null}
                    {data.status === 'rejected' ? (
                      <div className="mt-3 rounded border border-danger bg-danger p-3 text-white">
                        <i className="fa fa-exclamation-triangle" aria-hidden />{' '}
                        <strong>Your documents have been rejected. Please upload new documents.</strong>
                      </div>
                    ) : null}
                  </div>
                </div>
              </div>
            ) : null}
          </div>
        </div>
      ) : null}

      {lightbox ? (
        <div className="mw-modal is-open" role="dialog" aria-modal onClick={() => setLightbox(null)}>
          <div className="mw-modal-backdrop" />
          <div className="mw-modal-panel mw-modal-lg" onClick={(e) => e.stopPropagation()}>
            <div className="mw-modal-header">
              <h2 className="mw-modal-title">Document Preview</h2>
              <button type="button" className="mw-modal-close" onClick={() => setLightbox(null)}>
                ×
              </button>
            </div>
            <div className="mw-modal-body text-center">
              <img src={lightbox} alt="Document" className="img-fluid" style={{ maxHeight: '70vh' }} />
            </div>
          </div>
        </div>
      ) : null}

      <style>{`
        .document-placeholder {
          position: relative;
          width: 200px;
          height: 200px;
          border: 2px solid darkgray;
          overflow: hidden;
          display: flex;
          align-items: center;
          justify-content: center;
          cursor: pointer;
          margin: 0 auto;
          background: #fff;
        }
        .document-placeholder span {
          font-weight: 500;
          font-size: 18px;
          line-height: 33px;
          text-align: center;
          color: #666;
        }
        .doc-box-note {
          margin: 0;
          padding: 0 10px;
          font-size: 11px;
          font-weight: 500;
          line-height: 1.35;
          text-align: center;
          color: #b42318;
        }
        .doc-box-note-overlay {
          position: absolute;
          left: 6px;
          right: 6px;
          bottom: 6px;
          padding: 4px 6px;
          background: rgba(255, 255, 255, 0.94);
          border-radius: 4px;
        }
        .doc-remove {
          position: absolute;
          top: 6px;
          right: 6px;
          z-index: 2;
          width: 22px;
          height: 22px;
          padding: 0;
          border: 0;
          border-radius: 50%;
          background: rgba(15, 23, 42, 0.72);
          color: #fff;
          display: flex;
          align-items: center;
          justify-content: center;
          cursor: pointer;
          font-size: 12px;
          line-height: 1;
        }
        .doc-remove:hover { background: #dc3545; }
        .doc-upload-progress {
          display: flex;
          flex-direction: column;
          align-items: center;
          justify-content: center;
          gap: 6px;
          width: 100%;
          padding: 12px;
          color: #334155;
        }
        .doc-upload-progress span {
          font-size: 12px;
          font-weight: 600;
          line-height: 1.3;
          letter-spacing: 0;
        }
        .doc-upload-track {
          width: 80%;
          height: 4px;
          border-radius: 999px;
          background: #e2e8f0;
          overflow: hidden;
        }
        .doc-upload-fill {
          height: 100%;
          border-radius: 999px;
          background: #007bff;
          transition: width 0.15s ease;
        }
        .document-placeholder img {
          max-width: 100%;
          max-height: 100%;
          width: auto;
          height: auto;
          object-fit: contain;
          border-radius: 8px;
        }
        .upload-container { background: white; padding: 20px; }
        .addlogo {
          font-size: 18px !important;
          margin-bottom: 10px;
          margin-top: 15px;
          text-align: center;
        }
        .choose-btn {
          display: inline-block;
          padding: 10px 20px;
          background-color: #007bff;
          color: white;
          border: 0;
          border-radius: 5px;
          cursor: pointer;
          font-size: 14px;
          font-weight: 500;
        }
        .choose-btn:hover { background-color: #0056b3; color: white; }
        .choose-btn.disabled { opacity: 0.7; pointer-events: none; }
        .verification-file-input {
          position: absolute;
          width: 1px;
          height: 1px;
          padding: 0;
          margin: -1px;
          overflow: hidden;
          clip: rect(0, 0, 0, 0);
          white-space: nowrap;
          border: 0;
        }
        .btn-warning {
          background-color: #ffc107;
          border-color: #ffc107;
          color: #212529;
          font-weight: bold;
        }
        .btn-warning:hover {
          background-color: #e0a800;
          border-color: #d39e00;
          color: #212529;
        }
      `}</style>
    </>
  )
}
