import { clsx, type ClassValue } from 'clsx'
import { twMerge } from 'tailwind-merge'

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs))
}

/** Absolute URL for bundled static assets under public/assets (logos, CSS, images). */
export function assetUrl(path: string) {
  const base = (import.meta.env.VITE_ASSETS_BASE_URL || '/assets').replace(/\/$/, '')
  const clean = path.replace(/^\//, '')
  return `${base}/${clean}`
}

/** Same-origin path under Vite public/ (e.g. theme CSS). */
export function publicUrl(path: string) {
  if (/^https?:\/\//i.test(path)) return path
  const clean = path.replace(/^\//, '')
  return `/${clean}`
}

/** Uploaded media (profile photos, etc.) — usually production/CDN, not the SPA. */
export function mediaUrl(path = '') {
  const base = (import.meta.env.VITE_MEDIA_BASE_URL || import.meta.env.VITE_SITE_URL || '')
    .replace(/\/$/, '')
  if (!path) return base || '/'
  const clean = path.replace(/^\//, '')
  if (!base) return `/${clean}`
  return `${base}/${clean}`
}

/** Absolute URL on the public MiniWebsite origin (legacy PHP deep links). */
export function siteUrl(path = '') {
  const base = (import.meta.env.VITE_SITE_URL || '').replace(/\/$/, '')
  if (!path) return base || '/'
  return `${base}/${path.replace(/^\//, '')}`
}
