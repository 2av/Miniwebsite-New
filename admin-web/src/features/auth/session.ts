import { useSyncExternalStore } from 'react'

export type AdminSession = {
  userId: number
  email: string
  name: string
  accessToken: string
  refreshToken: string
}

const STORAGE_KEY = 'sb_admin_session'
const listeners = new Set<() => void>()

let cached: AdminSession | null = read()

function read(): AdminSession | null {
  try {
    const raw = localStorage.getItem(STORAGE_KEY)
    return raw ? (JSON.parse(raw) as AdminSession) : null
  } catch {
    return null
  }
}

export function getSession(): AdminSession | null {
  return cached
}

export function setSession(session: AdminSession | null) {
  cached = session
  if (session) localStorage.setItem(STORAGE_KEY, JSON.stringify(session))
  else localStorage.removeItem(STORAGE_KEY)
  listeners.forEach((l) => l())
}

function subscribe(listener: () => void) {
  listeners.add(listener)
  const onStorage = (e: StorageEvent) => {
    if (e.key !== STORAGE_KEY) return
    cached = read()
    listener()
  }
  window.addEventListener('storage', onStorage)
  return () => {
    listeners.delete(listener)
    window.removeEventListener('storage', onStorage)
  }
}

export function useSession(): AdminSession | null {
  return useSyncExternalStore(subscribe, getSession)
}
