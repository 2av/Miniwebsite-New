import { Link } from 'react-router-dom'
import { siteUrl } from '@/lib/utils'
import { useSiteStyles } from '@/shared/site/useSiteStyles'

/** Legacy marketing page — kept as a light landing; full Ajooba layout was orphaned. */
export function WebsiteDevelopmentPage() {
  useSiteStyles()

  return (
    <main className="mx-auto max-w-[900px] px-4 py-16 text-center">
      <h1 className="mb-4 text-3xl font-bold text-[var(--mw-navy)]">Website Development</h1>
      <p className="mb-6 text-slate-600">
        Looking for a full website? Start with a Mini Website today, or contact support for custom development.
      </p>
      <div className="flex flex-wrap items-center justify-center gap-3">
        <Link to="/login/customer" className="mw-btn-primary">
          Create Mini Website
        </Link>
        <a href={siteUrl('franchisee.php')} className="rounded-lg border px-4 py-2 text-[var(--mw-navy)]">
          Franchise Partner
        </a>
      </div>
    </main>
  )
}
