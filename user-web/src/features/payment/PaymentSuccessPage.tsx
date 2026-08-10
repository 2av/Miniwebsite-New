import { Link, useSearchParams } from 'react-router-dom'
import { siteUrl } from '@/lib/utils'
import { useSiteStyles } from '@/shared/site/useSiteStyles'

type SuccessKind = 'joining-deal' | 'distributor'

/**
 * Success shells after Razorpay verify (PHP still performs verify + receipt).
 * Deep-links to live receipt / dashboard when payment_id is present.
 */
export function PaymentSuccessPage({ kind }: { kind: SuccessKind }) {
  useSiteStyles()
  const [params] = useSearchParams()
  const paymentId = params.get('payment_id') || params.get('razorpay_payment_id') || ''
  const receiptUrl = paymentId
    ? siteUrl(`payment/download_receipt.php?payment_id=${encodeURIComponent(paymentId)}`)
    : siteUrl('payment/joining_deal_success.php')

  const title =
    kind === 'distributor' ? 'Distributor payment received' : 'Joining deal payment successful'

  return (
    <div className="container py-5 text-center" style={{ maxWidth: 520, margin: '48px auto' }}>
      <h1 style={{ color: '#002169', fontSize: '1.5rem', marginBottom: 12 }}>{title}</h1>
      <p style={{ color: '#6c757d', marginBottom: 24 }}>
        Your payment was recorded. You can download the receipt or return to your dashboard.
      </p>
      <div style={{ display: 'flex', gap: 12, justifyContent: 'center', flexWrap: 'wrap' }}>
        <a className="btn btn-primary" href={receiptUrl}>
          Download receipt
        </a>
        <Link className="btn btn-outline-secondary" to="/dashboard">
          Go to dashboard
        </Link>
      </div>
    </div>
  )
}
