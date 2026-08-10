import type {
  CategoryImportResult,
  ManageCategoriesMeta,
  ManageCategoriesPage,
  ManageCategoryRow,
  UpsertCategoryPayload,
} from '@/shared/types/api'
import { api, apiGet, apiSend, ApiError } from '@/shared/api/client'

const BASE = '/api/v1/admin/manage-categories'

export function fetchManageCategories(query: {
  page?: number
  pageSize?: number
  search?: string
  active?: string
}) {
  return apiGet<ManageCategoriesPage>(BASE, query as Record<string, unknown>)
}

export function fetchManageCategoriesMeta() {
  return apiGet<ManageCategoriesMeta>(`${BASE}/meta`)
}

export function createCategory(payload: UpsertCategoryPayload) {
  return apiSend<ManageCategoryRow>('post', BASE, payload)
}

export function updateCategory(id: number, payload: UpsertCategoryPayload) {
  return apiSend<ManageCategoryRow>('put', `${BASE}/${id}`, payload)
}

export function toggleCategoryActive(id: number) {
  return apiSend<ManageCategoryRow>('post', `${BASE}/${id}/toggle-active`)
}

export function deleteCategory(id: number) {
  return apiSend('delete', `${BASE}/${id}`)
}

export async function exportCategoriesCsv() {
  try {
    const res = await api.get(`${BASE}/export`, { responseType: 'blob' })
    const disposition = res.headers['content-disposition'] as string | undefined
    let fileName = `categories_${new Date().toISOString().slice(0, 19).replace(/[:T]/g, '-')}.csv`
    const match = disposition?.match(/filename="?([^"]+)"?/i)
    if (match?.[1]) fileName = match[1]

    const url = URL.createObjectURL(res.data as Blob)
    const a = document.createElement('a')
    a.href = url
    a.download = fileName
    document.body.appendChild(a)
    a.click()
    a.remove()
    URL.revokeObjectURL(url)
  } catch (e) {
    throw e instanceof ApiError ? e : new ApiError('Export failed')
  }
}

export async function importCategoriesCsv(params: {
  file: File
  replaceAll?: boolean
  skipDuplicates?: boolean
  createdBy?: string
}) {
  const form = new FormData()
  form.append('file', params.file)
  form.append('replaceAll', String(Boolean(params.replaceAll)))
  form.append('skipDuplicates', String(Boolean(params.skipDuplicates)))
  if (params.createdBy) form.append('createdBy', params.createdBy)

  try {
    const res = await api.post(`${BASE}/import`, form, {
      headers: { 'Content-Type': 'multipart/form-data' },
      transformRequest: [
        (data, headers) => {
          if (data instanceof FormData) {
            delete headers['Content-Type']
          }
          return data
        },
      ],
    })
    const payload = res.data as {
      Success?: boolean
      success?: boolean
      Message?: string | null
      message?: string | null
      Data?: CategoryImportResult
      data?: CategoryImportResult
      Errors?: Record<string, string[]>
      errors?: Record<string, string[]>
    }
    const success = payload.Success ?? payload.success
    const message = payload.Message ?? payload.message
    const data = payload.Data ?? payload.data
    const errors = payload.Errors ?? payload.errors
    if (!success) throw new ApiError(message || 'Import failed', res.status, errors)
    return { message, data }
  } catch (e) {
    if (e instanceof ApiError) throw e
    const ax = e as { response?: { data?: { Message?: string; message?: string }; status?: number } }
    throw new ApiError(
      ax.response?.data?.Message ?? ax.response?.data?.message ?? 'Import failed',
      ax.response?.status,
    )
  }
}
