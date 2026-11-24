import { createContext, createElement, useContext, type ReactNode } from 'react'

export type CompanyForm = {
  firstName: string
  lastName: string
  positionPrimary: string
  positionSecondary: string
  position: string
  contact: string
  contact2: string
  whatsapp: string
  gstNumber: string
  address: string
  address2: string
  city: string
  state: string
  pincode: string
  country: string
  email: string
  website: string
  location: string
  compEstDate: string
  aboutUs: string
  businessHours: string
  businessProfileType: string
  businessType: string
  businessOperationArea: string
  businessOperationLocations: string
}

export type OwnedService = {
  id: number
  productName: string
  productDescription: string
  productImage: string
  productCategory: number | null
  categorySource: string
  displayOrder: number
}

export type OwnedProduct = OwnedService & {
  mrp: number
  sellingPrice: number
  priceType: string
  pricingUnit: string
  ctaText: string
}

export type OwnedOffer = {
  id: number
  offerTitle: string
  offerDescription: string
  offerImage: string
  badge: string
  discountPercentage: number
  status: string
  displayOrder: number
  startDate: string | null
  startTime: string | null
  endDate: string | null
  endTime: string | null
}

export type OwnedGallery = {
  id: number
  galleryImage: string
  galleryImageUrl: string
  displayOrder: number
}

export type DialogRequest =
  | { kind: 'logo' | 'hero' | 'images' | 'address' | 'company' | 'about' | 'service-add' | 'product-add' | 'offer-add' | 'gallery-add' | 'videos' | 'social' | 'payment' | 'appointment' }
  | { kind: 'service-image' | 'product-image' | 'offer-image'; index: number }

export type ProfileEditApi = {
  revision: number
  company: CompanyForm
  patchCompany: (patch: Partial<CompanyForm>) => Promise<void>
  patchService: (index: number, patch: { productName?: string; productDescription?: string }) => Promise<void>
  patchProduct: (
    index: number,
    patch: { productName?: string; productDescription?: string; mrp?: number; sellingPrice?: number },
  ) => Promise<void>
  patchOffer: (
    index: number,
    patch: { offerTitle?: string; offerDescription?: string; discountPercentage?: number; badge?: string },
  ) => Promise<void>
  removeService: (index: number) => Promise<void>
  removeProduct: (index: number) => Promise<void>
  removeOffer: (index: number) => Promise<void>
  removeGallery: (index: number) => Promise<void>
  openDialog: (request: DialogRequest) => void
  openThemeDrawer: () => void
  offerCount: number
  productCount: number
  galleryCount: number
  uploadCompanyImage: (kind: 'logo' | 'hero', file: File, onProgress?: (percent: number) => void) => Promise<void>
}

const ProfileEditContext = createContext<ProfileEditApi | null>(null)

export function ProfileEditProvider({ value, children }: { value: ProfileEditApi; children: ReactNode }) {
  return createElement(ProfileEditContext.Provider, { value }, children)
}

export function useProfileEdit() {
  return useContext(ProfileEditContext)
}
