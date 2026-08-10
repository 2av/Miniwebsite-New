import { useEffect, useRef, useState } from 'react'
import { apiGet, apiSend, ApiError } from '@/shared/api/client'
import { useAuthSession } from '@/shared/auth/useAuthSession'
import { siteUrl } from '@/lib/utils'

type VerificationData = {
  status: string
  message?: string | null
  canSubmit: boolean
  agreementPaid: boolean
  isLocked: boolean
  panCardUrl?: string | null
  aadhaarFrontUrl?: string | null
  aadhaarBackUrl?: string | null
  adminRemarks?: string | null
  reviewedAtDisplay?: string | null
  reviewedBy?: string | null
}

type DocSlot = 'pan' | 'front' | 'back'

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
    adminRemarks: (pick(o, 'adminRemarks', 'AdminRemarks') as string | null) ?? null,
    reviewedAtDisplay: (pick(o, 'reviewedAtDisplay', 'ReviewedAtDisplay') as string | null) ?? null,
    reviewedBy: (pick(o, 'reviewedBy', 'ReviewedBy') as string | null) ?? null,
  }
}

function fileToBase64(file: File): Promise<string> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader()
    reader.onload = () => resolve(String(reader.result || ''))
    reader.onerror = () => reject(new Error('Failed to read file'))
    reader.readAsDataURL(file)
  })
}

function statusBadge(status: string) {
  const s = status.toLowerCase()
  if (s === 'submitted') {
    return (
      <span className="badge bg-warning text-dark">
        <i className="fa fa-clock" aria-hidden /> Under Review
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
  return (
    <span className="badge bg-secondary">
      <i className="fa fa-hourglass-half" aria-hidden /> Pending
    </span>
  )
}

export function VerificationPage() {
  const session = useAuthSession()
  const [data, setData] = useState<VerificationData | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [msg, setMsg] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)
  const [preview, setPreview] = useState<{ pan?: string; front?: string; back?: string }>({})
  const [pendingB64, setPendingB64] = useState<{ pan?: string; front?: string; back?: string }>({})
  const [lightbox, setLightbox] = useState<string | null>(null)

  const panRef = useRef<HTMLInputElement>(null)
  const frontRef = useRef<HTMLInputElement>(null)
  const backRef = useRef<HTMLInputElement>(null)

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
      })
      setPendingB64({})
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to load')
    }
  }

  useEffect(() => {
    void load()
  }, [session?.email])

  async function onPick(slot: DocSlot, file: File | null) {
    if (!file) return
    if (!file.type.startsWith('image/')) {
      setError('Please choose an image file (JPG, PNG, GIF, WebP).')
      return
    }
    if (file.size > 2_500_000) {
      setError('Image is too large (max ~2.5MB).')
      return
    }
    try {
      const b64 = await fileToBase64(file)
      setPendingB64((p) => ({ ...p, [slot]: b64 }))
      setPreview((p) => ({ ...p, [slot]: b64 }))
      setError(null)
    } catch {
      setError('Failed to read image')
    }
  }

  function clearSlot(slot: DocSlot) {
    setPendingB64((p) => {
      const next = { ...p }
      delete next[slot]
      return next
    })
    setPreview((p) => {
      const next = { ...p }
      // Restore server URL if present
      if (slot === 'pan') next.pan = data?.panCardUrl || undefined
      if (slot === 'front') next.front = data?.aadhaarFrontUrl || undefined
      if (slot === 'back') next.back = data?.aadhaarBackUrl || undefined
      if (!next[slot]) delete next[slot]
      return next
    })
  }

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (!session?.email || !data) return
    const panOk = pendingB64.pan || data.panCardUrl
    const frontOk = pendingB64.front || data.aadhaarFrontUrl
    const backOk = pendingB64.back || data.aadhaarBackUrl
    if (!panOk || !frontOk || !backOk) {
      setError('All documents are mandatory (PAN, Aadhaar Front, Aadhaar Back).')
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
    return (
      <div className="col-md-4 mb-4">
        <div className="upload-container text-center">
          <div
            className="document-placeholder mx-auto"
            role="button"
            tabIndex={0}
            onClick={() => {
              if (src) setLightbox(src)
              else if (!locked) inputRef.current?.click()
            }}
            onKeyDown={(e) => {
              if (e.key === 'Enter' || e.key === ' ') {
                e.preventDefault()
                if (src) setLightbox(src)
                else if (!locked) inputRef.current?.click()
              }
            }}
          >
            {src ? (
              <img src={src} alt={`${title} preview`} />
            ) : (
              <span>{label}</span>
            )}
          </div>
          <p className="addlogo">{title}</p>
          {!locked ? (
            <div className="file-upload-visible">
              <button
                type="button"
                className="choose-btn"
                onClick={() => inputRef.current?.click()}
              >
                Choose File
              </button>
              <input
                ref={inputRef}
                type="file"
                accept=".jpg,.jpeg,.png,.gif,.webp,image/*"
                className="d-none"
                onChange={(e) => void onPick(slot, e.target.files?.[0] ?? null)}
              />
              {src && pendingB64[slot] ? (
                <button
                  type="button"
                  className="btn btn-sm btn-outline-danger mt-2 w-100"
                  onClick={() => clearSlot(slot)}
                >
                  <i className="fa fa-trash" aria-hidden /> Remove
                </button>
              ) : null}
            </div>
          ) : null}
        </div>
      </div>
    )
  }

  return (
    <>
      <div className="main-top mw-page-header mb-4">
        <h1 className="heading mw-page-title">Franchise Verification</h1>
        <nav aria-label="breadcrumb">
          <ol className="breadcrumb mw-breadcrumb">
            <li className="breadcrumb-item">
              <a href="#">Mini Website</a>
            </li>
            <li className="breadcrumb-item active" aria-current="page">
              Verification
            </li>
          </ol>
        </nav>
      </div>

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

            <h4 className="mb-0 heading" style={{ fontSize: 22 }}>
              Document Verification
            </h4>
            <p className="text-muted mb-4 page_title_description">
              Upload your PAN Card and Aadhaar Card (Front &amp; Back) for verification.{' '}
              <strong className="text-danger">All documents are mandatory.</strong>
            </p>

            <form id="verificationForm" onSubmit={onSubmit}>
              <div className="row">
                <DocCard slot="pan" label="PAN CARD" title="Add PAN Card" inputRef={panRef} />
                <DocCard
                  slot="front"
                  label="AADHAAR FRONT"
                  title="Add Aadhaar Front"
                  inputRef={frontRef}
                />
                <DocCard
                  slot="back"
                  label="AADHAAR BACK"
                  title="Add Aadhaar Back"
                  inputRef={backRef}
                />
              </div>

              {!locked ? (
                <div className="text-center">
                  <button
                    type="submit"
                    name="submit_documents"
                    className="btn btn-warning btn-lg px-5 fw-bold"
                    disabled={busy || !data.agreementPaid}
                  >
                    {busy ? 'Submitting…' : 'SUBMIT'}
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
