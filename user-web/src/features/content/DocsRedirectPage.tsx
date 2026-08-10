import { useEffect } from 'react'
import { siteUrl } from '@/lib/utils'

/** Docs help center still served from live site until a public docs API exists. */
export function DocsRedirectPage({ slug }: { slug?: string }) {
  useEffect(() => {
    const path = slug ? `docs/${encodeURIComponent(slug)}` : 'docs/'
    window.location.replace(siteUrl(path))
  }, [slug])

  return (
    <div className="mx-auto max-w-[600px] px-4 py-16 text-center">
      <p>Opening documentation...</p>
    </div>
  )
}
