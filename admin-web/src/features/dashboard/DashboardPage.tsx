import { useQuery } from '@tanstack/react-query'
import {
  BadgeIndianRupee,
  CircleAlert,
  FileClock,
  Globe2,
  Share2,
  Users,
} from 'lucide-react'
import type { ReactNode } from 'react'
import { Link } from 'react-router-dom'
import { fetchAdminDashboard } from '@/features/dashboard/api'
import { Badge, Card } from '@/shared/ui/primitives'

function inr(value: number) {
  return `₹${Math.round(value).toLocaleString('en-IN')}`
}

function statusTone(status: string): 'ok' | 'warn' | 'danger' | 'neutral' {
  if (status === 'Active' || status.startsWith('Paid on ')) return 'ok'
  if (status === 'Pending' || status.startsWith('Expired')) return 'warn'
  if (status === 'Suspended' || status === 'Rejected' || status === 'InActive') return 'danger'
  return 'neutral'
}

export function DashboardPage() {
  const query = useQuery({
    queryKey: ['admin-dashboard'],
    queryFn: fetchAdminDashboard,
  })

  if (query.isLoading) {
    return <p className="text-sm text-slate-500">Loading the business snapshot…</p>
  }

  if (query.isError || !query.data) {
    return <p className="text-sm text-rose-600">{(query.error as Error)?.message || 'Unable to load the dashboard.'}</p>
  }

  const data = query.data

  return (
    <div className="h-full min-h-0 space-y-6 overflow-auto">
      <div>
        <h1 className="font-[family-name:var(--font-display)] text-3xl font-semibold tracking-tight text-slate-900">
          Dashboard
        </h1>
        <p className="mt-1 text-sm text-slate-500">Business snapshot for {data.asOf}. Amounts are in INR.</p>
      </div>

      <section className="grid gap-4 lg:grid-cols-3">
        <PeriodCard
          title="Orders"
          icon={<BadgeIndianRupee size={18} />}
          to="/all-orders"
          today={`${data.today.orders} received · ${inr(data.today.collected)} collected`}
          month={`${data.month.orders} received · ${inr(data.month.collected)} collected`}
        />
        <PeriodCard
          title="DB Profiles"
          icon={<Globe2 size={18} />}
          to="/db-profile-details"
          today={`${data.today.newProfiles} new · ${data.profiles.active} active now`}
          month={`${data.month.newProfiles} new · ${data.profiles.active} active now`}
        />
        <PeriodCard
          title="Partner commission"
          icon={<Share2 size={18} />}
          to="/referral-commissions"
          firstLabel="Still to pay"
          secondLabel="Earned this month"
          today={inr(data.payoutOutstanding)}
          month={inr(data.commissionEarnedThisMonth)}
        />
      </section>

      <section>
        <h2 className="mb-3 text-sm font-semibold tracking-wide text-slate-500 uppercase">Waiting on you</h2>
        <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
          <QueueCard
            to="/referral-commissions"
            label="Partner invoices to approve"
            value={String(data.queue.partnerInvoices)}
            hot={data.queue.partnerInvoices > 0}
          />
          <QueueCard
            to="/referral-commissions"
            label="Eligible commissions pending payout"
            value={`${data.queue.eligiblePayouts} · ${inr(data.queue.eligiblePayoutAmount)}`}
            hot={data.queue.eligiblePayouts > 0}
          />
          <QueueCard
            to="/sb-user-details"
            label="Documents in Pending Review"
            value={String(data.queue.documentsPending)}
            hot={data.queue.documentsPending > 0}
          />
          <QueueCard
            to="/db-profile-details"
            label="Suspended or expiring in 30 days"
            value={`${data.queue.suspendedProfiles} suspended · ${data.queue.expiringProfiles} expiring`}
            hot={data.queue.suspendedProfiles + data.queue.expiringProfiles > 0}
          />
        </div>
      </section>

      <section className="grid gap-4 lg:grid-cols-3">
        <Card className="p-5">
          <div className="mb-4 flex items-center gap-2 text-slate-900">
            <Users size={18} />
            <h2 className="text-lg font-semibold">SB users</h2>
          </div>
          <Split
            rows={[
              ['Active', data.users.active, 'ok'],
              ['InActive', data.users.inActive, 'neutral'],
            ]}
          />
          <FooterLink to="/sb-user-details">Open SB User Details</FooterLink>
        </Card>
        <Card className="p-5">
          <div className="mb-4 flex items-center gap-2 text-slate-900">
            <Globe2 size={18} />
            <h2 className="text-lg font-semibold">DB Profiles</h2>
          </div>
          <Split
            rows={[
              ['Active', data.profiles.active, 'ok'],
              ['Suspended', data.profiles.suspended, 'danger'],
              ['Expired', data.profiles.expired, 'warn'],
            ]}
          />
          <FooterLink to="/db-profile-details">Open DB Profile Details</FooterLink>
        </Card>
        <Card className="p-5">
          <div className="mb-4 flex items-center gap-2 text-slate-900">
            <Share2 size={18} />
            <h2 className="text-lg font-semibold">Sales channel partners</h2>
          </div>
          <ul className="space-y-2">
            {data.partners.map((partner) => (
              <li key={partner.slug}>
                <Link
                  to={`/sales-channel-partner/${partner.slug}`}
                  className="flex items-center justify-between rounded-xl px-2 py-1.5 text-sm hover:bg-slate-50"
                >
                  <span className="text-slate-700">{partner.label}</span>
                  <span className="font-semibold text-slate-900">{partner.count}</span>
                </Link>
              </li>
            ))}
          </ul>
        </Card>
      </section>

      <section className="grid gap-4 xl:grid-cols-2">
        <ActivityCard title="Latest orders" to="/all-orders" action="All orders">
          <ActivityTable
            headers={['Order ID', 'User', 'Date', 'Amount', 'Payment']}
            empty="No orders yet."
            rows={data.recentOrders.map((order) => [
              order.orderId || '—',
              order.user,
              order.when,
              inr(order.amount),
              order.paymentStatus,
            ])}
            statusColumn={4}
          />
        </ActivityCard>
        <ActivityCard title="Latest DB Profiles" to="/db-profile-details" action="All profiles">
          <ActivityTable
            headers={['DB ID', 'Company', 'Created', 'Status']}
            empty="No profiles yet."
            rows={data.recentProfiles.map((profile) => [
              profile.dbId,
              profile.company,
              profile.created,
              profile.status,
            ])}
            statusColumn={3}
          />
        </ActivityCard>
      </section>
    </div>
  )
}

function PeriodCard({
  title,
  icon,
  to,
  today,
  month,
  firstLabel = 'Today',
  secondLabel = 'This month',
}: {
  title: string
  icon: ReactNode
  to: string
  today: string
  month: string
  firstLabel?: string
  secondLabel?: string
}) {
  return (
    <Card className="p-5">
      <div className="flex items-center justify-between">
        <div className="inline-flex rounded-xl bg-rose-50 p-2 text-rose-600">{icon}</div>
        <Link to={to} className="text-sm font-semibold text-rose-600 hover:text-rose-500">
          Open
        </Link>
      </div>
      <h2 className="mt-4 text-lg font-semibold text-slate-900">{title}</h2>
      <p className="mt-3 text-xs font-semibold tracking-wide text-slate-400 uppercase">{firstLabel}</p>
      <p className="mt-1 text-sm font-medium text-slate-800">{today}</p>
      <p className="mt-3 text-xs font-semibold tracking-wide text-slate-400 uppercase">{secondLabel}</p>
      <p className="mt-1 text-sm font-medium text-slate-800">{month}</p>
    </Card>
  )
}

function QueueCard({ to, label, value, hot }: { to: string; label: string; value: string; hot: boolean }) {
  return (
    <Link to={to}>
      <Card className={`h-full p-4 transition hover:border-rose-200 ${hot ? 'ring-1 ring-amber-200' : ''}`}>
        <div className="flex items-start justify-between gap-3">
          <p className="text-sm text-slate-600">{label}</p>
          {hot ? <CircleAlert size={16} className="shrink-0 text-amber-600" /> : <FileClock size={16} className="shrink-0 text-slate-300" />}
        </div>
        <p className="mt-3 text-lg font-semibold text-slate-900">{value}</p>
      </Card>
    </Link>
  )
}

function Split({ rows }: { rows: Array<[string, number, 'ok' | 'warn' | 'danger' | 'neutral']> }) {
  return (
    <ul className="space-y-2">
      {rows.map(([label, count, tone]) => (
        <li key={label} className="flex items-center justify-between text-sm">
          <Badge tone={tone}>{label}</Badge>
          <span className="font-semibold text-slate-900">{count}</span>
        </li>
      ))}
    </ul>
  )
}

function FooterLink({ to, children }: { to: string; children: string }) {
  return (
    <Link to={to} className="mt-4 inline-flex text-sm font-semibold text-rose-600 hover:text-rose-500">
      {children}
    </Link>
  )
}

function ActivityCard({
  title,
  to,
  action,
  children,
}: {
  title: string
  to: string
  action: string
  children: ReactNode
}) {
  return (
    <Card className="overflow-hidden">
      <div className="flex items-center justify-between border-b border-slate-100 px-5 py-4">
        <h2 className="text-lg font-semibold text-slate-900">{title}</h2>
        <Link to={to} className="text-sm font-semibold text-rose-600 hover:text-rose-500">
          {action}
        </Link>
      </div>
      {children}
    </Card>
  )
}

function ActivityTable({
  headers,
  rows,
  empty,
  statusColumn,
}: {
  headers: string[]
  rows: string[][]
  empty: string
  statusColumn: number
}) {
  if (rows.length === 0) {
    return <p className="px-5 py-6 text-sm text-slate-500">{empty}</p>
  }

  return (
    <div className="overflow-x-auto">
      <table className="w-full text-sm">
        <thead>
          <tr className="text-left text-xs tracking-wide text-slate-400 uppercase">
            {headers.map((header) => (
              <th key={header} className="px-5 py-2 font-semibold">
                {header}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {rows.map((row, index) => (
            <tr key={`${row[0]}-${index}`} className="border-t border-slate-100">
              {row.map((cell, cellIndex) => (
                <td key={cellIndex} className="px-5 py-3 whitespace-nowrap text-slate-700">
                  {cellIndex === statusColumn ? <Badge tone={statusTone(cell)}>{cell}</Badge> : cell}
                </td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
