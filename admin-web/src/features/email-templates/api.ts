import type {
  EmailTemplateMeta,
  EmailTemplateRow,
  EmailTemplatesPage,
  PreviewEmailTemplatePayload,
  PreviewEmailTemplateResult,
  UpsertEmailTemplatePayload,
} from '@/shared/types/api'
import { apiGet, apiSend } from '@/shared/api/client'

const BASE = '/api/v1/admin/email-templates'

export function fetchEmailTemplates(query: {
  page?: number
  pageSize?: number
  search?: string
  category?: string
  status?: string
}) {
  return apiGet<EmailTemplatesPage & {
    Templates?: EmailTemplateRow[]
    TotalCount?: number
    Page?: number
    PageSize?: number
  }>(BASE, query as Record<string, unknown>).then((raw) => ({
    templates: raw.templates ?? raw.Templates ?? [],
    totalCount: raw.totalCount ?? raw.TotalCount ?? 0,
    page: raw.page ?? raw.Page ?? query.page ?? 1,
    pageSize: raw.pageSize ?? raw.PageSize ?? query.pageSize ?? 10,
  }))
}

export function fetchEmailTemplatesMeta() {
  return apiGet<EmailTemplateMeta>(`${BASE}/meta`)
}

export function createEmailTemplate(payload: UpsertEmailTemplatePayload) {
  return apiSend<EmailTemplateRow>('post', BASE, payload)
}

export function updateEmailTemplate(id: number, payload: UpsertEmailTemplatePayload) {
  return apiSend<EmailTemplateRow>('put', `${BASE}/${id}`, payload)
}

export function toggleEmailTemplateStatus(id: number) {
  return apiSend('patch', `${BASE}/${id}/status`)
}

export function deleteEmailTemplate(id: number) {
  return apiSend('delete', `${BASE}/${id}`)
}

export function previewEmailTemplate(payload: PreviewEmailTemplatePayload) {
  return apiSend<PreviewEmailTemplateResult>('post', `${BASE}/preview`, payload).then((res) => {
    const data = res.data as PreviewEmailTemplateResult & { Subject?: string; BodyHtml?: string } | undefined
    if (!data) throw new Error('Preview returned empty data')
    return {
      subject: data.subject ?? data.Subject ?? '',
      bodyHtml: data.bodyHtml ?? data.BodyHtml ?? '',
    }
  })
}
