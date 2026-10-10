import { Navigate, Outlet, useLocation, useSearchParams } from 'react-router-dom'
import { useAuthSession } from '@/shared/auth/useAuthSession'
import { isSessionValid, LOGIN_PATH, redirectToLogin } from '@/shared/auth/session'
import { useCardNumberFromSearch } from '@/features/website/cardContext'

/**
 * Thin shell for /website/* routes.
 * Sidebar navigation is handled by UserPortalLayout (website builder menu),
 * matching PHP header.php when $current_dir === 'website'.
 */
export function WebsiteBuilderLayout() {
  const session = useAuthSession()
  const [params] = useSearchParams()
  const location = useLocation()
  const cardId = useCardNumberFromSearch(params)

  if (!session) return <Navigate to={LOGIN_PATH} replace />
  if (!isSessionValid(session)) {
    redirectToLogin('expired')
    return null
  }

  const onBusinessName = location.pathname.includes('/business-name')
  if (!cardId && !onBusinessName) {
    return <Navigate to="/website/business-name" replace />
  }

  return <Outlet context={{ cardId, email: session.email }} />
}

export type WebsiteOutletContext = { cardId: number | null; email: string }
