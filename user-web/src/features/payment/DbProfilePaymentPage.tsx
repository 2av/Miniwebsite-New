import { useEffect, useMemo, useState, type ReactNode } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { apiGet, apiSend, ApiError } from '@/shared/api/client'
import { getAuthSession, LOGIN_PATH } from '@/shared/auth/session'

type Billing = {
  name: string
  email: string
  phone: string
  address: string
  state: string
  city: string
  pincode: string
  gstNumber: string
}

type Plan = {
  id: number
  planName: string
  validityMonths?: number | null
  validityLabel: string
  originalPrice: number
  offeredPrice: number
  originalPriceGst: number
  offeredPriceGst: number
  saveAmount: number
  perMonthApprox?: number | null
  badge?: string | null
  lockLabel?: string | null
}

type PageData = {
  cardId: number
  companyName?: string | null
  alreadyPaid: boolean
  sellerState: string
  billing: Billing
  plans: Plan[]
}

type Quote = {
  original: number
  discount: number
  subTotal: number
  cgst: number
  sgst: number
  igst: number
  total: number
}

type RazorpayCtor = new (options: Record<string, unknown>) => { open: () => void }

const fieldClass =
  'h-11 w-full rounded-lg border border-[#D9DEE7] bg-white px-3 text-[15px] text-[#1F2937] outline-none placeholder:text-[#9CA3AF] focus:border-[#1E3A8A] focus:ring-2 focus:ring-[#1E3A8A]/15'

function pick<T>(obj: Record<string, unknown>, camel: string, pascal: string): T {
  return (obj[camel] ?? obj[pascal]) as T
}

function num(value: unknown) {
  const n = Number(value ?? 0)
  return Number.isFinite(n) ? n : 0
}

function normalizePlan(raw: unknown): Plan {
  const r = (raw ?? {}) as Record<string, unknown>
  return {
    id: num(pick(r, 'id', 'Id')),
    planName: String(pick(r, 'planName', 'PlanName') ?? ''),
    validityMonths: pick(r, 'validityMonths', 'ValidityMonths') as number | null,
    validityLabel: String(pick(r, 'validityLabel', 'ValidityLabel') ?? ''),
    originalPrice: num(pick(r, 'originalPrice', 'OriginalPrice')),
    offeredPrice: num(pick(r, 'offeredPrice', 'OfferedPrice')),
    originalPriceGst: num(pick(r, 'originalPriceGst', 'OriginalPriceGst')),
    offeredPriceGst: num(pick(r, 'offeredPriceGst', 'OfferedPriceGst')),
    saveAmount: num(pick(r, 'saveAmount', 'SaveAmount')),
    perMonthApprox: pick(r, 'perMonthApprox', 'PerMonthApprox') as number | null,
    badge: (pick(r, 'badge', 'Badge') as string | null) || null,
    lockLabel: (pick(r, 'lockLabel', 'LockLabel') as string | null) || null,
  }
}

function normalizePage(raw: unknown): PageData {
  const o = (raw ?? {}) as Record<string, unknown>
  const billing = (pick(o, 'billing', 'Billing') ?? {}) as Record<string, unknown>
  const plans = ((pick(o, 'plans', 'Plans') as unknown[]) || []).map(normalizePlan)
  return {
    cardId: num(pick(o, 'cardId', 'CardId')),
    companyName: pick(o, 'companyName', 'CompanyName') as string | null,
    alreadyPaid: Boolean(pick(o, 'alreadyPaid', 'AlreadyPaid')),
    sellerState: String(pick(o, 'sellerState', 'SellerState') ?? 'Haryana'),
    billing: {
      name: String(pick(billing, 'name', 'Name') ?? ''),
      email: String(pick(billing, 'email', 'Email') ?? ''),
      phone: String(pick(billing, 'phone', 'Phone') ?? ''),
      address: String(pick(billing, 'address', 'Address') ?? ''),
      state: String(pick(billing, 'state', 'State') ?? ''),
      city: String(pick(billing, 'city', 'City') ?? ''),
      pincode: String(pick(billing, 'pincode', 'Pincode') ?? ''),
      gstNumber: String(pick(billing, 'gstNumber', 'GstNumber') ?? ''),
    },
    plans,
  }
}

function rupees(value: number) {
  return Math.round(value * 100) / 100
}

function percent(amount: number, rate: number) {
  return Math.round(amount * rate) / 100
}

function inr(value: number, digits = 2) {
  return `₹${value.toLocaleString('en-IN', {
    minimumFractionDigits: digits,
    maximumFractionDigits: digits,
  })}`
}

function wholeInr(value: number) {
  return `₹${Math.round(value).toLocaleString('en-IN', { maximumFractionDigits: 0 })}`
}

/** Same split as the API: Haryana is intra-state (CGST + SGST), every other state is IGST. */
function buildQuote(plan: Plan, promoDiscount: number, state: string, sellerState: string): Quote {
  const original = rupees(plan.originalPrice)
  const offered = rupees(plan.offeredPrice)
  const base = offered > original ? offered : original
  let promo = rupees(promoDiscount)
  if (promo < 0) promo = 0
  if (promo > offered) promo = offered
  const subTotal = rupees(offered - promo)
  const intra = state.trim().toLowerCase() === sellerState.trim().toLowerCase()
  const cgst = intra ? percent(subTotal, 9) : 0
  const sgst = intra ? percent(subTotal, 9) : 0
  const igst = intra ? 0 : percent(subTotal, 18)
  return {
    original: base,
    discount: rupees(base - offered + promo),
    subTotal,
    cgst,
    sgst,
    igst,
    total: rupees(subTotal + cgst + sgst + igst),
  }
}

function loadRazorpay() {
  const existing = (window as Window & { Razorpay?: RazorpayCtor }).Razorpay
  if (existing) return Promise.resolve(existing)
  return new Promise<RazorpayCtor>((resolve, reject) => {
    const script = document.createElement('script')
    script.src = 'https://checkout.razorpay.com/v1/checkout.js'
    script.async = true
    script.onload = () => {
      const ctor = (window as Window & { Razorpay?: RazorpayCtor }).Razorpay
      if (ctor) resolve(ctor)
      else reject(new Error('Unable to open the payment window.'))
    }
    script.onerror = () => reject(new Error('Unable to open the payment window.'))
    document.body.appendChild(script)
  })
}

export function DbProfilePaymentPage() {
  const [params] = useSearchParams()
  const cardId = Number(params.get('id') || 0)
  const session = getAuthSession()

  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [page, setPage] = useState<PageData | null>(null)
  const [gstNumber, setGstNumber] = useState('')
  const [name, setName] = useState('')
  const [email, setEmail] = useState('')
  const [phone, setPhone] = useState('')
  const [address, setAddress] = useState('')
  const [state, setState] = useState('')
  const [city, setCity] = useState('')
  const [pincode, setPincode] = useState('')
  const [planId, setPlanId] = useState<number | null>(null)
  const [promoInput, setPromoInput] = useState('')
  const [appliedPromo, setAppliedPromo] = useState('')
  const [promoDiscount, setPromoDiscount] = useState(0)
  const [promoNote, setPromoNote] = useState<string | null>(null)
  const [agreed, setAgreed] = useState(false)
  const [busy, setBusy] = useState(false)
  const [paid, setPaid] = useState(false)

  useEffect(() => {
    if (!session?.email || !cardId) {
      setLoading(false)
      return
    }
    let cancelled = false
    ;(async () => {
      setLoading(true)
      setError(null)
      try {
        const data = normalizePage(
          await apiGet<unknown>('/api/v1/payments/db-profile', { email: session.email, cardId }),
        )
        if (cancelled) return
        setPage(data)
        setGstNumber(data.billing.gstNumber)
        setName(data.billing.name)
        setEmail(data.billing.email)
        setPhone(data.billing.phone)
        setAddress(data.billing.address)
        setState(data.billing.state)
        setCity(data.billing.city)
        setPincode(data.billing.pincode)
        setPlanId(data.plans[0]?.id ?? null)
      } catch (e) {
        if (!cancelled) setError(e instanceof Error ? e.message : 'Unable to load payment details.')
      } finally {
        if (!cancelled) setLoading(false)
      }
    })()
    return () => {
      cancelled = true
    }
  }, [session?.email, cardId])

  const plan = page?.plans.find((p) => p.id === planId) ?? null
  const sellerState = page?.sellerState || 'Haryana'
  const quote = useMemo(
    () => (plan ? buildQuote(plan, promoDiscount, state, sellerState) : null),
    [plan, promoDiscount, state, sellerState],
  )

  async function applyPromo() {
    if (!session?.email || !plan) return
    const code = promoInput.trim()
    if (!code) {
      setPromoNote('Enter a promo code.')
      return
    }
    setBusy(true)
    setPromoNote(null)
    try {
      const res = await apiSend<Record<string, unknown>>('post', '/api/v1/payments/db-profile/promo', {
        email: session.email,
        cardId,
        planId: plan.id,
        promoCode: code,
        state,
      })
      const payload = (res.data ?? {}) as Record<string, unknown>
      const serverQuote = (pick(payload, 'quote', 'Quote') ?? {}) as Record<string, unknown>
      setAppliedPromo(code)
      setPromoDiscount(num(pick(serverQuote, 'promoDiscount', 'PromoDiscount')))
      setPromoNote(res.message || 'Promo code applied.')
    } catch (e) {
      setAppliedPromo('')
      setPromoDiscount(0)
      setPromoNote(e instanceof ApiError ? e.message : 'Unable to apply this promo code.')
    } finally {
      setBusy(false)
    }
  }

  async function proceed() {
    if (!session?.email || !plan || !quote) return
    setBusy(true)
    setError(null)
    try {
      const res = await apiSend<Record<string, unknown>>('post', '/api/v1/payments/db-profile/checkout', {
        email: session.email,
        cardId,
        planId: plan.id,
        promoCode: appliedPromo || undefined,
        gstNumber: gstNumber.trim() || undefined,
        name: name.trim(),
        billingEmail: email.trim(),
        phone: phone.trim(),
        address: address.trim(),
        state: state.trim(),
        city: city.trim(),
        pincode: pincode.trim(),
        agreed,
      })
      const data = (res.data ?? {}) as Record<string, unknown>
      const Razorpay = await loadRazorpay()
      const amount = num(pick(data, 'amount', 'Amount'))
      const invoiceId = num(pick(data, 'invoiceId', 'InvoiceId'))
      const orderId = String(pick(data, 'orderId', 'OrderId') ?? '')
      const checkout = new Razorpay({
        key: String(pick(data, 'keyId', 'KeyId') ?? ''),
        amount: Math.round(amount * 100),
        currency: String(pick(data, 'currency', 'Currency') ?? 'INR'),
        name: 'ShareBizz',
        description: String(pick(data, 'planName', 'PlanName') ?? 'DB Profile'),
        order_id: orderId,
        prefill: { name: name.trim(), email: email.trim(), contact: phone.trim() },
        theme: { color: '#0B1F6E' },
        handler: async (response: { razorpay_order_id?: string; razorpay_payment_id?: string; razorpay_signature?: string }) => {
          try {
            await apiSend('post', '/api/v1/payments/db-profile/verify', {
              email: session.email,
              cardId,
              invoiceId,
              razorpayOrderId: response.razorpay_order_id || orderId,
              razorpayPaymentId: response.razorpay_payment_id || '',
              razorpaySignature: response.razorpay_signature || '',
            })
            setPaid(true)
          } catch (e) {
            setError(e instanceof Error ? e.message : 'Payment could not be verified.')
          }
        },
      })
      checkout.open()
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Unable to start payment.')
    } finally {
      setBusy(false)
    }
  }

  if (!session?.email) {
    return (
      <Shell>
        <p className="px-6 py-10 text-center text-[#4B5563]">
          Please log in to pay for your DB Profile.{' '}
          <Link className="font-semibold text-[#0B1F6E]" to={LOGIN_PATH}>
            Log in
          </Link>
        </p>
      </Shell>
    )
  }

  if (!cardId) {
    return (
      <Shell>
        <p className="px-6 py-10 text-center text-[#4B5563]">
          Open Pay Now from your DB Profile to continue.{' '}
          <Link className="font-semibold text-[#0B1F6E]" to="/dashboard">
            Back to dashboard
          </Link>
        </p>
      </Shell>
    )
  }

  return (
    <Shell>
      <header className="bg-[#0B1F6E] px-5 py-5 text-center text-white">
        <div className="mx-auto mb-2 flex h-11 w-11 items-center justify-center rounded-full bg-white text-[#0B1F6E]">
          <svg width="22" height="22" viewBox="0 0 24 24" fill="none" aria-hidden>
            <path d="M7 3.5h7.2L19 8.2V20.5H7V3.5Z" stroke="currentColor" strokeWidth="1.6" />
            <path d="M14 3.8V8.4H18.6" stroke="currentColor" strokeWidth="1.6" />
            <path d="M9.5 12h5M9.5 15.2h5" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" />
          </svg>
        </div>
        <h1 className="text-[22px] font-bold leading-tight">Billing & GST Details</h1>
        <p className="mt-1 text-sm text-white/80">Please fill in your details to proceed with the payment.</p>
      </header>

      {loading ? <p className="px-6 py-10 text-center text-[#6B7280]">Loading plans…</p> : null}

      {!loading && error && !page ? (
        <p className="px-6 py-10 text-center text-[#B91C1C]">{error}</p>
      ) : null}

      {!loading && page?.alreadyPaid ? (
        <div className="px-6 py-10 text-center">
          <p className="text-[#166534]">This DB Profile is already paid.</p>
          <Link className="mt-4 inline-block font-semibold text-[#0B1F6E]" to="/dashboard">
            Back to dashboard
          </Link>
        </div>
      ) : null}

      {!loading && page && !page.alreadyPaid && paid ? (
        <div className="px-6 py-10 text-center">
          <p className="text-lg font-semibold text-[#166534]">Payment received</p>
          <p className="mt-2 text-sm text-[#4B5563]">Your DB Profile payment is confirmed.</p>
          <Link className="mt-5 inline-flex h-11 items-center rounded-lg bg-[#0B1F6E] px-5 font-semibold text-white" to="/dashboard">
            Go to dashboard
          </Link>
        </div>
      ) : null}

      {!loading && page && !page.alreadyPaid && !paid ? (
        <div className="space-y-4 px-4 py-5 sm:px-5">
          <label className="block">
            <span className="mb-1 block text-[11px] font-semibold tracking-wide text-[#6B7280]">ENTER GST NUMBER (OPTIONAL)</span>
            <input className={fieldClass} value={gstNumber} onChange={(e) => setGstNumber(e.target.value.toUpperCase())} />
          </label>
          <input className={fieldClass} placeholder="Name" value={name} onChange={(e) => setName(e.target.value)} />
          <input className={fieldClass} placeholder="Email" type="email" value={email} onChange={(e) => setEmail(e.target.value)} />
          <input className={fieldClass} placeholder="Phone" inputMode="tel" value={phone} onChange={(e) => setPhone(e.target.value)} />
          <input className={fieldClass} placeholder="Address" value={address} onChange={(e) => setAddress(e.target.value)} />
          <div className="grid grid-cols-2 gap-3">
            <input className={fieldClass} placeholder="State" value={state} onChange={(e) => setState(e.target.value)} />
            <input className={fieldClass} placeholder="City" value={city} onChange={(e) => setCity(e.target.value)} />
          </div>
          <input className={fieldClass} placeholder="Pin Code" inputMode="numeric" value={pincode} onChange={(e) => setPincode(e.target.value)} />

          <div className="pt-2 text-center">
            <p className="text-[11px] font-semibold tracking-[0.14em] text-[#9CA3AF]">CHOOSE YOUR PLAN</p>
          </div>

          <div className="flex items-start gap-2 rounded-lg bg-[#FFF7ED] px-3 py-2 text-sm text-[#C2410C]">
            <span aria-hidden>🎁</span>
            <p>
              <span className="font-semibold">Special Launch Pricing.</span> Prices may be revised in future.
            </p>
          </div>

          {page.plans.length === 0 ? (
            <p className="text-center text-sm text-[#6B7280]">No DB Profile plans are available right now.</p>
          ) : (
            <div className="space-y-3">
              {page.plans.map((item) => {
                const selected = item.id === planId
                return (
                  <label
                    key={item.id}
                    className={`block cursor-pointer rounded-xl border px-3 py-3 ${selected ? 'border-[#0B1F6E] bg-[#F8FAFF] shadow-sm' : 'border-[#E5E7EB] bg-white'}`}
                  >
                    <div className="flex items-start gap-3">
                      <input
                        type="radio"
                        className="mt-1 h-4 w-4 accent-[#0B1F6E]"
                        name="db-profile-plan"
                        checked={selected}
                        onChange={() => {
                          setPlanId(item.id)
                          setAppliedPromo('')
                          setPromoDiscount(0)
                          setPromoNote(null)
                        }}
                      />
                      <div className="min-w-0 flex-1">
                        <div className="flex items-start justify-between gap-3">
                          <div>
                            <p className="font-semibold text-[#111827]">{item.validityLabel}</p>
                            {item.perMonthApprox != null ? (
                              <p className="text-xs text-[#6B7280]">Rs {Math.round(item.perMonthApprox)}/month approx.</p>
                            ) : null}
                            {item.lockLabel ? (
                              <span className="mt-1 inline-block rounded-full bg-[#1D4ED8] px-2 py-0.5 text-[10px] font-bold tracking-wide text-white">
                                {item.lockLabel}
                              </span>
                            ) : null}
                          </div>
                          <div className="text-right">
                            {item.saveAmount > 0 ? (
                              <p className="text-sm text-[#9CA3AF] line-through">{wholeInr(item.originalPriceGst)}</p>
                            ) : null}
                            <p className="text-xl font-bold text-[#F97316]">{wholeInr(item.offeredPriceGst)}</p>
                            {item.saveAmount > 0 ? (
                              <p className="text-xs font-semibold text-[#16A34A]">Save Rs {Math.round(item.saveAmount)}</p>
                            ) : null}
                            {item.badge ? (
                              <span
                                className={`mt-1 inline-block rounded px-1.5 py-0.5 text-[10px] font-bold tracking-wide text-white ${item.badge === 'BEST VALUE' ? 'bg-[#F97316]' : 'bg-[#16A34A]'}`}
                              >
                                {item.badge}
                              </span>
                            ) : null}
                          </div>
                        </div>
                      </div>
                    </div>
                  </label>
                )
              })}
            </div>
          )}

          {quote ? (
            <div className="rounded-xl bg-[#F8FAFC] px-4 py-3 text-sm">
              <Row label="Original Price" value={inr(quote.original)} />
              <Row label="Discount" value={inr(quote.discount)} />
              <Row label="Sub Total" value={inr(quote.subTotal)} />
              <Row label="CGST (9%)" value={inr(quote.cgst)} />
              <Row label="SGST (9%)" value={inr(quote.sgst)} />
              <Row label="IGST (18%)" value={inr(quote.igst)} />
              <div className="mt-2 flex items-center justify-between border-t border-[#E5E7EB] pt-2 text-base font-bold text-[#0B1F6E]">
                <span>Final Total:</span>
                <span>{inr(quote.total)}</span>
              </div>
            </div>
          ) : null}

          <div className="flex gap-2">
            <div className="relative min-w-0 flex-1">
              <span className="pointer-events-none absolute top-3 left-3 text-[#9CA3AF]" aria-hidden>
                🏷
              </span>
              <input
                className={`${fieldClass} pl-9`}
                placeholder="Enter promo code"
                value={promoInput}
                onChange={(e) => setPromoInput(e.target.value.toUpperCase())}
              />
            </div>
            <button
              type="button"
              className="h-11 shrink-0 rounded-lg bg-[#0B1F6E] px-4 text-sm font-semibold text-white disabled:opacity-60"
              disabled={busy || !plan}
              onClick={applyPromo}
            >
              Apply
            </button>
          </div>
          {promoNote ? <p className="text-sm text-[#4B5563]">{promoNote}</p> : null}

          <label className="flex items-start gap-2 text-sm text-[#374151]">
            <input type="checkbox" className="mt-1 h-4 w-4 accent-[#0B1F6E]" checked={agreed} onChange={(e) => setAgreed(e.target.checked)} />
            <span>
              I have read and agree to the{' '}
              <Link className="font-semibold text-[#0B1F6E] underline" to="/terms-conditions" target="_blank">
                Terms, Conditions & Refund Policy
              </Link>{' '}
              and{' '}
              <Link className="font-semibold text-[#0B1F6E] underline" to="/privacy-policy" target="_blank">
                Privacy Policy
              </Link>
              .
            </span>
          </label>

          {error ? <p className="text-sm text-[#B91C1C]">{error}</p> : null}

          <button
            type="button"
            className="flex h-12 w-full items-center justify-center gap-2 rounded-lg bg-[#F5B400] text-sm font-bold tracking-wide text-[#1F2937] disabled:opacity-60"
            disabled={busy || !plan || !agreed}
            onClick={proceed}
          >
            <span aria-hidden>🔒</span>
            {busy ? 'PLEASE WAIT…' : 'PROCEED TO PAY'}
          </button>

          <div className="grid grid-cols-3 gap-2 pt-1 text-center text-[11px] text-[#6B7280]">
            <p>100% Secure Payments</p>
            <p>SSL Encrypted</p>
            <p>Your Data is Safe</p>
          </div>
        </div>
      ) : null}
    </Shell>
  )
}

function Shell({ children }: { children: ReactNode }) {
  return (
    <div className="min-h-screen bg-[#EEF2F7] px-3 py-6 font-[Poppins,sans-serif] text-[#1F2937]">
      <div className="mx-auto w-full max-w-[460px] overflow-hidden rounded-2xl bg-white shadow-[0_12px_40px_rgba(15,23,42,0.12)]">
        {children}
      </div>
    </div>
  )
}

function Row({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex items-center justify-between py-1 text-[#374151]">
      <span>{label}</span>
      <span>{value}</span>
    </div>
  )
}
