import { useEffect } from 'react'
import { assetUrl } from '@/lib/utils'
import portalShellUrl from '@/styles/portal-shell.css?url'

const LOADED = new Set<string>()

function ensureLink(href: string, id: string) {
  if (LOADED.has(id) || document.getElementById(id)) {
    LOADED.add(id)
    return
  }
  const link = document.createElement('link')
  link.id = id
  link.rel = 'stylesheet'
  link.href = href
  document.head.appendChild(link)
  LOADED.add(id)
}

function ensureScript(src: string, id: string): Promise<void> {
  if (document.getElementById(id)) return Promise.resolve()
  return new Promise((resolve, reject) => {
    const s = document.createElement('script')
    s.id = id
    s.src = src
    s.onload = () => resolve()
    s.onerror = () => reject(new Error(`Failed to load ${src}`))
    document.head.appendChild(s)
  })
}

const PORTAL_STYLE_IDS = [
  'mw-portal-bootstrap',
  'mw-portal-datatables',
  'mw-portal-fa4',
  'mw-portal-styles',
  'mw-portal-responsive',
  'mw-portal-dashboard-pro',
  'mw-portal-common',
  'mw-portal-fa7',
  'mw-portal-fa7-v4-face',
  'mw-portal-fa7-v4-shims',
  'mw-portal-inline',
  'mw-portal-shell',
]

function setPortalSheetsDisabled(disabled: boolean) {
  for (const id of PORTAL_STYLE_IDS) {
    const link = document.getElementById(id) as HTMLLinkElement | null
    if (link) link.disabled = disabled
  }
}

/**
 * Same CSS stack as user/includes/header.php (portal pages).
 * Order matches PHP so overrides behave the same.
 * The profile editor passes enabled=false so the public theme is not restyled.
 */
export function usePortalStyles(enabled = true) {
  useEffect(() => {
    if (!enabled) {
      document.body.classList.remove('sb-nav-fixed', 'mw-portal-app', 'sb-sidenav-toggled')
      setPortalSheetsDisabled(true)
      return () => setPortalSheetsDisabled(false)
    }
    let applyPortalTailwind = true
    setPortalSheetsDisabled(false)
    ensureLink(
      'https://fonts.googleapis.com/css2?family=Poppins:wght@400;500;600;700;800&display=swap',
      'mw-portal-font',
    )
    ensureLink(
      'https://cdn.jsdelivr.net/npm/bootstrap@4.6.2/dist/css/bootstrap.min.css',
      'mw-portal-bootstrap',
    )
    ensureLink(
      'https://cdn.jsdelivr.net/npm/simple-datatables@7.1.2/dist/style.min.css',
      'mw-portal-datatables',
    )
    // PHP loads FA4 then FA7 + v4 shims
    ensureLink(
      'https://cdnjs.cloudflare.com/ajax/libs/font-awesome/4.7.0/css/font-awesome.css',
      'mw-portal-fa4',
    )
    ensureLink(assetUrl('css/styles.css'), 'mw-portal-styles')
    ensureLink(assetUrl('css/responsive.css'), 'mw-portal-responsive')
    ensureLink(assetUrl('css/dashboard-professional.css'), 'mw-portal-dashboard-pro')
    ensureLink(assetUrl('css/common.css'), 'mw-portal-common')
    ensureLink(
      'https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css',
      'mw-portal-fa7',
    )
    ensureLink(
      'https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/v4-font-face.min.css',
      'mw-portal-fa7-v4-face',
    )
    ensureLink(
      'https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/v4-shims.min.css',
      'mw-portal-fa7-v4-shims',
    )
    // Same rules as header.php <style id="mw-design-tokens|mw-components">
    ensureLink(assetUrl('css/mw-portal-inline.css'), 'mw-portal-inline')
    // Thin React bridge only (layout wrappers) — do not restyle PHP components
    ensureLink(portalShellUrl, 'mw-portal-shell')

    // PHP header also loads Tailwind CDN (preflight off) for utility classes on nav/profile
    void ensureScript('https://cdn.tailwindcss.com', 'mw-portal-tailwind').then(() => {
      if (!applyPortalTailwind) return
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      const tw = (window as any).tailwind
      if (!tw) return
      tw.config = {
        corePlugins: { preflight: false },
        theme: {
          extend: {
            colors: {
              primary: {
                DEFAULT: 'var(--mw-color-primary)',
                dark: 'var(--mw-color-primary-dark)',
                light: 'var(--mw-color-primary-light)',
              },
              secondary: {
                DEFAULT: 'var(--mw-color-secondary)',
                dark: 'var(--mw-color-secondary-dark)',
                light: 'var(--mw-color-secondary-light)',
              },
              accent: 'var(--mw-color-accent)',
              background: 'var(--mw-color-bg)',
              surface: 'var(--mw-color-surface)',
              foreground: 'var(--mw-color-text)',
              muted: 'var(--mw-color-border)',
              'muted-foreground': 'var(--mw-color-text-muted)',
              border: 'var(--mw-color-border)',
            },
            fontFamily: {
              sans: ['Poppins', 'system-ui', 'sans-serif'],
              display: ['Poppins', 'system-ui', 'sans-serif'],
            },
            boxShadow: {
              soft: 'var(--mw-shadow-soft)',
              card: 'var(--mw-shadow-card)',
              elevated: 'var(--mw-shadow-elevated)',
            },
          },
        },
      }
    })

    document.body.classList.add('sb-nav-fixed', 'mw-portal-app')
    return () => {
      applyPortalTailwind = false
      document.body.classList.remove('sb-nav-fixed', 'mw-portal-app', 'sb-sidenav-toggled')
    }
  }, [enabled])
}
