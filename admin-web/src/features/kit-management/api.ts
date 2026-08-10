import type {
  CreateKitFolderPayload,
  KitExplorer,
  KitFolderTile,
  KitItem,
  KitManagementMeta,
  MoveKitItemPayload,
  UpdateKitFolderPayload,
  UpdateKitVideoPayload,
} from '@/shared/types/api'
import { api, apiGet, apiSend, ApiError } from '@/shared/api/client'

const BASE = '/api/v1/admin/kit-management'

export function fetchKitMeta() {
  return apiGet<KitManagementMeta>(`${BASE}/meta`)
}

export function fetchKitExplorer(category: string, folderId = 0) {
  return apiGet<KitExplorer>(`${BASE}/explorer`, { category, folderId })
}

export function createKitFolder(payload: CreateKitFolderPayload) {
  return apiSend<KitFolderTile>('post', `${BASE}/folders`, payload)
}

export function updateKitFolder(id: number, payload: UpdateKitFolderPayload) {
  return apiSend<KitFolderTile>('put', `${BASE}/folders/${id}`, payload)
}

export function deleteKitFolder(id: number, category: string) {
  return deleteKitFolderQuery(id, category)
}

export async function deleteKitFolderQuery(id: number, category: string) {
  try {
    const res = await api.delete(`${BASE}/folders/${id}`, { params: { category } })
    const payload = res.data as { success?: boolean; Success?: boolean; message?: string; Message?: string }
    const success = payload.success ?? payload.Success
    const message = payload.message ?? payload.Message
    if (!success) throw new ApiError(message || 'Delete failed', res.status)
    return { message }
  } catch (e) {
    throw e instanceof ApiError ? e : new ApiError('Delete failed')
  }
}

export async function uploadKitImage(form: FormData) {
  return uploadForm(`${BASE}/items/image`, form)
}

export async function uploadKitVideoFile(form: FormData) {
  return uploadForm(`${BASE}/items/video-file`, form)
}

export async function uploadKitFile(form: FormData) {
  return uploadForm(`${BASE}/items/file`, form)
}

export function addKitVideoUrl(payload: {
  category: string
  title: string
  videoUrl: string
  folderId?: number | null
  displayOrder: number
}) {
  return apiSend<KitItem>('post', `${BASE}/items/video-url`, payload)
}

export async function updateKitImage(id: number, form: FormData) {
  return uploadForm(`${BASE}/items/${id}/image`, form, 'put')
}

export async function updateKitFile(id: number, form: FormData) {
  return uploadForm(`${BASE}/items/${id}/file`, form, 'put')
}

export function updateKitVideo(id: number, payload: UpdateKitVideoPayload) {
  return apiSend<KitItem>('put', `${BASE}/items/${id}/video`, payload)
}

export function toggleKitItemStatus(id: number, status: string) {
  return apiSend('patch', `${BASE}/items/${id}/status`, { status })
}

export function moveKitItem(id: number, payload: MoveKitItemPayload) {
  return apiSend('patch', `${BASE}/items/${id}/move`, payload)
}

export function deleteKitItem(id: number) {
  return apiSend('delete', `${BASE}/items/${id}`)
}

async function uploadForm(url: string, form: FormData, method: 'post' | 'put' = 'post') {
  try {
    const res = await api.request({
      method,
      url,
      data: form,
      transformRequest: [
        (data, headers) => {
          if (data instanceof FormData) delete headers['Content-Type']
          return data
        },
      ],
    })
    const payload = res.data as {
      success?: boolean
      Success?: boolean
      message?: string | null
      Message?: string | null
      data?: KitItem
      Data?: KitItem
    }
    const success = payload.success ?? payload.Success
    const message = payload.message ?? payload.Message
    const data = payload.data ?? payload.Data
    if (!success) throw new ApiError(message || 'Upload failed', res.status)
    return { message, data }
  } catch (e) {
    if (e instanceof ApiError) throw e
    throw new ApiError('Upload failed')
  }
}
