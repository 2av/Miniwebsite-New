import { Link } from 'react-router-dom'
import { assetUrl } from '@/lib/utils'
import { isAuthenticated } from '@/shared/auth/session'
import { useSiteStyles } from '@/shared/site/useSiteStyles'

export function NotFoundPage() {
  useSiteStyles()
  const home = isAuthenticated() ? '/dashboard' : '/'

  return (
    <main className="Dashboard">
      <div className="container-fluid customer_content_area not-found-wrapper" style={wrapStyle}>
        <div className="not-found-card" style={cardStyle}>
          <img
            src={assetUrl('images/error-404-monochrome.svg')}
            alt="404"
            style={{ maxWidth: 220, width: '70%', marginBottom: 20 }}
          />
          <h1 style={{ fontSize: 28, margin: '16px 0 8px' }}>Oops! Page not found</h1>
          <p style={{ fontSize: 15, color: '#666', marginBottom: 20 }}>
            The page you are looking for doesn’t exist, was moved, or the URL is incorrect.
          </p>
          <Link to={home} className="btn btn-primary" style={{ padding: '10px 24px', fontWeight: 600, borderRadius: 6 }}>
            Go to Home
          </Link>
        </div>
      </div>
    </main>
  )
}

const wrapStyle: React.CSSProperties = {
  minHeight: '80vh',
  display: 'flex',
  alignItems: 'center',
  justifyContent: 'center',
  padding: '40px 15px',
}

const cardStyle: React.CSSProperties = {
  maxWidth: 520,
  width: '100%',
  background: '#ffffff',
  borderRadius: 12,
  boxShadow: '0 10px 30px rgba(0,0,0,0.06)',
  textAlign: 'center',
  padding: '30px 24px',
}
