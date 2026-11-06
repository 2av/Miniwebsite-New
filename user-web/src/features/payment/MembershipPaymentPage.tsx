import { useEffect, useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { apiGet, apiSend, ApiError } from '@/shared/api/client'
import { getAuthSession, LOGIN_PATH } from '@/shared/auth/session'

type Plan = { id: number; planName: string; offeredPriceGst: number }
type RazorpayCtor = new (options: Record<string, unknown>) => { open: () => void }

function pick<T>(obj: Record<string, unknown>, camel: string, pascal: string): T {
  return (obj[camel] ?? obj[pascal]) as T
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

export function MembershipPaymentPage() {
  const session = getAuthSession()
  const [params] = useSearchParams()
  const payerEmail = session?.email || params.get('email') || ''
  const [feeLabel, setFeeLabel] = useState('Membership')
  const [plans, setPlans] = useState<Plan[]>([])
  const [planId, setPlanId] = useState(0)
  const [paid, setPaid] = useState(false)
  const [name, setName] = useState(session?.name ?? '')
  const [email, setEmail] = useState(session?.email ?? '')
  const [phone, setPhone] = useState('')
  const [address, setAddress] = useState('')
  const [state, setState] = useState('')
  const [city, setCity] = useState('')
  const [pincode, setPincode] = useState('')
  const [gstNumber, setGstNumber] = useState('')
  const [agreed, setAgreed] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  useEffect(() => {
    if (!payerEmail) return
    let cancelled = false
    ;(async () => {
      try {
        const raw = await apiGet<Record<string, unknown>>('/api/v1/payments/membership', { email: payerEmail })
        if (cancelled) return
        setFeeLabel(String(pick(raw, 'feeLabel', 'FeeLabel') ?? 'Membership'))
        setPaid(Boolean(pick(raw, 'alreadyPaid', 'AlreadyPaid')))
        const billing = (pick(raw, 'billing', 'Billing') ?? {}) as Record<string, unknown>
        setName(String(pick(billing, 'name', 'Name') ?? session?.name ?? ''))
        setEmail(String(pick(billing, 'email', 'Email') ?? payerEmail))
        setPhone(String(pick(billing, 'phone', 'Phone') ?? ''))
        setAddress(String(pick(billing, 'address', 'Address') ?? ''))
        setState(String(pick(billing, 'state', 'State') ?? ''))
        setCity(String(pick(billing, 'city', 'City') ?? ''))
        setPincode(String(pick(billing, 'pincode', 'Pincode') ?? ''))
        setGstNumber(String(pick(billing, 'gstNumber', 'GstNumber') ?? ''))
        const list = ((pick(raw, 'plans', 'Plans') as unknown[]) || []).map((item) => {
          const row = (item ?? {}) as Record<string, unknown>
          return {
            id: Number(pick(row, 'id', 'Id') ?? 0),
            planName: String(pick(row, 'planName', 'PlanName') ?? ''),
            offeredPriceGst: Number(pick(row, 'offeredPriceGst', 'OfferedPriceGst') ?? 0),
          }
        })
        setPlans(list)
        if (list[0]) setPlanId(list[0].id)
      } catch (e) {
        if (!cancelled) setError(e instanceof Error ? e.message : 'Unable to load membership payment.')
      }
    })()
    return () => {
      cancelled = true
    }
  }, [payerEmail, session?.name])

  async function proceed(e: React.FormEvent) {
    e.preventDefault()
    if (!payerEmail) return
    setBusy(true)
    setError(null)
    try {
      const res = await apiSend<Record<string, unknown>>('post', '/api/v1/payments/membership/checkout', {
        email: payerEmail,
        planId,
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
      if (Boolean(pick(data, 'paymentCompleted', 'PaymentCompleted'))) {
        setPaid(true)
        return
      }
      const Razorpay = await loadRazorpay()
      const amount = Number(pick(data, 'amount', 'Amount') ?? 0)
      const invoiceId = Number(pick(data, 'invoiceId', 'InvoiceId') ?? 0)
      const orderId = String(pick(data, 'orderId', 'OrderId') ?? '')
      const checkout = new Razorpay({
        key: String(pick(data, 'keyId', 'KeyId') ?? ''),
        amount: Math.round(amount * 100),
        currency: 'INR',
        name: 'ShareBizz',
        description: feeLabel,
        order_id: orderId,
        handler: async (response: { razorpay_order_id?: string; razorpay_payment_id?: string; razorpay_signature?: string }) => {
          await apiSend('post', '/api/v1/payments/membership/verify', {
            email: payerEmail,
            invoiceId,
            razorpayOrderId: response.razorpay_order_id || orderId,
            razorpayPaymentId: response.razorpay_payment_id || '',
            razorpaySignature: response.razorpay_signature || '',
          })
          setPaid(true)
        },
      })
      checkout.open()
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Unable to start payment.')
    } finally {
      setBusy(false)
    }
  }

  if (!payerEmail) {
    return (
      <p className="p-8 text-center">
        <Link to={LOGIN_PATH}>Log in</Link> to pay.
      </p>
    )
  }

  return (
    <main className="mx-auto max-w-xl p-6">
      <h1 className="mb-2 text-2xl font-semibold">{feeLabel}</h1>
      <p className="mb-4 text-sm text-[#4B5563]">Payment confirmation unlocks document verification.</p>
      {error && <p className="mb-3 text-red-600">{error}</p>}
      {paid ? (
        <p>
          Payment received. <Link to="/verification">Continue to document verification</Link>
        </p>
      ) : plans.length === 0 ? (
        <p>No membership plan is configured yet. Ask the SB team to add a Membership pricing plan.</p>
      ) : (
        <form className="grid gap-3" onSubmit={(e) => void proceed(e)}>
          <label className="grid gap-1 text-sm">
            Plan
            <select className="h-11 rounded-lg border px-3" value={planId} onChange={(e) => setPlanId(Number(e.target.value))}>
              {plans.map((plan) => (
                <option key={plan.id} value={plan.id}>
                  {plan.planName} · ₹ {plan.offeredPriceGst.toLocaleString('en-IN')}
                </option>
              ))}
            </select>
          </label>
          {(
            [
              ['Name', name, setName],
              ['Email', email, setEmail],
              ['Phone', phone, setPhone],
              ['Address', address, setAddress],
              ['State', state, setState],
              ['City', city, setCity],
              ['Pincode', pincode, setPincode],
              ['GST (optional)', gstNumber, setGstNumber],
            ] as Array<[string, string, (value: string) => void]>
          ).map(([label, value, setter]) => (
            <label key={label} className="grid gap-1 text-sm">
              {label}
              <input
                className="h-11 rounded-lg border px-3"
                value={value}
                onChange={(e) => setter(e.target.value)}
              />
            </label>
          ))}
          <label className="flex items-center gap-2 text-sm">
            <input type="checkbox" checked={agreed} onChange={(e) => setAgreed(e.target.checked)} />
            I agree to the Terms and Privacy Policy.
          </label>
          <button type="submit" className="sb-btn-primary" disabled={busy}>
            {busy ? 'Please wait…' : `Pay ${feeLabel}`}
          </button>
        </form>
      )}
    </main>
  )
}
