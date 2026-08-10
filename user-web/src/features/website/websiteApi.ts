import { apiGet, apiSend } from '@/shared/api/client'

function ownerQS(email: string) {
  return { ownerEmail: email }
}

export const websiteApi = {
  checkName: (compName: string, excludeId?: number | null) =>
    apiGet<{ available: boolean; slug: string; message?: string }>('/api/v1/digi-cards/check-name', {
      compName,
      excludeId: excludeId || undefined,
    }),

  createBusiness: (body: Record<string, unknown>) =>
    apiSend<Record<string, unknown>>('post', '/api/v1/digi-cards', body),

  getBusiness: (cardId: number, email: string) =>
    apiGet<Record<string, unknown>>(`/api/v1/digi-cards/${cardId}/business-name`, ownerQS(email)),

  putBusiness: (cardId: number, body: Record<string, unknown>) =>
    apiSend<Record<string, unknown>>('put', `/api/v1/digi-cards/${cardId}/business-name`, body),

  listThemes: () => apiGet<unknown[]>('/api/v1/digi-cards/themes'),

  getTheme: (cardId: number, email: string) =>
    apiGet<Record<string, unknown>>(`/api/v1/digi-cards/${cardId}/theme`, ownerQS(email)),

  putTheme: (cardId: number, email: string, dCss: string) =>
    apiSend('put', `/api/v1/digi-cards/${cardId}/theme?ownerEmail=${encodeURIComponent(email)}`, { dCss }),

  getCompany: (cardId: number, email: string) =>
    apiGet<Record<string, unknown>>(`/api/v1/digi-cards/${cardId}/company-details`, ownerQS(email)),

  putCompany: (cardId: number, email: string, body: Record<string, unknown>) =>
    apiSend('put', `/api/v1/digi-cards/${cardId}/company-details?ownerEmail=${encodeURIComponent(email)}`, body),

  getSocial: (cardId: number, email: string) =>
    apiGet<Record<string, unknown>>(`/api/v1/digi-cards/${cardId}/social-links`, ownerQS(email)),

  putSocial: (cardId: number, email: string, body: Record<string, unknown>) =>
    apiSend('put', `/api/v1/digi-cards/${cardId}/social-links?ownerEmail=${encodeURIComponent(email)}`, body),

  getPayment: (cardId: number, email: string) =>
    apiGet<Record<string, unknown>>(`/api/v1/digi-cards/${cardId}/payment-details`, ownerQS(email)),

  putPayment: (cardId: number, email: string, body: Record<string, unknown>) =>
    apiSend('put', `/api/v1/digi-cards/${cardId}/payment-details?ownerEmail=${encodeURIComponent(email)}`, body),

  getVideos: (cardId: number, email: string) =>
    apiGet<Record<string, unknown>>(`/api/v1/digi-cards/${cardId}/videos`, ownerQS(email)),

  putVideos: (cardId: number, email: string, urls: (string | null)[]) =>
    apiSend('put', `/api/v1/digi-cards/${cardId}/videos?ownerEmail=${encodeURIComponent(email)}`, { urls }),

  listProducts: (cardId: number, email: string) =>
    apiGet<unknown[]>(`/api/v1/digi-cards/${cardId}/products`, ownerQS(email)),

  createProduct: (cardId: number, email: string, body: Record<string, unknown>) =>
    apiSend('post', `/api/v1/digi-cards/${cardId}/products?ownerEmail=${encodeURIComponent(email)}`, body),

  updateProduct: (cardId: number, productId: number, email: string, body: Record<string, unknown>) =>
    apiSend('put', `/api/v1/digi-cards/${cardId}/products/${productId}?ownerEmail=${encodeURIComponent(email)}`, body),

  deleteProduct: (cardId: number, productId: number, email: string) =>
    apiSend('delete', `/api/v1/digi-cards/${cardId}/products/${productId}?ownerEmail=${encodeURIComponent(email)}`),

  listServices: (cardId: number, email: string) =>
    apiGet<unknown[]>(`/api/v1/digi-cards/${cardId}/services`, ownerQS(email)),

  createService: (cardId: number, email: string, body: Record<string, unknown>) =>
    apiSend('post', `/api/v1/digi-cards/${cardId}/services?ownerEmail=${encodeURIComponent(email)}`, body),

  updateService: (cardId: number, serviceId: number, email: string, body: Record<string, unknown>) =>
    apiSend('put', `/api/v1/digi-cards/${cardId}/services/${serviceId}?ownerEmail=${encodeURIComponent(email)}`, body),

  deleteService: (cardId: number, serviceId: number, email: string) =>
    apiSend('delete', `/api/v1/digi-cards/${cardId}/services/${serviceId}?ownerEmail=${encodeURIComponent(email)}`),

  listGallery: (cardId: number, email: string) =>
    apiGet<unknown[]>(`/api/v1/digi-cards/${cardId}/gallery`, ownerQS(email)),

  createGallery: (cardId: number, email: string, body: Record<string, unknown>) =>
    apiSend('post', `/api/v1/digi-cards/${cardId}/gallery?ownerEmail=${encodeURIComponent(email)}`, body),

  deleteGallery: (cardId: number, imageId: number, email: string) =>
    apiSend('delete', `/api/v1/digi-cards/${cardId}/gallery/${imageId}?ownerEmail=${encodeURIComponent(email)}`),

  listOffers: (cardId: number, email: string) =>
    apiGet<unknown[]>(`/api/v1/digi-cards/${cardId}/special-offers`, ownerQS(email)),

  createOffer: (cardId: number, email: string, body: Record<string, unknown>) =>
    apiSend('post', `/api/v1/digi-cards/${cardId}/special-offers?ownerEmail=${encodeURIComponent(email)}`, body),

  updateOffer: (cardId: number, offerId: number, email: string, body: Record<string, unknown>) =>
    apiSend('put', `/api/v1/digi-cards/${cardId}/special-offers/${offerId}?ownerEmail=${encodeURIComponent(email)}`, body),

  deleteOffer: (cardId: number, offerId: number, email: string) =>
    apiSend('delete', `/api/v1/digi-cards/${cardId}/special-offers/${offerId}?ownerEmail=${encodeURIComponent(email)}`),
}

export function pickVal(o: Record<string, unknown>, camel: string, pascal: string) {
  return o[camel] ?? o[pascal]
}
