import type {
  ManageContentItem,
  ManageContentList,
  ManageContentMeta,
  UpsertContentPayload,
} from '@/shared/types/api'
import { apiGet, apiSend } from '@/shared/api/client'

const BASE = '/api/v1/admin/manage-content'

export function fetchManageContentMeta() {
  return apiGet<ManageContentMeta>(`${BASE}/meta`)
}

export function fetchManageContent() {
  return apiGet<ManageContentList>(BASE)
}

export function fetchManageContentByType(contentType: string) {
  return apiGet<ManageContentItem>(`${BASE}/${encodeURIComponent(contentType)}`)
}

export function upsertManageContent(payload: UpsertContentPayload) {
  return apiSend<ManageContentItem>('put', BASE, payload)
}
