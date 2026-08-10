import { useEffect } from 'react'
import { assetUrl } from '@/lib/utils'

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
    document.body.appendChild(s)
  })
}

/** Injects shared site CSS (bootstrap + layout + responsive) for visual parity. */
export function useSiteStyles(options?: { owl?: boolean }) {
  useEffect(() => {
    ensureLink(assetUrl('css/bootstrap.min.css'), 'mw-site-bootstrap')
    ensureLink(assetUrl('css/layout.css'), 'mw-site-layout')
    ensureLink(assetUrl('css/responsive.css'), 'mw-site-responsive')
    if (options?.owl) {
      ensureLink(assetUrl('css/owl.carousel.min.css'), 'mw-site-owl')
      ensureLink(assetUrl('css/owl.theme.default.css'), 'mw-site-owl-theme')
    }
  }, [options?.owl])
}

/** Loads jQuery + Owl for home carousels. */
export async function initOwlCarousels() {
  try {
    await ensureScript(assetUrl('js/jquery.slim.min.js'), 'mw-jquery')
    await ensureScript(assetUrl('js/owl.carousel.min.js'), 'mw-owl')
    const $ = (window as unknown as { jQuery?: (sel: string) => { owlCarousel: (o: object) => void } }).jQuery
    if (!$) return
    $('.screenshots-carousel').owlCarousel({
      loop: true,
      margin: 20,
      nav: true,
      dots: false,
      responsive: { 0: { items: 1 }, 576: { items: 2 }, 992: { items: 3 } },
    })
    $('.testimonial-carousel').owlCarousel({
      loop: true,
      margin: 20,
      nav: false,
      dots: true,
      responsive: { 0: { items: 1 }, 768: { items: 2 }, 992: { items: 3 } },
    })
  } catch {
    // Assets may be unavailable in some envs; page still renders statically.
  }
}
