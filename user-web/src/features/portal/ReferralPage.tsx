import { useEffect, useState } from 'react'
import { apiGet } from '@/shared/api/client'
import { useAuthSession } from '@/shared/auth/useAuthSession'
import { siteUrl } from '@/lib/utils'
import { TablePagination, useClientPagination } from '@/components/table'
import { PortalPageHeader } from '@/components/layout/PortalPageHeader'

type ReferredUser = {
  userId?: number | null
  referredUserId: string
  cardId?: number | null
  dbId: string
  dbIdIsLink: boolean
  email?: string | null
  name?: string | null
  phone?: string | null
  joinedOnDisplay: string
  dateCreatedDisplay: string
  expiryDateDisplay: string
  dbProfileStatus: string
  dbProfileStatusClass: string
  orderDescription: string
  commissionStatus: string
  commissionStatusClass: string
  commissionAmount: number
  totalPayableAmount: number
  partnerInvoiceId?: number | null
  partnerInvoiceApproval: string
  partnerInvoiceApprovalClass: string
  sbPayoutStatus: string
  sbPayoutStatusClass: string
}

type ReferralData = {
  pageTitle: string
  totalEarning: number
  referralCount: number
  referredUsers: ReferredUser[]
}

const COLUMNS = [
  'Referred User ID',
  'DB ID',
  'User Email',
  'User Name',
  'User Number',
  'Joined On',
  'Date Created',
  'Expiry Date',
  'DB Profile Status',
  'Order Description',
  'Referrer Commission Status',
  'Commission Amt.',
  'Total Payable Amount',
  'Partner Invoice Details',
  'Partner Invoice Admin Approval',
  'SB Payout Status',
]

function pick(o: Record<string, unknown>, camel: string, pascal: string) {
  return o[camel] ?? o[pascal]
}

function text(value: unknown, fallback = '') {
  const raw = value == null ? '' : String(value)
  return raw.length > 0 ? raw : fallback
}

function normalizeUser(raw: unknown): ReferredUser {
  const r = (raw ?? {}) as Record<string, unknown>
  const invoiceId = Number(pick(r, 'partnerInvoiceId', 'PartnerInvoiceId') ?? 0)
  return {
    userId: (pick(r, 'userId', 'UserId') as number | null) ?? null,
    referredUserId: text(pick(r, 'referredUserId', 'ReferredUserId'), 'N/A'),
    cardId: (pick(r, 'cardId', 'CardId') as number | null) ?? null,
    dbId: text(pick(r, 'dbId', 'DbId'), 'N/A'),
    dbIdIsLink: Boolean(pick(r, 'dbIdIsLink', 'DbIdIsLink')),
    email: (pick(r, 'email', 'Email') as string | null) ?? null,
    name: (pick(r, 'name', 'Name') as string | null) ?? null,
    phone: (pick(r, 'phone', 'Phone') as string | null) ?? null,
    joinedOnDisplay: text(pick(r, 'joinedOnDisplay', 'JoinedOnDisplay'), '-'),
    dateCreatedDisplay: text(pick(r, 'dateCreatedDisplay', 'DateCreatedDisplay'), '-'),
    expiryDateDisplay: text(pick(r, 'expiryDateDisplay', 'ExpiryDateDisplay'), '-'),
    dbProfileStatus: text(pick(r, 'dbProfileStatus', 'DbProfileStatus'), '-'),
    dbProfileStatusClass: text(pick(r, 'dbProfileStatusClass', 'DbProfileStatusClass')),
    orderDescription: text(pick(r, 'orderDescription', 'OrderDescription'), '-'),
    commissionStatus: text(pick(r, 'commissionStatus', 'CommissionStatus'), 'Not Eligible'),
    commissionStatusClass: text(pick(r, 'commissionStatusClass', 'CommissionStatusClass'), 'not-eligible'),
    commissionAmount: Number(pick(r, 'commissionAmount', 'CommissionAmount') ?? 0),
    totalPayableAmount: Number(pick(r, 'totalPayableAmount', 'TotalPayableAmount') ?? 0),
    partnerInvoiceId: invoiceId > 0 ? invoiceId : null,
    partnerInvoiceApproval: text(pick(r, 'partnerInvoiceApproval', 'PartnerInvoiceApproval'), 'Pending'),
    partnerInvoiceApprovalClass: text(pick(r, 'partnerInvoiceApprovalClass', 'PartnerInvoiceApprovalClass'), 'bg-pending'),
    sbPayoutStatus: text(pick(r, 'sbPayoutStatus', 'SbPayoutStatus'), 'Pending'),
    sbPayoutStatusClass: text(pick(r, 'sbPayoutStatusClass', 'SbPayoutStatusClass'), 'bg-pending'),
  }
}

function normalize(raw: unknown): ReferralData {
  const o = (raw ?? {}) as Record<string, unknown>
  const users = ((pick(o, 'referredUsers', 'ReferredUsers') as unknown[]) || []) as unknown[]
  return {
    pageTitle: text(pick(o, 'pageTitle', 'PageTitle'), 'Referral Details'),
    totalEarning: Number(pick(o, 'totalEarning', 'TotalEarning') ?? 0),
    referralCount: Number(pick(o, 'referralCount', 'ReferralCount') ?? 0),
    referredUsers: users.map(normalizeUser),
  }
}

function formatInr(n: number) {
  return Math.round(n).toLocaleString('en-IN')
}

function Status({ label, className }: { label: string; className: string }) {
  if (!label || label === '-') return <>-</>
  return <span className={className}>{label}</span>
}

export function ReferralPage({ title }: { title?: string } = {}) {
  const session = useAuthSession()
  const [data, setData] = useState<ReferralData | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (!session?.email) return
    let cancelled = false
    ;(async () => {
      try {
        setError(null)
        const raw = await apiGet<unknown>('/api/v1/portal/referral', { email: session.email })
        if (!cancelled) setData(normalize(raw))
      } catch (e) {
        if (!cancelled) setError(e instanceof Error ? e.message : 'Failed to load')
      }
    })()
    return () => {
      cancelled = true
    }
  }, [session?.email])

  const pageTitle = title || data?.pageTitle || 'Referral Details'
  const referredUsers = data?.referredUsers ?? []
  const pager = useClientPagination(referredUsers.length, { resetKey: session?.email ?? '' })
  const pagedUsers = pager.slice(referredUsers)

  return (
    <>
      <PortalPageHeader title={pageTitle} />

      {error ? (
        <div className="mw-alert mw-alert-danger" role="alert">
          <i className="fa fa-exclamation-circle mw-alert-icon" aria-hidden />
          <div className="mw-alert-body">{error}</div>
        </div>
      ) : null}

      {data ? (
        <div className="mw-card mb-4">
          <div className="mw-card-body">
            <div className="ReferralDetails-head">
              <div className="mw-dash-card-grid">
                <div className="card_area">
                  <div className="card">
                    <div className="img mw-dash-card-icon mw-dash-card-icon--blue" aria-hidden>
                      <i className="fa-solid fa-users" />
                    </div>
                    <div className="content">
                      <p>Total Referred</p>
                      <h4>{data.referralCount}</h4>
                    </div>
                  </div>
                </div>
                <div className="card_area">
                  <div className="card">
                    <div className="img mw-dash-card-icon mw-dash-card-icon--blue" aria-hidden>
                      <i className="fa-solid fa-indian-rupee-sign" />
                    </div>
                    <div className="content">
                      <p>Total Commission Earning</p>
                      <h4>
                        <i className="fa fa-inr" aria-hidden /> {formatInr(data.totalEarning)}/-
                      </h4>
                    </div>
                  </div>
                </div>
              </div>
            </div>

            <section className="pt-6">
              <h2 className="heading mw-section-title mb-4">Referred Users</h2>
              <div className="mw-table-scroll mw-table-scroll-xl">
                <table id="ReferredUsers" className="display table w-full mb-0">
                  <thead>
                    <tr>
                      {COLUMNS.map((heading) => (
                        <th className="text-center whitespace-nowrap" key={heading}>
                          {heading}
                        </th>
                      ))}
                    </tr>
                  </thead>
                  <tbody>
                    {referredUsers.length === 0 ? (
                      <tr>
                        <td colSpan={COLUMNS.length} className="text-center">
                          No referrals found
                        </td>
                      </tr>
                    ) : (
                      pagedUsers.map((row, idx) => (
                        <tr key={`${row.email ?? 'u'}-${row.cardId ?? idx}-${idx}`}>
                          <td className="text-center whitespace-nowrap">{row.referredUserId}</td>
                          <td className="text-center whitespace-nowrap">
                            {row.dbIdIsLink && row.cardId ? (
                              <a
                                href={siteUrl(`n.php?n=${row.cardId}`)}
                                target="_blank"
                                rel="noopener noreferrer"
                                className="mw-table-cell-inline text-primary hover:text-primary-dark !no-underline"
                              >
                                <i className="fa fa-eye" aria-hidden /> {row.dbId}
                              </a>
                            ) : (
                              row.dbId
                            )}
                          </td>
                          <td className="text-center">{row.email || 'N/A'}</td>
                          <td className="text-center">{row.name || 'Unknown'}</td>
                          <td className="text-center whitespace-nowrap">{row.phone || 'N/A'}</td>
                          <td className="text-center whitespace-nowrap">{row.joinedOnDisplay}</td>
                          <td className="text-center whitespace-nowrap">{row.dateCreatedDisplay}</td>
                          <td className="text-center whitespace-nowrap">{row.expiryDateDisplay}</td>
                          <td className="text-center">
                            <Status label={row.dbProfileStatus} className={row.dbProfileStatusClass} />
                          </td>
                          <td className="text-center">{row.orderDescription}</td>
                          <td className="text-center">
                            <Status label={row.commissionStatus} className={row.commissionStatusClass} />
                          </td>
                          <td className="text-center whitespace-nowrap">₹ {formatInr(row.commissionAmount)}</td>
                          <td className="text-center whitespace-nowrap">₹ {formatInr(row.totalPayableAmount)}</td>
                          <td className="text-center">
                            {row.partnerInvoiceId && session?.email ? (
                              <a
                                className="btn btn-sm btn-primary"
                                href={`/api/v1/portal/invoices/${row.partnerInvoiceId}/html?email=${encodeURIComponent(session.email)}`}
                                target="_blank"
                                rel="noopener noreferrer"
                              >
                                View
                              </a>
                            ) : (
                              '-'
                            )}
                          </td>
                          <td className="text-center">
                            <Status label={row.partnerInvoiceApproval} className={row.partnerInvoiceApprovalClass} />
                          </td>
                          <td className="text-center">
                            <Status label={row.sbPayoutStatus} className={row.sbPayoutStatusClass} />
                          </td>
                        </tr>
                      ))
                    )}
                  </tbody>
                </table>
                {pager.showPager ? (
                  <TablePagination
                    total={referredUsers.length}
                    page={pager.page}
                    pageSize={pager.pageSize}
                    onPageChange={pager.setPage}
                    onPageSizeChange={pager.setPageSize}
                  />
                ) : null}
              </div>
            </section>
          </div>
        </div>
      ) : !error ? (
        <p className="mw-helper-text">Loading…</p>
      ) : null}
    </>
  )
}
