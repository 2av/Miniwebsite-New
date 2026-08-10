import { Link } from 'react-router-dom'
import { useSiteStyles } from '@/shared/site/useSiteStyles'

export function RegistrationHubPage() {
  useSiteStyles()

  return (
    <div className="login-wrap">
      <div className="login-container" style={{ textAlign: 'center' }}>
        <h2 className="heading">
          <Link to="/">
            <i className="fa fa-angle-left" aria-hidden="true" />
          </Link>
          Create Account
        </h2>
        <p className="text-white mb-4">Choose how you want to join MiniWebsite</p>
        <Link to="/registration/customer" className="btn btn-login" style={{ display: 'block', marginBottom: 12 }}>
          Customer Registration
        </Link>
        <Link to="/registration/franchisee" className="btn btn-login" style={{ display: 'block' }}>
          Franchisee Registration
        </Link>
      </div>
    </div>
  )
}
