import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { apiGet } from '@/shared/api/client'
import { useAuthSession } from '@/shared/auth/useAuthSession'
import { dashboardTitleForRole } from '@/shared/auth/session'
import { assetUrl, siteUrl } from '@/lib/utils'
import { DashboardProfileTable, type DashboardProfileRow } from '@/components/table'
import { PortalPageHeader } from '@/components/layout/PortalPageHeader'

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
  dbIdDisplay: string
}

type CustomerDashboard = {
  email: string
  role: string
  referralCode: string
  refundStatus?: string | null
  refundStatusDate?: string | null
  mwReferralId?: number | null
  influencer: string
  userName: string
  phone: string
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
  cardSlug?: string | null
  dbIdDisplay: string
}

type FranchiseeDashboard = {
  email: string
  role: string
  totalCardsCreated: number
  managedUsers: FranchiseeManagedUser[]
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
    mwReferralId: pick(o, 'mwReferralId', 'MwReferralId') as number | null,
    influencer: String(pick(o, 'influencer', 'Influencer') ?? 'NO'),
    userName: String(pick(o, 'userName', 'UserName') ?? ''),
    phone: String(pick(o, 'phone', 'Phone') ?? ''),
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
        dbIdDisplay: String(pick(r, 'dbIdDisplay', 'DbIdDisplay') ?? ''),
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
        cardSlug: pick(r, 'cardSlug', 'CardSlug') as string | null,
        dbIdDisplay: String(pick(r, 'dbIdDisplay', 'DbIdDisplay') ?? ''),
      }
    }),
  }
}

function toFranchiseeProfileRows(users: FranchiseeManagedUser[]): DashboardProfileRow[] {
  return users.map((user) => ({
    key: `${user.userId}-${user.cardDbId ?? 0}`,
    dbId: user.dbIdDisplay || user.cardDbId,
    email: user.email,
    name: user.name,
    phone: user.phone,
    companyName: user.companyName,
    createdAt: user.cardCreatedDate || user.userCreatedAt,
    expiryDate: user.validityDate,
    expiryDisplay: user.validityDisplay,
    paymentStatus: user.paymentStatus,
    paymentDate: user.paymentDate,
    cardSlug: user.cardSlug,
    cardId: user.cardDbId,
  }))
}

function toCustomerProfileRows(
  cards: DigiCardListItem[],
  customer: CustomerDashboard,
  fallbackName?: string,
): DashboardProfileRow[] {
  return cards.map((card) => ({
    key: String(card.id),
    dbId: card.dbIdDisplay || card.id,
    email: customer.email,
    name: customer.userName || fallbackName,
    phone: customer.phone,
    companyName: card.companyName,
    createdAt: card.uploadedDate,
    expiryDate: card.validityDate,
    expiryDisplay: card.validityDisplay,
    paymentStatus: card.paymentStatus,
    cardStatus: card.cardStatus,
    paymentDate: card.paymentDate,
    cardSlug: card.cardSlug,
    cardId: card.id,
  }))
}

export function DashboardPage() {
  const session = useAuthSession()
  const prefix = (session?.rolePrefix || '').toUpperCase()
  const isFranchisee = prefix === 'FRP' || (!prefix && session?.role === 'FRANCHISEE')
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [customer, setCustomer] = useState<CustomerDashboard | null>(null)
  const [franchisee, setFranchisee] = useState<FranchiseeDashboard | null>(null)
  const [mappedRoleName, setMappedRoleName] = useState(session?.roleName ?? '')

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
  const pageTitle = dashboardTitleForRole(mappedRoleName || session?.roleName, session?.role)
  const franchiseeRows = toFranchiseeProfileRows(managedUsers)
  const customerRows = customer ? toCustomerProfileRows(cards, customer, session?.name) : []

  return (
    <div className="sb-dash">
        <PortalPageHeader
          title={pageTitle}
          subtitle={
            isFranchisee
              ? 'Track the DB Profiles you created and the users you manage.'
              : 'Manage your DB Profiles, payments, and sharing from one place.'
          }
        />

        {loading && <p>Loading dashboard...</p>}
        {error && <div className="alert alert-danger">{error}</div>}

        {!loading && !error && isFranchisee && franchisee && (
          <div className="card mb-4 sb-dash-panel">
            <div className="card-body">
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
                            DB Profile
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
                        <p>DB Profiles</p>
                        <h4 className="marginbottom5">{franchisee.totalCardsCreated}</h4>
                      </div>
                    </div>
                  </div>
                </div>
              </div>

              <DashboardProfileTable
                title="Manage Users: "
                rows={franchiseeRows}
                resetKey={session?.email ?? ''}
                emptyMessage="No users found."
              />
            </div>
          </div>
        )}

        {!loading && !error && !isFranchisee && customer && (
          <div className="card mb-4 sb-dash-panel">
            <div className="card-body">
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
                            DB Profile
                          </p>
                        </div>
                      </div>
                    </Link>
                  </div>
                </div>
              </div>

              <DashboardProfileTable
                rows={customerRows}
                resetKey={session?.email ?? ''}
                emptyMessage="No DB Profiles found. Create your first one!"
              />

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
