import { Handshake, Monitor, Printer, Share2, Store, UsersRound } from 'lucide-react'
import type { ComponentType } from 'react'

export type SalesChannelPartnerType = {
  slug: string
  label: string
  icon: ComponentType<{ size?: number; className?: string }>
}

/** Slugs must match the API catalog in AdminSalesChannelPartnersService. */
export const SALES_CHANNEL_PARTNERS: SalesChannelPartnerType[] = [
  { slug: 'franchisee-partner', label: 'Franchisee Partner', icon: Handshake },
  { slug: 'print-shop-partner', label: 'Print Shop Partner', icon: Printer },
  { slug: 'digital-marketing-partner', label: 'Digital Marketing Partner', icon: Monitor },
  { slug: 'social-media-partner', label: 'Social Media Partner', icon: Share2 },
  { slug: 'field-sales-partner', label: 'Field Sales Partner', icon: Store },
  { slug: 'field-sales-team', label: 'Field Sales Team', icon: UsersRound },
]

export const SALES_CHANNEL_PARTNER_BASE = '/sales-channel-partner'

export function findSalesChannelPartner(slug?: string) {
  return SALES_CHANNEL_PARTNERS.find((p) => p.slug === slug)
}
