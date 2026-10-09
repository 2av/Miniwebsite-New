import { useEffect, useMemo, useState } from 'react'
import { apiGet, apiSend, ApiError } from '@/shared/api/client'
import { useAuthSession } from '@/shared/auth/useAuthSession'
import { siteUrl } from '@/lib/utils'
import { TablePagination, useClientPagination } from '@/components/table'

type BankForm = {
  bankName: string
  accountHolder: string
  accountNumber: string
  ifscCode: string
  upiId: string
  upiName: string
}

type ReferredUser = {
  userId?: number | null
  cardId?: number | null
  mwFrIdLabel: string
  mwFrIsCardLink: boolean
  email?: string | null
  userType: string
  name?: string | null
  phone?: string | null
  joinedOnDisplay?: string | null
  referralSource: string
  dateCreatedDisplay?: string | null
  validityDisplay?: string | null
  mwStatus: string
  mwStatusClass: string
  userPaymentLabel: string
  userPaymentClass: string
  amount: number
  referralPaymentLabel: string
  referralPaymentClass: string
}

type ReferralData = {
  role: string
  isTeamView: boolean
  pageTitle: string
  referralCode: string
  pendingAmount: number
  totalEarning: number
  referralCount: number
  totalSales: number
  totalMwCreated: number
  bankDetailsComplete: boolean
  bank: BankForm
  referredUsers: ReferredUser[]
}

function pick(o: Record<string, unknown>, camel: string, pascal: string) {
  return o[camel] ?? o[pascal]
}

function normalize(raw: unknown): ReferralData {
  const o = (raw ?? {}) as Record<string, unknown>
  const bank = (pick(o, 'bank', 'Bank') ?? {}) as Record<string, unknown>
  const users = ((pick(o, 'referredUsers', 'ReferredUsers') as unknown[]) || []) as unknown[]
  return {
    role: String(pick(o, 'role', 'Role') ?? ''),
    isTeamView: Boolean(pick(o, 'isTeamView', 'IsTeamView')),
    pageTitle: String(pick(o, 'pageTitle', 'PageTitle') ?? 'Referral Details'),
    referralCode: String(pick(o, 'referralCode', 'ReferralCode') ?? ''),
    pendingAmount: Number(pick(o, 'pendingAmount', 'PendingAmount') ?? 0),
    totalEarning: Number(pick(o, 'totalEarning', 'TotalEarning') ?? 0),
    referralCount: Number(pick(o, 'referralCount', 'ReferralCount') ?? 0),
    totalSales: Number(pick(o, 'totalSales', 'TotalSales') ?? 0),
    totalMwCreated: Number(pick(o, 'totalMwCreated', 'TotalMwCreated') ?? 0),
    bankDetailsComplete: Boolean(pick(o, 'bankDetailsComplete', 'BankDetailsComplete')),
    bank: {
      bankName: String(pick(bank, 'bankName', 'BankName') ?? ''),
      accountHolder: String(pick(bank, 'accountHolder', 'AccountHolder') ?? ''),
      accountNumber: String(pick(bank, 'accountNumber', 'AccountNumber') ?? ''),
      ifscCode: String(pick(bank, 'ifscCode', 'IfscCode') ?? ''),
      upiId: String(pick(bank, 'upiId', 'UpiId') ?? ''),
      upiName: String(pick(bank, 'upiName', 'UpiName') ?? ''),
    },
    referredUsers: users.map((u) => {
      const r = (u ?? {}) as Record<string, unknown>
      return {
        userId: (pick(r, 'userId', 'UserId') as number | null) ?? null,
        cardId: (pick(r, 'cardId', 'CardId') as number | null) ?? null,
        mwFrIdLabel: String(pick(r, 'mwFrIdLabel', 'MwFrIdLabel') ?? 'N/A'),
        mwFrIsCardLink: Boolean(pick(r, 'mwFrIsCardLink', 'MwFrIsCardLink')),
        email: (pick(r, 'email', 'Email') as string | null) ?? null,
        userType: String(pick(r, 'userType', 'UserType') ?? 'MW'),
        name: (pick(r, 'name', 'Name') as string | null) ?? null,
        phone: (pick(r, 'phone', 'Phone') as string | null) ?? null,
        joinedOnDisplay: (pick(r, 'joinedOnDisplay', 'JoinedOnDisplay') as string | null) ?? null,
        referralSource: String(pick(r, 'referralSource', 'ReferralSource') ?? 'Referral'),
        dateCreatedDisplay: (pick(r, 'dateCreatedDisplay', 'DateCreatedDisplay') as string | null) ?? null,
        validityDisplay: (pick(r, 'validityDisplay', 'ValidityDisplay') as string | null) ?? null,
        mwStatus: String(pick(r, 'mwStatus', 'MwStatus') ?? '-'),
        mwStatusClass: String(pick(r, 'mwStatusClass', 'MwStatusClass') ?? 'bg-pending'),
        userPaymentLabel: String(pick(r, 'userPaymentLabel', 'UserPaymentLabel') ?? 'Unpaid'),
        userPaymentClass: String(pick(r, 'userPaymentClass', 'UserPaymentClass') ?? 'bg-unpaid'),
        amount: Number(pick(r, 'amount', 'Amount') ?? 0),
        referralPaymentLabel: String(pick(r, 'referralPaymentLabel', 'ReferralPaymentLabel') ?? 'Not Eligible'),
        referralPaymentClass: String(pick(r, 'referralPaymentClass', 'ReferralPaymentClass') ?? 'not-eligible'),
      }
    }),
  }
}

function formatInr(n: number) {
  return Math.round(n).toLocaleString('en-IN')
}

export function ReferralPage({ title }: { title?: string } = {}) {
  const session = useAuthSession()
  const [data, setData] = useState<ReferralData | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [bankAlert, setBankAlert] = useState<{ type: 'success' | 'danger'; message: string } | null>(null)
  const [bank, setBank] = useState<BankForm>({
    bankName: '',
    accountHolder: '',
    accountNumber: '',
    ifscCode: '',
    upiId: '',
    upiName: '',
  })
  const [saving, setSaving] = useState(false)

  async function load() {
    if (!session?.email) return
    try {
      setError(null)
      const raw = await apiGet<unknown>('/api/v1/portal/referral', { email: session.email })
      const page = normalize(raw)
      setData(page)
      setBank(page.bank)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to load')
    }
  }

  useEffect(() => {
    void load()
  }, [session?.email])

  const pageTitle = title || data?.pageTitle || (session?.role === 'TEAM' ? 'Sales Details' : 'Referral Details')
  const isTeam = data?.isTeamView ?? session?.role === 'TEAM'
  const bankLocked = Boolean(data?.bankDetailsComplete)
  const referredUsers = data?.referredUsers ?? []
  const pager = useClientPagination(referredUsers.length, { resetKey: `${session?.email ?? ''}:${isTeam ? 't' : 'c'}` })
  const pagedUsers = pager.slice(referredUsers)

  const statCards = useMemo(() => {
    if (!data) return []
    if (isTeam) {
      return [
        { label: 'Total Sales', icon: 'cart-shopping', value: String(data.totalSales), currency: false },
        { label: 'Total MW Created', icon: 'circle-check', value: String(data.totalMwCreated), currency: false },
      ]
    }
    return [
      { label: 'Pending Amount', icon: 'clock', value: formatInr(data.pendingAmount), currency: true },
      { label: 'Total Referral Earning', icon: 'indian-rupee-sign', value: formatInr(data.totalEarning), currency: true },
      { label: 'Referred MW', icon: 'users', value: String(data.referralCount), currency: false },
    ]
  }, [data, isTeam])

  function validateBank(): string | null {
    const fields = [bank.bankName, bank.accountHolder, bank.accountNumber, bank.ifscCode, bank.upiId, bank.upiName]
    if (fields.some((v) => !v.trim())) return 'All fields are mandatory. Please fill all the required fields.'
    if (!/^\d+$/.test(bank.accountNumber.trim())) return 'Account number should contain only numbers.'
    if (bank.ifscCode.trim().length !== 11) return 'IFSC code should be exactly 11 characters.'
    if (!bank.upiId.includes('@')) return 'Please enter a valid UPI ID (e.g., user@paytm).'
    return null
  }

  async function saveBank(e: React.FormEvent) {
    e.preventDefault()
    if (bankLocked) return
    const err = validateBank()
    if (err) {
      setBankAlert({ type: 'danger', message: err })
      return
    }
    setSaving(true)
    setBankAlert(null)
    try {
      const res = await apiSend('post', '/api/v1/portal/bank-details', {
        email: session?.email,
        ...bank,
      })
      setBankAlert({ type: 'success', message: res.message || 'Bank details saved successfully!' })
      await load()
    } catch (err) {
      setBankAlert({
        type: 'danger',
        message: err instanceof ApiError ? err.message : 'Save failed',
      })
    } finally {
      setSaving(false)
    }
  }

  const colspan = isTeam ? 11 : 14

  return (
    <>
      <div className="main-top mw-page-header">
        <h1 className="heading mw-page-title">{pageTitle}</h1>
        <nav aria-label="breadcrumb">
          <ol className="breadcrumb mw-breadcrumb">
            <li className="breadcrumb-item mw-breadcrumb-item">
              <a href="#">ShareBizz</a>
            </li>
            <li className="breadcrumb-item mw-breadcrumb-item active" aria-current="page">
              {pageTitle}
            </li>
          </ol>
        </nav>
      </div>

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
                {statCards.map((stat) => (
                  <div className="card_area" key={stat.label}>
                    <div className="card">
                      <div className="img mw-dash-card-icon mw-dash-card-icon--blue" aria-hidden>
                        <i className={`fa-solid fa-${stat.icon}`} />
                      </div>
                      <div className="content">
                        <p>{stat.label}</p>
                        <h4>
                          {stat.currency ? <i className="fa fa-inr" aria-hidden /> : null}{' '}
                          {stat.value}
                          {stat.currency ? '/-' : ''}
                        </h4>
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            </div>

            {!isTeam ? (
              <section className="mw-bank-details-section mb-8 pt-6 border-t border-border">
                <h2 className="heading mw-section-title">Bank Account Details</h2>
                <p className="mw-helper-text mb-4">
                  <i className="fa fa-info-circle" aria-hidden />{' '}
                  <span>Please submit the bank details where you want us to transfer your referral earnings.</span>
                </p>

                {bankAlert ? (
                  <div
                    className={`mw-alert mw-alert-${bankAlert.type === 'success' ? 'success' : 'danger'}`}
                    role="alert"
                  >
                    <i
                      className={`fa fa-${bankAlert.type === 'success' ? 'check-circle' : 'exclamation-circle'} mw-alert-icon`}
                      aria-hidden
                    />
                    <div className="mw-alert-body">{bankAlert.message}</div>
                  </div>
                ) : null}

                <form id="bankDetailsForm" className="mw-form" onSubmit={saveBank}>
                  <div className="mw-form-grid-2">
                    {(
                      [
                        ['bankName', 'Bank Name', 'Enter Bank Name'],
                        ['accountHolder', 'Account Holder Name', 'Account Holder Name'],
                        ['accountNumber', 'Bank Account Number', 'Enter Your Bank Account Number'],
                        ['ifscCode', 'Bank IFSC Code', 'Enter IFSC Code'],
                        ['upiId', 'UPI ID', 'Enter UPI ID (e.g., user@paytm)'],
                        ['upiName', 'UPI Name', 'Enter UPI Name'],
                      ] as const
                    ).map(([key, label, placeholder]) => (
                      <div className="mw-form-group" key={key}>
                        <label className="mw-label" htmlFor={key}>
                          {label} <span className="req">*</span>
                        </label>
                        <input
                          type="text"
                          name={key}
                          id={key}
                          placeholder={placeholder}
                          className="form-control mw-input"
                          value={bank[key]}
                          required
                          readOnly={bankLocked}
                          onChange={(e) => setBank((b) => ({ ...b, [key]: e.target.value }))}
                        />
                      </div>
                    ))}
                  </div>

                  {bankLocked ? (
                    <>
                      <p className="mw-helper-text mb-4">
                        <i className="fa fa-info-circle" aria-hidden />{' '}
                        <span>
                          Bank details have been saved successfully. Contact support if you need to make changes.
                        </span>
                      </p>
                      <button type="button" className="mw-btn mw-btn-secondary" disabled>
                        Bank Details Saved
                      </button>
                    </>
                  ) : (
                    <button type="submit" className="mw-btn mw-btn-save" disabled={saving}>
                      {saving ? 'Saving…' : 'Submit'}
                    </button>
                  )}
                </form>
              </section>
            ) : null}

            <section className={isTeam ? '' : 'pt-6 border-t border-border'}>
              <h2 className="heading mw-section-title mb-4">Referred Users</h2>
              <div className="mw-table-scroll mw-table-scroll-xl">
                <table id="ReferredUsers" className="display table w-full mb-0">
                  <thead>
                    <tr>
                      <th className="text-center">User ID</th>
                      <th className="text-center">MW ID/FR ID</th>
                      <th className="text-center">User Email</th>
                      <th className="text-center">User Type</th>
                      <th className="text-center">User Name</th>
                      <th className="text-center">User Number</th>
                      <th className="text-center">Joined On</th>
                      {!isTeam ? <th className="text-center">Referral Source</th> : null}
                      <th className="text-center">Date Created</th>
                      <th className="text-center">Validity Date</th>
                      <th className="text-center">MW Status</th>
                      <th className="text-center">User Payment Status</th>
                      {!isTeam ? (
                        <>
                          <th className="text-center">Referral Amt.</th>
                          <th className="text-center">MW Payment Status</th>
                        </>
                      ) : null}
                    </tr>
                  </thead>
                  <tbody>
                    {referredUsers.length === 0 ? (
                      <tr>
                        <td colSpan={colspan} className="text-center">
                          No referrals found
                        </td>
                      </tr>
                    ) : (
                      pagedUsers.map((row, idx) => (
                        <tr key={`${row.email ?? 'u'}-${row.cardId ?? idx}-${idx}`}>
                          <td className="text-center">{row.userId ?? 'N/A'}</td>
                          <td className="text-center">
                            {row.mwFrIsCardLink && row.cardId ? (
                              <a
                                href={siteUrl(`n.php?n=${row.cardId}`)}
                                target="_blank"
                                rel="noopener noreferrer"
                                className="mw-table-cell-inline text-primary hover:text-primary-dark !no-underline"
                              >
                                <i className="fa fa-eye" aria-hidden /> {row.mwFrIdLabel}
                              </a>
                            ) : (
                              row.mwFrIdLabel
                            )}
                          </td>
                          <td className="text-center">{row.email || 'N/A'}</td>
                          <td className="text-center">{row.userType}</td>
                          <td className="text-center">{row.name || 'Unknown'}</td>
                          <td className="text-center">{row.phone || 'N/A'}</td>
                          <td className="text-center">{row.joinedOnDisplay || '-'}</td>
                          {!isTeam ? <td className="text-center">{row.referralSource}</td> : null}
                          <td className="text-center">{row.dateCreatedDisplay || '-'}</td>
                          <td className="text-center">{row.validityDisplay || '-'}</td>
                          <td className="text-center">
                            <span className={row.mwStatusClass}>{row.mwStatus}</span>
                          </td>
                          <td className="text-center">
                            <span className={row.userPaymentClass}>{row.userPaymentLabel}</span>
                          </td>
                          {!isTeam ? (
                            <>
                              <td className="text-center">₹ {formatInr(row.amount)}</td>
                              <td className="text-center">
                                <span className={row.referralPaymentClass}>{row.referralPaymentLabel}</span>
                              </td>
                            </>
                          ) : null}
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
