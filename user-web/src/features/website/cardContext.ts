const KEY = 'mw_card_id_inprocess'

export function getCardInProcess(): number | null {
  const raw = sessionStorage.getItem(KEY) || localStorage.getItem(KEY)
  const n = Number(raw)
  return Number.isFinite(n) && n > 0 ? n : null
}

export function setCardInProcess(cardId: number) {
  const v = String(cardId)
  sessionStorage.setItem(KEY, v)
  localStorage.setItem(KEY, v)
}

export function clearCardInProcess() {
  sessionStorage.removeItem(KEY)
  localStorage.removeItem(KEY)
}

export function useCardNumberFromSearch(searchParams: URLSearchParams): number | null {
  const fromUrl = Number(searchParams.get('card_number') || searchParams.get('cardNumber') || 0)
  if (fromUrl > 0) {
    setCardInProcess(fromUrl)
    return fromUrl
  }
  return getCardInProcess()
}

export const WEBSITE_STEPS = [
  { id: 'business-name', label: 'Business Name', path: '/website/business-name', needsCard: false },
  { id: 'select-theme', label: 'Select Theme', path: '/website/select-theme', needsCard: true },
  { id: 'company-details', label: 'Company Details', path: '/website/company-details', needsCard: true },
  { id: 'services', label: 'Services', path: '/website/services', needsCard: true },
  { id: 'special-offers', label: 'Special Offers', path: '/website/special-offers', needsCard: true },
  { id: 'products', label: 'Details & Pricing', path: '/website/products', needsCard: true },
  { id: 'videos', label: 'Videos', path: '/website/videos', needsCard: true },
  { id: 'image-gallery', label: 'Image Gallery', path: '/website/image-gallery', needsCard: true },
  { id: 'payment-details', label: 'Payment Details', path: '/website/payment-details', needsCard: true },
  { id: 'social-links', label: 'Social Links', path: '/website/social-links', needsCard: true },
] as const

export function stepPath(path: string, cardId: number | null) {
  if (!cardId) return path
  return `${path}?card_number=${cardId}`
}
