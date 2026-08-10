import type {
  ManageTeamRow,
  ManageTeamsPage,
  TeamReferrals,
  TeamTracker,
} from '@/shared/types/api'
import { api, apiGet, apiSend, ApiError } from '@/shared/api/client'

const BASE = '/api/v1/admin/manage-teams'

export function fetchManageTeams(query: {
  page?: number
  pageSize?: number
  search?: string
  status?: string
}) {
  return apiGet<ManageTeamsPage>(BASE, query as Record<string, unknown>)
}

export function createTeamMember(payload: {
  name: string
  email: string
  phone?: string
  district?: string
  state?: string
  password: string
}) {
  return apiSend<ManageTeamRow>('post', BASE, payload)
}

export function updateTeamMember(
  id: number,
  payload: {
    name: string
    email: string
    phone?: string
    district?: string
    state?: string
  },
) {
  return apiSend<ManageTeamRow>('put', `${BASE}/${id}`, payload)
}

export function toggleTeamStatus(id: number, newStatus?: string) {
  return apiSend<ManageTeamRow>('post', `${BASE}/${id}/toggle-status`, { newStatus })
}

export function resetTeamPassword(id: number, newPassword: string) {
  return apiSend('post', `${BASE}/${id}/reset-password`, { newPassword })
}

export function fetchTeamReferrals(id: number) {
  return apiGet<TeamReferrals>(`${BASE}/${id}/referrals`)
}

export function fetchTeamTracker(id: number) {
  return apiGet<TeamTracker>(`${BASE}/${id}/tracker`)
}

export async function exportTeamTrackerCsv(id: number) {
  try {
    const res = await api.get(`${BASE}/${id}/tracker/export`, { responseType: 'blob' })
    const disposition = res.headers['content-disposition'] as string | undefined
    let fileName = `tracker_${id}.csv`
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
  } catch {
    throw new ApiError('Tracker export failed')
  }
}
