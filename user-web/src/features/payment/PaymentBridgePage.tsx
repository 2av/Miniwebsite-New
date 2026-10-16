import { useEffect, useMemo } from 'react'
import { useLocation, useSearchParams } from 'react-router-dom'
import { siteUrl } from '@/lib/utils'
import { useSiteStyles } from '@/shared/site/useSiteStyles'

type BridgeKind = 'pay' | 'miniwebsite' | 'joining-deal'

const PHP_PATH: Record<BridgeKind, string> = {
  pay: 'payment/pay.php',
  miniwebsite: 'payment/pay_miniwebsite.php',
  'joining-deal': 'payment/joining_deal_pay.php',
}

/**
 * Thin checkout bridges: Razorpay order/verify stay on the live PHP payment pages
 * (session + secret key). React owns the route and forwards query params.
 */
export function PaymentBridgePage({ kind }: { kind: BridgeKind }) {
  useSiteStyles()
  const [params] = useSearchParams()
  const location = useLocation()

  const target = useMemo(() => {
    const qs = params.toString()
    const path = PHP_PATH[kind]
    return siteUrl(qs ? `${path}?${qs}` : path)
  }, [kind, params])

  useEffect(() => {
    // Franchise / joining-deal flows that POST form bodies still use live PHP forms.
    // Query-based checkouts (e.g. card id) redirect here.
    if (location.state && kind === 'joining-deal') {
      // Allow a brief render so POST body isn't lost when opened as a GET shell.
    }
    window.location.replace(target)
  }, [target, location.state, kind])

  return (
    <div className="container py-5 text-center" style={{ maxWidth: 480, margin: '40px auto' }}>
      <h1 style={{ color: '#002169', fontSize: '1.35rem', marginBottom: 12 }}>Redirecting to secure payment…</h1>
      <p style={{ color: '#6c757d', marginBottom: 20 }}>
        Checkout is processed on ShareBizz&apos;s payment server. If you are not redirected, use the link below.
      </p>
      <a className="btn btn-primary" href={target}>
        Continue to payment
      </a>
    </div>
  )
}
