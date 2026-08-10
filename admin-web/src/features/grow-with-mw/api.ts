import type {
  DocMediaItem,
  DocMediaList,
  DocPageDetail,
  DocPagesPage,
  DocSection,
  GrowWithMwMeta,
  UpsertDocPagePayload,
  UpsertDocSectionPayload,
} from '@/shared/types/api'
import { api, apiGet, apiSend, ApiError } from '@/shared/api/client'

const BASE = '/api/v1/admin/grow-with-mw'

export function fetchGrowWithMwMeta() {
  return apiGet<GrowWithMwMeta>(`${BASE}/meta`)
}

export function fetchDocPages(query: {
  page?: number
  pageSize?: number
  search?: string
  sectionId?: number
  status?: string
}) {
  return apiGet<DocPagesPage>(`${BASE}/pages`, query as Record<string, unknown>)
}

export function fetchDocPage(id: number) {
  return apiGet<DocPageDetail>(`${BASE}/pages/${id}`)
}

export function createDocPage(payload: UpsertDocPagePayload) {
  return apiSend<DocPageDetail>('post', `${BASE}/pages`, payload)
}

export function updateDocPage(id: number, payload: UpsertDocPagePayload) {
  return apiSend<DocPageDetail>('put', `${BASE}/pages/${id}`, payload)
}

export function deleteDocPage(id: number) {
  return apiSend('delete', `${BASE}/pages/${id}`)
}

export function reorderDocPages(sectionId: number, order: number[]) {
  return apiSend('post', `${BASE}/pages/reorder`, { sectionId, order })
}

export function fetchDocSections() {
  return apiGet<DocSection[]>(`${BASE}/sections`)
}

export function createDocSection(payload: UpsertDocSectionPayload) {
  return apiSend<DocSection>('post', `${BASE}/sections`, payload)
}

export function updateDocSection(id: number, payload: UpsertDocSectionPayload) {
  return apiSend<DocSection>('put', `${BASE}/sections/${id}`, payload)
}

export function deleteDocSection(id: number) {
  return apiSend('delete', `${BASE}/sections/${id}`)
}

export function reorderDocSections(order: number[]) {
  return apiSend('post', `${BASE}/sections/reorder`, { order })
}

export function fetchDocMedia() {
  return apiGet<DocMediaList>(`${BASE}/media`)
}

export async function uploadDocMedia(file: File) {
  const form = new FormData()
  form.append('file', file)
  try {
    const res = await api.post(`${BASE}/media/upload`, form, {
      transformRequest: [
        (data, headers) => {
          if (data instanceof FormData) delete headers['Content-Type']
          return data
        },
      ],
    })
    const payload = res.data as {
      Success?: boolean
      success?: boolean
      Message?: string | null
      message?: string | null
      Data?: { location: string; media: DocMediaItem }
      data?: { location: string; media: DocMediaItem }
    }
    const success = payload.Success ?? payload.success
    const message = payload.Message ?? payload.message
    const data = payload.Data ?? payload.data
    if (!success || !data) throw new ApiError(message || 'Upload failed', res.status)
    return { message, data }
  } catch (e) {
    if (e instanceof ApiError) throw e
    throw new ApiError('Upload failed')
  }
}
