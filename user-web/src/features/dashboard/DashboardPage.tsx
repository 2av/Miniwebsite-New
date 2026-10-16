import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { apiGet } from '@/shared/api/client'
import { useAuthSession } from '@/shared/auth/useAuthSession'
import { dashboardTitleForRole } from '@/shared/auth/session'
import { assetUrl, siteUrl } from '@/lib/utils'
import { TablePagination, useClientPagination } from '@/components/table'

type DigiCardListItem = {
  id: number
  cardSlug?: string | null
  companyName?: string | null
  paymentStatus?: string | null
  cardStatus?: string | null
  uploadedDate?: string | null
  paymentDate?: string | null
  validityDate?: string | null
  validityDisplay: string
  publicUrl: string
}

type CustomerDashboard = {
  email: string
  role: string
  referralCode: string
  refundStatus?: string | null
  refundStatusDate?: string | null
  showInvoiceColumn: boolean
  mwReferralId?: number | null
  influencer: string
  cards: DigiCardListItem[]
}

type FranchiseeManagedUser = {
  userId: number
  name?: string | null
  phone?: string | null
  email?: string | null
  userCreatedAt?: string | null
  referralCode?: string | null
  referredBy?: string | null
  cardDbId?: number | null
  companyName?: string | null
  paymentStatus?: string | null
  cardCreatedDate?: string | null
  paymentDate?: string | null
  validityDate?: string | null
  validityDisplay: string
}

type FranchiseeDashboard = {
  email: string
  role: string
  totalCardsCreated: number
  managedUsers: FranchiseeManagedUser[]
}

function fmtDate(value?: string | null) {
  if (!value) return '-'
  const d = new Date(value)
  if (Number.isNaN(d.getTime())) return '-'
  const dd = String(d.getDate()).padStart(2, '0')
  const mm = String(d.getMonth() + 1).padStart(2, '0')
  const yyyy = d.getFullYear()
  return `${dd}-${mm}-${yyyy}`
}

function pick<T>(obj: Record<string, unknown>, camel: string, pascal: string): T {
  return (obj[camel] ?? obj[pascal]) as T
}

function normalizeCustomer(raw: unknown): CustomerDashboard {
  const o = (raw ?? {}) as Record<string, unknown>
  const cardsRaw = (pick(o, 'cards', 'Cards') as unknown[]) || []
  return {
    email: String(pick(o, 'email', 'Email') ?? ''),
    role: String(pick(o, 'role', 'Role') ?? ''),
    referralCode: String(pick(o, 'referralCode', 'ReferralCode') ?? ''),
    refundStatus: pick(o, 'refundStatus', 'RefundStatus') as string | null,
    refundStatusDate: pick(o, 'refundStatusDate', 'RefundStatusDate') as string | null,
    showInvoiceColumn: Boolean(pick(o, 'showInvoiceColumn', 'ShowInvoiceColumn')),
    mwReferralId: pick(o, 'mwReferralId', 'MwReferralId') as number | null,
    influencer: String(pick(o, 'influencer', 'Influencer') ?? 'NO'),
    cards: cardsRaw.map((c) => {
      const r = (c ?? {}) as Record<string, unknown>
      return {
        id: Number(pick(r, 'id', 'Id')),
        cardSlug: pick(r, 'cardSlug', 'CardSlug') as string | null,
        companyName: pick(r, 'companyName', 'CompanyName') as string | null,
        paymentStatus: pick(r, 'paymentStatus', 'PaymentStatus') as string | null,
        cardStatus: pick(r, 'cardStatus', 'CardStatus') as string | null,
        uploadedDate: pick(r, 'uploadedDate', 'UploadedDate') as string | null,
        paymentDate: pick(r, 'paymentDate', 'PaymentDate') as string | null,
        validityDate: pick(r, 'validityDate', 'ValidityDate') as string | null,
        validityDisplay: String(pick(r, 'validityDisplay', 'ValidityDisplay') ?? '-'),
        publicUrl: String(pick(r, 'publicUrl', 'PublicUrl') ?? ''),
      }
    }),
  }
}

function normalizeFranchisee(raw: unknown): FranchiseeDashboard {
  const o = (raw ?? {}) as Record<string, unknown>
  const usersRaw = (pick(o, 'managedUsers', 'ManagedUsers') as unknown[]) || []
  return {
    email: String(pick(o, 'email', 'Email') ?? ''),
    role: String(pick(o, 'role', 'Role') ?? ''),
    totalCardsCreated: Number(pick(o, 'totalCardsCreated', 'TotalCardsCreated') ?? 0),
    managedUsers: usersRaw.map((u) => {
      const r = (u ?? {}) as Record<string, unknown>
      return {
        userId: Number(pick(r, 'userId', 'UserId')),
        name: pick(r, 'name', 'Name') as string | null,
        phone: pick(r, 'phone', 'Phone') as string | null,
        email: pick(r, 'email', 'Email') as string | null,
        userCreatedAt: pick(r, 'userCreatedAt', 'UserCreatedAt') as string | null,
        referralCode: pick(r, 'referralCode', 'ReferralCode') as string | null,
        referredBy: pick(r, 'referredBy', 'ReferredBy') as string | null,
        cardDbId: pick(r, 'cardDbId', 'CardDbId') as number | null,
        companyName: pick(r, 'companyName', 'CompanyName') as string | null,
        paymentStatus: pick(r, 'paymentStatus', 'PaymentStatus') as string | null,
        cardCreatedDate: pick(r, 'cardCreatedDate', 'CardCreatedDate') as string | null,
        paymentDate: pick(r, 'paymentDate', 'PaymentDate') as string | null,
        validityDate: pick(r, 'validityDate', 'ValidityDate') as string | null,
        validityDisplay: String(pick(r, 'validityDisplay', 'ValidityDisplay') ?? '-'),
      }
    }),
  }
}

function mwStatus(paymentStatus?: string | null, cardStatus?: string | null) {
  if (paymentStatus === 'Success') return { className: 'bg-success', text: cardStatus || 'Active' }
  if (paymentStatus === 'Failed') return { className: 'not-eligible', text: 'Inactive' }
  return { className: 'not-eligible', text: 'Pending Payment' }
}

export function DashboardPage() {
  const session = useAuthSession()
  const isFranchisee = session?.role === 'FRANCHISEE'
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [customer, setCustomer] = useState<CustomerDashboard | null>(null)
  const [franchisee, setFranchisee] = useState<FranchiseeDashboard | null>(null)
  const [portalQuery, setPortalQuery] = useState('')
  const [mappedRoleName, setMappedRoleName] = useState(session?.roleName ?? '')

  useEffect(() => {
    const onSearch = (event: Event) => {
      setPortalQuery(String((event as CustomEvent<string>).detail ?? ''))
    }
    window.addEventListener('sb-portal-search', onSearch)
    return () => window.removeEventListener('sb-portal-search', onSearch)
  }, [])

  useEffect(() => {
    if (!session?.email) return
    let cancelled = false
    ;(async () => {
      setLoading(true)
      setError(null)
      try {
        if (isFranchisee) {
          const data = await apiGet<unknown>('/api/v1/dashboard/franchisee', { email: session.email })
          if (!cancelled) setFranchisee(normalizeFranchisee(data))
        } else {
          const data = await apiGet<unknown>('/api/v1/dashboard/customer', { email: session.email })
          if (!cancelled) setCustomer(normalizeCustomer(data))
        }
        try {
          const me = await apiGet<Record<string, unknown>>('/api/v1/portal/me', { email: session.email })
          const roleName = String(me.roleName ?? me.RoleName ?? '').trim()
          if (!cancelled && roleName) setMappedRoleName(roleName)
        } catch {
          // title falls back to the login role
        }
      } catch (e) {
        if (!cancelled) setError(e instanceof Error ? e.message : 'Failed to load dashboard')
      } finally {
        if (!cancelled) setLoading(false)
      }
    })()
    return () => {
      cancelled = true
    }
  }, [session?.email, isFranchisee])

  const managedUsers = franchisee?.managedUsers ?? []
  const cards = customer?.cards ?? []
  const query = portalQuery.trim().toLowerCase()
  const filteredUsers = query
    ? managedUsers.filter((u) =>
        [u.name, u.email, u.phone, u.companyName, String(u.userId), String(u.cardDbId ?? '')]
          .join(' ')
          .toLowerCase()
          .includes(query),
      )
    : managedUsers
  const filteredCards = query
    ? cards.filter((c) =>
        [c.companyName, c.cardSlug, c.paymentStatus, c.cardStatus, String(c.id)].join(' ').toLowerCase().includes(query),
      )
    : cards
  const tableRows = isFranchisee ? filteredUsers : filteredCards
  const pager = useClientPagination(tableRows.length, { resetKey: session?.email ?? '' })
  const pagedUsers = pager.slice(filteredUsers)
  const pagedCards = pager.slice(filteredCards)

  const pageTitle = dashboardTitleForRole(mappedRoleName || session?.roleName, session?.role)
  const showRefundCol = Boolean(customer?.refundStatus && customer.refundStatus !== 'None')

  const activeCount = filteredCards.filter((c) => c.paymentStatus === 'Success').length
  const pendingCount = filteredCards.length - activeCount

  return (
    <div className="sb-dash">
        <div className="sb-dash-head">
          <div>
            <h1 className="sb-dash-title">{pageTitle}</h1>
            <p className="sb-dash-sub">
              {isFranchisee
                ? 'Track the ShareBizz websites you created and managed users.'
                : 'Manage your ShareBizz websites, payments, and sharing from one place.'}
            </p>
          </div>
          <div>
            <nav className="sb-crumbs" aria-label="breadcrumb">
              <Link to="/dashboard">ShareBizz</Link>
              <span>/</span>
              <span className="is-current">{pageTitle}</span>
            </nav>
            {!isFranchisee ? (
              <Link className="sb-btn-primary" to="/website/business-name?new=1">
                <i className="fa fa-plus" aria-hidden /> Create ShareBizz
              </Link>
            ) : null}
          </div>
        </div>

        {loading && <p>Loading dashboard...</p>}
        {error && <div className="alert alert-danger">{error}</div>}

        {!loading && !error && isFranchisee && franchisee && (
          <div className="card mb-4 sb-dash-panel">
            <div className="card-body">
              <div className="sb-stat-grid">
                <div className="sb-stat sb-stat--purple">
                  <i className="fa fa-id-card" aria-hidden />
                  <div>
                    <strong>{franchisee.totalCardsCreated}</strong>
                    <span>Websites created</span>
                  </div>
                </div>
                <div className="sb-stat sb-stat--green">
                  <i className="fa fa-users" aria-hidden />
                  <div>
                    <strong>{filteredUsers.length}</strong>
                    <span>Managed users</span>
                  </div>
                </div>
              </div>
              <div className="FranchiseeDashboard-head">
                <div className="mw-dash-card-grid">
                  <div className="card_area">
                    <Link to="/website/business-name?new=1">
                      <div className="card">
                        <div className="img mw-dash-card-icon mw-dash-card-icon--blue" aria-hidden="true">
                          <i className="fa-solid fa-pen-to-square" />
                        </div>
                        <div className="content">
                          <p>
                            Create New
                            <br />
                            ShareBizz
                          </p>
                        </div>
                      </div>
                    </Link>
                  </div>
                  <div className="card_area">
                    <div className="card">
                      <div className="img mw-dash-card-icon mw-dash-card-icon--blue" aria-hidden="true">
                        <i className="fa-solid fa-circle-check" />
                      </div>
                      <div className="content">
                        <p>MW Created</p>
                        <h4 className="marginbottom5">{franchisee.totalCardsCreated}</h4>
                      </div>
                    </div>
                  </div>
                </div>
              </div>

              <div className="ManageUsers">
                <h4 className="heading">Manage Users: </h4>
                <div className="table-responsive mw-table-scroll mw-table-scroll-wide mw-dashboard-table-wrap">
                  <table id="ReferredUsers" className="display table mb-0" style={{ textAlign: 'center' }}>
                    <thead className="bg-secondary">
                      <tr>
                        <th className="text-left">User ID</th>
                        <th className="text-left">MW ID</th>
                        <th className="text-left">User Email</th>
                        <th className="text-left">User Name</th>
                        <th className="text-left">User Number</th>
                        <th className="text-left">Date Created</th>
                        <th className="text-left">Validity Date</th>
                        <th className="text-left">MW Status</th>
                      </tr>
                    </thead>
                    <tbody>
                      {filteredUsers.length === 0 ? (
                        <tr>
                          <td colSpan={8} className="text-center">
                            No users found.
                          </td>
                        </tr>
                      ) : (
                        pagedUsers.map((u) => {
                          const st = mwStatus(u.paymentStatus)
                          return (
                            <tr key={`${u.userId}-${u.cardDbId ?? 0}`}>
                              <td className="text-left">{u.userId}</td>
                              <td className="text-left">{u.cardDbId ?? '-'}</td>
                              <td className="text-left">{u.email}</td>
                              <td className="text-left">{u.name}</td>
                              <td className="text-left">{u.phone}</td>
                              <td className="text-left">{fmtDate(u.userCreatedAt || u.cardCreatedDate)}</td>
                              <td className="text-left">{u.validityDisplay || fmtDate(u.validityDate)}</td>
                              <td className="text-left">
                                <span className={st.className}>{st.text}</span>
                              </td>
                            </tr>
                          )
                        })
                      )}
                    </tbody>
                  </table>
                </div>
                {pager.showPager ? (
                  <TablePagination
                    total={filteredUsers.length}
                    page={pager.page}
                    pageSize={pager.pageSize}
                    onPageChange={pager.setPage}
                    onPageSizeChange={pager.setPageSize}
                  />
                ) : null}
              </div>
            </div>
          </div>
        )}

        {!loading && !error && !isFranchisee && customer && (
          <div className="card mb-4 sb-dash-panel">
            <div className="card-body">
              <div className="sb-stat-grid">
                <div className="sb-stat sb-stat--purple">
                  <i className="fa fa-th-large" aria-hidden />
                  <div>
                    <strong>{filteredCards.length}</strong>
                    <span>Total websites</span>
                  </div>
                </div>
                <div className="sb-stat sb-stat--green">
                  <i className="fa fa-check-circle" aria-hidden />
                  <div>
                    <strong>{activeCount}</strong>
                    <span>Active</span>
                  </div>
                </div>
                <div className="sb-stat sb-stat--orange">
                  <i className="fa fa-clock-o" aria-hidden />
                  <div>
                    <strong>{pendingCount}</strong>
                    <span>Pending payment</span>
                  </div>
                </div>
                <div className="sb-stat sb-stat--pink">
                  <i className="fa fa-share-alt" aria-hidden />
                  <div>
                    <strong>{customer.referralCode ? 1 : 0}</strong>
                    <span>Referral code</span>
                  </div>
                </div>
                <div className="sb-stat sb-stat--blue">
                  <i className="fa fa-file-text-o" aria-hidden />
                  <div>
                    <strong>{customer.showInvoiceColumn ? activeCount : 0}</strong>
                    <span>Invoices</span>
                  </div>
                </div>
              </div>
              <div className="CustomerDashboard-head">
                <div className="mw-dash-card-grid">
                  <div className="card_area top_section">
                    <Link to="/website/business-name?new=1">
                      <div className="card">
                        <div className="img mw-dash-card-icon mw-dash-card-icon--blue" aria-hidden="true">
                          <i className="fa-solid fa-pen-to-square" />
                        </div>
                        <div className="content">
                          <p>
                            Create New
                            <br />
                            ShareBizz
                          </p>
                        </div>
                      </div>
                    </Link>
                  </div>
                </div>
              </div>

              <div className="table-responsive mw-table-scroll mw-dashboard-table-wrap" id="dashboardTableWrap">
                <table id="ReferredUsers" className="display table mb-0" style={{ textAlign: 'center' }}>
                  <thead className="bg-secondary">
                    <tr>
                      <th>MW ID</th>
                      <th>Company Name</th>
                      <th>Date Created</th>
                      <th>Validity Date</th>
                      <th>MW Status</th>
                      <th>View/Edit/Share</th>
                      <th>User Payment Status</th>
                      {customer.showInvoiceColumn && <th>Invoice</th>}
                      {showRefundCol && <th>Refund Status</th>}
                    </tr>
                  </thead>
                  <tbody>
                    {filteredCards.length === 0 ? (
                      <tr>
                        <td colSpan={customer.showInvoiceColumn ? (showRefundCol ? 9 : 8) : showRefundCol ? 8 : 7} className="text-center">
                          No ShareBizz websites found. Create your first one!
                        </td>
                      </tr>
                    ) : (
                      pagedCards.map((card) => {
                        const st = mwStatus(card.paymentStatus, card.cardStatus)
                        const viewUrl = `/n/${encodeURIComponent(card.cardSlug || String(card.id))}`
                        const editUrl = `/website/business-name?card_number=${card.id}`
                        const absShare = `${window.location.origin}${viewUrl}`
                        const shareUrl = `https://api.whatsapp.com/send?text=${encodeURIComponent(absShare)}`
                        const paid = card.paymentStatus === 'Success'
                        return (
                          <tr key={card.id}>
                            <td>{card.id}</td>
                            <td>{card.companyName}</td>
                            <td>{fmtDate(card.uploadedDate)}</td>
                            <td>{card.validityDisplay || fmtDate(card.validityDate)}</td>
                            <td>
                              <span className={st.className}>{st.text}</span>
                            </td>
                            <td>
                              <div className="mw-table-cell-inline">
                                <span className="view">
                                  <a href={viewUrl} target="_blank" rel="noreferrer" style={{ textDecoration: 'none', color: 'inherit' }}>
                                    <span className="view_icon_style">
                                      <i className="fa-regular fa-eye" />
                                    </span>
                                  </a>
                                </span>
                                <span className="edit">
                                  <Link to={editUrl}>
                                    <span className="edit_icon_style">
                                      <i className="fa-solid fa-pen" />
                                    </span>
                                  </Link>
                                </span>
                                <span className="share">
                                  <a href={shareUrl} target="_blank" rel="noreferrer">
                                    <span className="share_icon_style">
                                      <i className="fa-solid fa-share-nodes" />
                                    </span>
                                  </a>
                                </span>
                              </div>
                            </td>
                            <td>
                              {!paid ? (
                                <Link
                                  className="btn btn-primary paynow_btn"
                                  to={`/payment/miniwebsite?id=${card.id}&source=${encodeURIComponent((session?.role || 'customer').toLowerCase())}`}
                                  target="_blank"
                                  rel="noreferrer"
                                >
                                  Pay Now
                                </Link>
                              ) : (
                                <span className="badge bg-success paidOn_text">
                                  {card.paymentDate ? `Paid on ${fmtDate(card.paymentDate)}` : 'Paid'}
                                </span>
                              )}
                            </td>
                            {customer.showInvoiceColumn && (
                              <td style={{ textAlign: 'left' }}>
                                {paid ? (
                                  <Link
                                    className="btn btn-info btn-sm view_btn"
                                    to={`/dashboard/invoices?cardId=${card.id}`}
                                    title="View Invoice History"
                                  >
                                    View
                                  </Link>
                                ) : (
                                  <span className="download" title="Payment required to download invoice">
                                    <span className="download_icon_style" style={{ filter: 'grayscale(100%)', opacity: 0.5 }}>
                                      <i className="fa-solid fa-arrow-down" />
                                    </span>
                                  </span>
                                )}
                              </td>
                            )}
                            {showRefundCol && (
                              <td style={{ textAlign: 'left' }}>
                                <span className={`badge ${customer.refundStatus === 'Refund Settled' ? 'bg-success' : 'bg-warning'}`}>
                                  {customer.refundStatus === 'Refund Settled' ? 'Refund Settled' : 'Refund Claimed'}
                                  {customer.refundStatusDate ? (
                                    <>
                                      <br />
                                      on {fmtDate(customer.refundStatusDate)}
                                    </>
                                  ) : null}
                                </span>
                              </td>
                            )}
                          </tr>
                        )
                      })
                    )}
                  </tbody>
                </table>
                {pager.showPager ? (
                  <TablePagination
                    total={filteredCards.length}
                    page={pager.page}
                    pageSize={pager.pageSize}
                    onPageChange={pager.setPage}
                    onPageSizeChange={pager.setPageSize}
                  />
                ) : null}
              </div>

              <br />
              <br />
              {/* Same gate as PHP: show unless collaboration is on without mw_referral_id */}
              {(session?.collaborationEnabled !== true || customer.mwReferralId === 1) && customer.referralCode ? (
                <>
                  <div className="referral-id">ShareBizz Referral ID</div>
                  <div className="referral-container">
                    <div className="referral-box col-md-6">
                      <p id="regular_link_value">
                        {siteUrl(`registration/customer-registration.php?ref=${encodeURIComponent(customer.referralCode)}`)}
                      </p>
                      <button
                        type="button"
                        className="copy-btn"
                        onClick={() => {
                          const el = document.getElementById('regular_link_value')
                          if (el?.textContent) void navigator.clipboard.writeText(el.textContent)
                        }}
                      >
                        COPY LINK
                      </button>
                    </div>
                    <div className="referral-box col-md-6">
                      <p id="regular_code_value">{customer.referralCode}</p>
                      <button
                        type="button"
                        className="copy-btn"
                        onClick={() => {
                          void navigator.clipboard.writeText(customer.referralCode)
                        }}
                      >
                        COPY CODE
                      </button>
                    </div>
                  </div>
                  <div className="social-icons">
                    <p>Refer ShareBizz</p>
                    <ul>
                      {(() => {
                        const refUrl = siteUrl(
                          `registration/customer-registration.php?ref=${encodeURIComponent(customer.referralCode)}`,
                        )
                        const msg = `🚀 Grow Your Business with ShareBizz\n\n👇 Create Your ShareBizz Today\n\n${refUrl}\n\n🌐 www.sharebizz.com`
                        return (
                          <>
                            <li>
                              <a
                                href={`https://api.whatsapp.com/send?text=${encodeURIComponent(msg)}`}
                                target="_blank"
                                rel="noreferrer"
                              >
                                <img src={assetUrl('images/whatsapp.png')} alt="" />
                              </a>
                            </li>
                            <li>
                              <a
                                href={`https://www.facebook.com/sharer/sharer.php?u=${encodeURIComponent(refUrl)}`}
                                target="_blank"
                                rel="noreferrer"
                              >
                                <img src={assetUrl('images/facebook.png')} alt="" />
                              </a>
                            </li>
                            <li>
                              <a
                                href={`https://www.instagram.com/share?url=${encodeURIComponent(refUrl)}`}
                                target="_blank"
                                rel="noreferrer"
                              >
                                <img src={assetUrl('images/instagram.png')} alt="" />
                              </a>
                            </li>
                            <li>
                              <a
                                href={`https://twitter.com/intent/tweet?text=${encodeURIComponent(`Join using my referral link: ${refUrl}`)}&url=${encodeURIComponent(refUrl)}`}
                                target="_blank"
                                rel="noreferrer"
                              >
                                <img src={assetUrl('images/twitter.png')} alt="" />
                              </a>
                            </li>
                            <li>
                              <a
                                href={`https://www.linkedin.com/sharing/share-offsite/?url=${encodeURIComponent(refUrl)}`}
                                target="_blank"
                                rel="noreferrer"
                              >
                                <img src={assetUrl('images/linkedin.png')} alt="" />
                              </a>
                            </li>
                          </>
                        )
                      })()}
                    </ul>
                  </div>
                </>
              ) : null}
            </div>
          </div>
        )}
    </div>
  )
}
