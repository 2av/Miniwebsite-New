import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { apiGet } from '@/shared/api/client'
import { useAuthSession } from '@/shared/auth/useAuthSession'
import { TablePagination, useClientPagination } from '@/components/table'

type WalletTxn = {
  id: number
  deposit?: string | null
  withdraw?: string | null
  orderId?: string | null
  balance: number
  dateDisplay?: string | null
  message?: string | null
}

type WalletData = {
  balance: number
  hasSufficientBalance: boolean
  transactions: WalletTxn[]
}

function parseTxnDate(display?: string | null) {
  if (!display) return { date: '-', time: '-' }
  // API may return "dd-MM-yyyy" or "dd-MM-yyyy HH:mm"
  const parts = display.trim().split(/\s+/)
  if (parts.length >= 2) return { date: parts[0], time: parts.slice(1).join(' ') }
  return { date: parts[0] || '-', time: '-' }
}

function moneyOrDash(raw?: string | null, tone?: 'success' | 'danger') {
  const n = Number(String(raw ?? '').replace(/[^0-9.-]/g, ''))
  if (!raw || !Number.isFinite(n) || n === 0) return <span className="text-muted">-</span>
  return (
    <span className={tone === 'success' ? 'text-success' : tone === 'danger' ? 'text-danger' : undefined}>
      <i className="fa fa-inr" aria-hidden /> {Math.abs(n).toFixed(2)}
    </span>
  )
}

export function WalletPage() {
  const session = useAuthSession()
  const [data, setData] = useState<WalletData | null>(null)
  const [amount, setAmount] = useState('350')
  const [error, setError] = useState<string | null>(null)
  const transactions = data?.transactions ?? []
  const pager = useClientPagination(transactions.length, { resetKey: session?.email ?? '' })
  const pagedTx = pager.slice(transactions)

  useEffect(() => {
    if (!session?.email) return
    ;(async () => {
      try {
        const raw = await apiGet<Record<string, unknown>>('/api/v1/portal/wallet', { email: session.email })
        const tx = ((raw.transactions ?? raw.Transactions) as unknown[]) || []
        setData({
          balance: Number(raw.balance ?? raw.Balance ?? 0),
          hasSufficientBalance: Boolean(raw.hasSufficientBalance ?? raw.HasSufficientBalance),
          transactions: tx.map((t) => {
            const r = (t ?? {}) as Record<string, unknown>
            return {
              id: Number(r.id ?? r.Id),
              deposit: (r.deposit ?? r.Deposit) as string | null,
              withdraw: (r.withdraw ?? r.Withdraw) as string | null,
              orderId: (r.orderId ?? r.OrderId) as string | null,
              balance: Number(r.balance ?? r.Balance ?? 0),
              dateDisplay: (r.dateDisplay ?? r.DateDisplay) as string | null,
              message: (r.message ?? r.Message) as string | null,
            }
          }),
        })
      } catch (e) {
        setError(e instanceof Error ? e.message : 'Failed to load wallet')
      }
    })()
  }, [session?.email])

  function goRecharge(e: React.FormEvent) {
    e.preventDefault()
    const n = Number(amount)
    if (n < 350) {
      setError('Minimum recharge amount is ₹350')
      return
    }
    window.location.href = `/payment/wallet-recharge?amount=${encodeURIComponent(String(n))}`
  }

  return (
    <>
      <div className="main-top mw-page-header">
        <h1 className="heading mw-page-title">Franchise Wallet</h1>
        <nav aria-label="breadcrumb">
          <ol className="breadcrumb mw-breadcrumb">
            <li className="breadcrumb-item mw-breadcrumb-item">
              <Link to="/dashboard">ShareBizz</Link>
            </li>
            <li className="breadcrumb-item mw-breadcrumb-item active" aria-current="page">
              Wallet
            </li>
          </ol>
        </nav>
      </div>

      {error ? <div className="alert alert-danger">{error}</div> : null}

      {data ? (
        <div className="card mb-4">
          <div className="card-body">
            <div className="FranchiseeDashboard-head Wallet">
              <div className="mw-dash-card-grid">
                <div className="card_area">
                  <div className="card">
                    <div className="img mw-dash-card-icon mw-dash-card-icon--gold" aria-hidden>
                      <i className="fa-solid fa-wallet" />
                    </div>
                    <div className="content">
                      <p>Wallet Balance</p>
                      <h4>
                        <i className="fa fa-inr" aria-hidden /> {data.balance.toFixed(2)}
                      </h4>
                    </div>
                  </div>
                  {!data.hasSufficientBalance ? (
                    <p className="low_balance_title">Your balance is low. Please recharge wallet.</p>
                  ) : null}
                </div>

                <div className="card_area">
                  <div className="card">
                    <div className="img mw-dash-card-icon mw-dash-card-icon--blue" aria-hidden>
                      <i className="fa-solid fa-receipt" />
                    </div>
                    <div className="content">
                      <p>Total Transactions</p>
                      <h4>{transactions.length}</h4>
                    </div>
                  </div>
                </div>

                <div className="card_area card_area-recharge">
                  <div className="card RechargeWallet">
                    <div className="img mw-dash-card-icon mw-dash-card-icon--blue" aria-hidden>
                      <i className="fa-solid fa-circle-plus" />
                    </div>
                    <div className="content">
                      <p>Recharge Your Wallet</p>
                      <form id="walletRechargeForm" onSubmit={goRecharge}>
                        <div className="recharge_input_section">
                          <input
                            type="number"
                            name="recharge_amount"
                            className="form-control recharge_amount"
                            placeholder="Enter Recharge Amount"
                            min={350}
                            step={100}
                            value={amount}
                            onChange={(e) => setAmount(e.target.value)}
                            required
                          />
                        </div>
                        <button type="submit" name="add_money" className="btn btn-primary addMoney_btn">
                          ADD MONEY
                        </button>
                      </form>
                    </div>
                  </div>
                </div>
              </div>
            </div>

            <div className="ManageUsers">
              <h4 className="heading">Wallet Transactions: </h4>
              <div className="table-responsive mw-table-scroll mw-dashboard-table-wrap">
                <table id="WalletTransactions" className="display table mb-0">
                  <thead className="bg-secondary">
                    <tr>
                      <th>Date</th>
                      <th>Time</th>
                      <th>Transaction ID</th>
                      <th>Deposit</th>
                      <th>Withdrawal</th>
                      <th>Balance</th>
                      <th>Transaction Details</th>
                    </tr>
                  </thead>
                  <tbody>
                    {transactions.length === 0 ? (
                      <tr>
                        <td colSpan={7} className="text-center">
                          No transactions.
                        </td>
                      </tr>
                    ) : (
                      pagedTx.map((t) => {
                        const { date, time } = parseTxnDate(t.dateDisplay)
                        return (
                          <tr key={t.id}>
                            <td>{date}</td>
                            <td>{time}</td>
                            <td>
                              <span>{t.orderId || '-'}</span>
                            </td>
                            <td>{moneyOrDash(t.deposit, 'success')}</td>
                            <td>{moneyOrDash(t.withdraw, 'danger')}</td>
                            <td>
                              <i className="fa fa-inr" aria-hidden /> {t.balance.toFixed(2)}
                            </td>
                            <td>{t.message || '-'}</td>
                          </tr>
                        )
                      })
                    )}
                  </tbody>
                </table>
              </div>
              {pager.showPager ? (
                <TablePagination
                  total={transactions.length}
                  page={pager.page}
                  pageSize={pager.pageSize}
                  onPageChange={pager.setPage}
                  onPageSizeChange={pager.setPageSize}
                />
              ) : null}
            </div>
          </div>
        </div>
      ) : !error ? (
        <p>Loading wallet…</p>
      ) : null}
    </>
  )
}
