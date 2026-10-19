export type PortalFlags = {
  collaborationEnabled: boolean
  saleskitEnabled: boolean
  influencer: boolean
  mwPaid: boolean
}

/** Role prefix from `roles.Prefix`. SBPU menu is dashboard-only until it is confirmed. */
export type UserTypePrefix = 'SBU' | 'SBPU' | 'FRP' | 'DMP' | 'PSP' | 'SMP' | 'FST' | 'FSP'

export type PortalFeature =
  | 'dashboard'
  | 'referral'
  | 'sales'
  | 'verification'
  | 'customers'
  | 'marketing'
  | 'id-card'

export type PortalMenuItem = {
  id: PortalFeature
  label: string
  icon: string
  url: string
}

const ITEMS: Record<PortalFeature, PortalMenuItem> = {
  dashboard: { id: 'dashboard', label: 'Dashboard', icon: 'fa-line-chart', url: '/dashboard' },
  referral: { id: 'referral', label: 'Referral Details', icon: 'fa-share-alt', url: '/referral' },
  sales: { id: 'sales', label: 'Sales Details', icon: 'fa-bar-chart', url: '/referral' },
  verification: {
    id: 'verification',
    label: 'Document Verification',
    icon: 'fa-check-square-o',
    url: '/verification',
  },
  customers: { id: 'customers', label: 'Customer Manager', icon: 'fa-users', url: '/customer-manager' },
  marketing: { id: 'marketing', label: 'Marketing Resources', icon: 'fa-bullhorn', url: '/kit' },
  'id-card': { id: 'id-card', label: 'ID Card', icon: 'fa-id-card', url: '/id-card' },
}

const PARTNER_MENU: PortalFeature[] = [
  'dashboard',
  'verification',
  'referral',
  'customers',
  'id-card',
  'marketing',
]

/**
 * Single place to add or remove a menu item for a user type.
 * Must stay aligned with `PortalMenu.cs`.
 */
const MENU_BY_PREFIX: Record<UserTypePrefix, PortalFeature[]> = {
  SBU: ['dashboard', 'referral', 'customers', 'verification', 'marketing'],
  SBPU: ['dashboard'],
  FRP: PARTNER_MENU,
  DMP: PARTNER_MENU,
  PSP: PARTNER_MENU,
  SMP: PARTNER_MENU,
  FSP: PARTNER_MENU,
  FST: ['dashboard', 'sales', 'customers', 'id-card', 'marketing'],
}

export function normalizeRolePrefix(prefix?: string | null): UserTypePrefix | null {
  const value = (prefix || '').trim().toUpperCase()
  return value in MENU_BY_PREFIX ? (value as UserTypePrefix) : null
}

export function portalFeaturesFor(prefix?: string | null): PortalFeature[] {
  const key = normalizeRolePrefix(prefix)
  return key ? MENU_BY_PREFIX[key] : ['dashboard']
}

export function hasPortalFeature(prefix: string | null | undefined, feature: PortalFeature) {
  return portalFeaturesFor(prefix).includes(feature)
}

export function getPortalMenu(prefix?: string | null): PortalMenuItem[] {
  return portalFeaturesFor(prefix).map((id) => ITEMS[id])
}

/** Account pages and the website builder stay available to every signed-in user. */
function isOpenPortalPath(pathname: string) {
  return pathname === '/change-password' || pathname === '/website' || pathname.startsWith('/website/')
}

function pathFeature(pathname: string): PortalFeature | 'referral-area' | null {
  if (pathname === '/dashboard' || pathname.startsWith('/dashboard/')) return 'dashboard'
  if (pathname === '/referral' || pathname.startsWith('/referral/') || pathname === '/collaboration')
    return 'referral-area'
  if (pathname === '/verification' || pathname.startsWith('/verification/')) return 'verification'
  if (
    pathname === '/customer-manager' ||
    pathname.startsWith('/customer-manager/') ||
    pathname === '/customer-tracker' ||
    pathname === '/grow-with-mw'
  )
    return 'customers'
  if (pathname === '/kit' || pathname.startsWith('/kit/')) return 'marketing'
  if (pathname === '/id-card' || pathname === '/idcard') return 'id-card'
  return null
}

/** True when this user type may open the path. Unknown portal paths are closed. */
export function canAccessPortalPath(prefix: string | null | undefined, pathname: string) {
  if (isOpenPortalPath(pathname)) return true
  const feature = pathFeature(pathname)
  if (!feature) return false
  if (feature === 'referral-area') return hasPortalFeature(prefix, 'referral') || hasPortalFeature(prefix, 'sales')
  return hasPortalFeature(prefix, feature)
}

/** Sidebar item for website builder (mirrors menu_config.json website_menu_items + PHP extras). */
export type WebsiteNavItem = {
  id: string
  label: string
  icon: string
  url: string
  locked?: boolean
  external?: boolean
  separatorBefore?: boolean
}

const WEBSITE_CORE: Omit<WebsiteNavItem, 'url' | 'locked'>[] = [
  { id: 'ws_dashboard', label: 'Dashboard', icon: 'tachometer' },
  { id: 'ws_business_name', label: 'Business Name', icon: 'edit', separatorBefore: true },
  { id: 'ws_select_theme', label: 'Select Theme', icon: 'paint-brush' },
  { id: 'ws_company_details', label: 'Company Details', icon: 'building' },
  { id: 'ws_services', label: 'Services', icon: 'cogs' },
  { id: 'ws_special_offers', label: 'Special Offers', icon: 'tag' },
  { id: 'ws_products', label: 'Details & Pricing', icon: 'shopping-cart' },
  { id: 'ws_videos', label: 'Videos', icon: 'video-camera' },
  { id: 'ws_image_gallery', label: 'Image Gallery', icon: 'image' },
  { id: 'ws_payment_details', label: 'Payment Details', icon: 'credit-card' },
  { id: 'ws_social_links', label: 'Social Links', icon: 'share-alt' },
]

const STEP_PATH: Record<string, string> = {
  ws_dashboard: '/dashboard',
  ws_business_name: '/website/business-name',
  ws_select_theme: '/website/select-theme',
  ws_company_details: '/website/company-details',
  ws_services: '/website/services',
  ws_special_offers: '/website/special-offers',
  ws_products: '/website/products',
  ws_videos: '/website/videos',
  ws_image_gallery: '/website/image-gallery',
  ws_payment_details: '/website/payment-details',
  ws_social_links: '/website/social-links',
}

/**
 * Same menu PHP shows when $current_dir === 'website'.
 * Steps after Business Name lock until cardId exists; Preview locks until slug exists.
 */
export function getWebsiteBuilderMenu(
  cardId: number | null,
  flags: PortalFlags,
  cardSlug?: string | null,
): WebsiteNavItem[] {
  const q = cardId ? `?card_number=${cardId}` : ''
  const lockAfterBusiness = !cardId
  let pastBusiness = false

  const items: WebsiteNavItem[] = WEBSITE_CORE.map((raw) => {
    if (raw.id === 'ws_business_name') pastBusiness = true
    const path = STEP_PATH[raw.id] || '/'
    const isDash = raw.id === 'ws_dashboard'
    return {
      ...raw,
      url: isDash ? path : `${path}${q}`,
      locked: pastBusiness && raw.id !== 'ws_business_name' && lockAfterBusiness,
    }
  })

  if (flags.collaborationEnabled) {
    items.push({
      id: 'ws_collaboration',
      label: 'Collaboration Details',
      icon: 'exchange',
      url: '/collaboration',
    })
  }
  if (flags.saleskitEnabled && !flags.collaborationEnabled) {
    items.push({ id: 'ws_sales_kit', label: 'Sales Kit', icon: 'folder-open', url: '/kit' })
  }
  if (flags.collaborationEnabled) {
    items.push({ id: 'ws_marketing_kit', label: 'Marketing Kit', icon: 'bullhorn', url: '/kit' })
  }

  const slug = (cardSlug || '').trim()
  items.push({
    id: 'ws_preview',
    label: 'Preview',
    icon: 'external-link',
    url: slug ? `/n/${encodeURIComponent(slug)}` : '#',
    external: true,
    separatorBefore: true,
    locked: lockAfterBusiness || !slug,
  })

  return items
}
