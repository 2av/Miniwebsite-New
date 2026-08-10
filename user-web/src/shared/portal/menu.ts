import type { UserRole } from '@/shared/types/api'

export type PortalFlags = {
  collaborationEnabled: boolean
  saleskitEnabled: boolean
  influencer: boolean
  mwPaid: boolean
}

type MenuItem = {
  id: string
  label: string
  icon: string
  url: string
  roles: UserRole[]
  alwaysVisible?: boolean
  roleAccessFeature?: string
  conditions?: Partial<Record<UserRole, Record<string, boolean>>>
}

/** Mirrors user/menu_config.json visibility rules (role-access features treated as allowed for now). */
const MENU: MenuItem[] = [
  {
    id: 'dashboard',
    label: 'Dashboard',
    icon: 'fa-line-chart',
    url: '/dashboard',
    roles: ['CUSTOMER', 'FRANCHISEE', 'TEAM'],
    alwaysVisible: true,
  },
  {
    id: 'collaboration',
    label: 'Referral Details',
    icon: 'fa-exchange',
    url: '/collaboration',
    roles: ['CUSTOMER'],
    conditions: { CUSTOMER: { collaboration_enabled: true } },
  },
  {
    id: 'verification',
    label: 'Verification',
    icon: 'fa-check-square-o',
    url: '/verification',
    roles: ['FRANCHISEE'],
    alwaysVisible: true,
  },
  {
    id: 'wallet',
    label: 'Wallet',
    icon: 'fa-money',
    url: '/wallet',
    roles: ['FRANCHISEE'],
    alwaysVisible: true,
  },
  {
    id: 'franchisee_mw_sales_kit',
    label: 'MW Sales Kit',
    icon: 'fa-folder-open',
    url: '/kit',
    roles: ['FRANCHISEE'],
  },
  {
    id: 'franchisee_franchise_sales_kit',
    label: 'Franchisee Sales Kit',
    icon: 'fa-suitcase',
    url: '/kit?kit=franchise_sales',
    roles: ['FRANCHISEE'],
  },
  {
    id: 'referral_customer',
    label: 'Referral Details',
    icon: 'fa-share-alt',
    url: '/referral',
    roles: ['CUSTOMER'],
    conditions: { CUSTOMER: { collaboration_enabled: false } },
  },
  {
    id: 'sales_details_team',
    label: 'Sales Details',
    icon: 'fa-bar-chart',
    url: '/referral',
    roles: ['TEAM'],
    alwaysVisible: true,
  },
  {
    id: 'customer_tracker',
    label: 'Customer Manager',
    icon: 'fa-users',
    url: '/customer-manager',
    roles: ['TEAM', 'FRANCHISEE'],
  },
  {
    id: 'customer_tracker_collaboration',
    label: 'Customer Manager',
    icon: 'fa-users',
    url: '/customer-manager',
    roles: ['CUSTOMER'],
    conditions: { CUSTOMER: { collaboration_enabled: true, mw_paid: true } },
  },
  {
    id: 'customer_tracker_customer',
    label: 'Customer Manager',
    icon: 'fa-users',
    url: '/customer-manager',
    roles: ['CUSTOMER'],
    conditions: { CUSTOMER: { collaboration_enabled: false, mw_paid: true } },
  },
  {
    id: 'kit',
    label: 'MW Sales Kit',
    icon: 'fa-folder-open',
    url: '/kit',
    roles: ['TEAM'],
  },
  {
    id: 'franchisee_sales_kit_team',
    label: 'Franchisee Sales Kit',
    icon: 'fa-suitcase',
    url: '/kit?kit=franchise_sales',
    roles: ['TEAM'],
  },
  {
    id: 'sales_kit_customer',
    label: 'Sales Kit',
    icon: 'fa-folder-open',
    url: '/kit',
    roles: ['CUSTOMER'],
    conditions: { CUSTOMER: { saleskit_enabled: true, collaboration_enabled: false } },
  },
  {
    id: 'idcard',
    label: 'ID Card',
    icon: 'fa-id-card',
    url: '/id-card',
    roles: ['TEAM', 'FRANCHISEE'],
  },
  {
    id: 'idcard_customer',
    label: 'ID Card',
    icon: 'fa-id-card',
    url: '/id-card',
    roles: ['CUSTOMER'],
    conditions: { CUSTOMER: { collaboration_enabled: true, influencer: false } },
  },
  {
    id: 'marketing_kit',
    label: 'Creator Kit',
    icon: 'fa-bullhorn',
    url: '/kit',
    roles: ['CUSTOMER'],
    conditions: { CUSTOMER: { collaboration_enabled: true } },
  },
  {
    id: 'mw_sales_kit_influencer',
    label: 'MW Sales Kit',
    icon: 'fa-folder-open',
    url: '/kit?kit=mw_sales',
    roles: ['CUSTOMER'],
    conditions: { CUSTOMER: { collaboration_enabled: true, influencer: true } },
  },
]

function matchesConditions(item: MenuItem, role: UserRole, flags: PortalFlags) {
  if (item.alwaysVisible) return true
  const cond = item.conditions?.[role]
  if (!cond) return true
  const map: Record<string, boolean> = {
    collaboration_enabled: flags.collaborationEnabled,
    saleskit_enabled: flags.saleskitEnabled,
    influencer: flags.influencer,
    mw_paid: flags.mwPaid,
  }
  return Object.entries(cond).every(([k, v]) => map[k] === v)
}

export function getPortalMenu(role: UserRole, flags: PortalFlags) {
  return MENU.filter((item) => item.roles.includes(role) && matchesConditions(item, role, flags))
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
