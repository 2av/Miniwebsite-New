import { Card } from '@/shared/ui/primitives'
import { Link } from 'react-router-dom'
import { Users, ArrowRight } from 'lucide-react'

export function DashboardPage() {
  return (
    <div className="h-full min-h-0 space-y-6 overflow-auto">
      <div>
        <h1 className="font-[family-name:var(--font-display)] text-3xl font-semibold tracking-tight text-slate-900">
          Welcome back
        </h1>
        <p className="mt-1 text-slate-500">Manage MiniWebsite from the React admin. Start with users.</p>
      </div>

      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
        <Card className="p-5">
          <div className="flex items-start justify-between">
            <div>
              <div className="inline-flex rounded-xl bg-rose-50 p-2 text-rose-600">
                <Users size={20} />
              </div>
              <h2 className="mt-4 text-lg font-semibold text-slate-900">Manage Users</h2>
              <p className="mt-1 text-sm text-slate-500">
                Filters, deals, Sales Kit, collaboration, refunds, and password resets — live via .NET API.
              </p>
            </div>
          </div>
          <Link
            to="/user-details"
            className="mt-5 inline-flex items-center gap-2 text-sm font-semibold text-rose-600 hover:text-rose-500"
          >
            Open module <ArrowRight size={16} />
          </Link>
        </Card>

        <Card className="p-5 opacity-70">
          <h2 className="text-lg font-semibold text-slate-900">Payments</h2>
          <p className="mt-1 text-sm text-slate-500">Placeholder — migrate next from PHP admin.</p>
        </Card>

        <Card className="p-5 opacity-70">
          <h2 className="text-lg font-semibold text-slate-900">Deals</h2>
          <p className="mt-1 text-sm text-slate-500">Placeholder — migrate when APIs are ready.</p>
        </Card>
      </div>
    </div>
  )
}
