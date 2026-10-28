import { apiGet } from '@/shared/api/client'

export type AdminFileRow = {
  id: number
  userId: number
  userName: string
  userEmail: string
  folder: string
  document: string
  originalFileName: string
  storedFileName: string
  contentType: string
  sizeBytes: number
  status: string
  uploadedAt: string
  replacedAt?: string | null
  viewUrl?: string | null
}

export type AdminFilesPage = {
  files: AdminFileRow[]
  totalCount: number
  page: number
  pageSize: number
  totalSizeBytes: number
}

export function fetchAdminFiles(query: {
  page?: number
  pageSize?: number
  search?: string
  folder?: string
  status?: string
}) {
  return apiGet<AdminFilesPage>('/api/v1/admin/files', query as Record<string, unknown>)
}
