import { Outlet } from 'react-router-dom'
import { PublicFooter } from '@/components/layout/PublicFooter'
import { PublicHeader } from '@/components/layout/PublicHeader'
import { useSiteStyles } from '@/shared/site/useSiteStyles'

/** Marketing / public pages — shared header + footer chrome. */
export function PublicLayout() {
  useSiteStyles()
  return (
    <div className="flex min-h-dvh flex-col bg-white">
      <PublicHeader />
      <main className="flex-1">
        <Outlet />
      </main>
      <PublicFooter />
    </div>
  )
}
