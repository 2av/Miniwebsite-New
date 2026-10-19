import { useEffect, useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { apiGet } from '@/shared/api/client'
import { useAuthSession } from '@/shared/auth/useAuthSession'
import { TablePagination, useClientPagination } from '@/components/table'
import { PortalPageHeader } from '@/components/layout/PortalPageHeader'

type InvoiceRow = {
  id: number
  invoiceNumber?: string | null
  invoiceDateDisplay?: string | null
  amount: number
  status?: string | null
}

export function InvoiceHistoryPage() {
  const session = useAuthSession()
  const [params] = useSearchParams()
  const cardId = Number(params.get('cardId') || params.get('card_id') || 0)
  const [rows, setRows] = useState<InvoiceRow[]>([])
  const [error, setError] = useState<string | null>(null)
  const pager = useClientPagination(rows.length, { resetKey: `${session?.email ?? ''}:${cardId}` })
  const pagedRows = pager.slice(rows)

  useEffect(() => {
    if (!session?.email || !cardId) return
    ;(async () => {
      try {
        const raw = await apiGet<unknown[]>('/api/v1/portal/invoices', { email: session.email, cardId })
        const list = Array.isArray(raw) ? raw : []
        setRows(
          list.map((x) => {
            const o = (x ?? {}) as Record<string, unknown>
            return {
              id: Number(o.id ?? o.Id),
              invoiceNumber: (o.invoiceNumber ?? o.InvoiceNumber) as string | null,
              invoiceDateDisplay: (o.invoiceDateDisplay ?? o.InvoiceDateDisplay) as string | null,
              amount: Number(o.amount ?? o.Amount ?? 0),
              status: (o.status ?? o.Status) as string | null,
            }
          }),
        )
      } catch (e) {
        setError(e instanceof Error ? e.message : 'Failed to load invoices')
      }
    })()
  }, [session?.email, cardId])

  return (
    <>
      <PortalPageHeader title="Invoice History" />

      {!cardId && <div className="alert alert-warning">Open invoices from a ShareBizz row (card id required).</div>}
      {error && <div className="alert alert-danger">{error}</div>}

      <div className="card">
        <div className="card-body table-responsive">
          <table className="table table-bordered">
            <thead>
              <tr>
                <th>Invoice #</th>
                <th>Date</th>
                <th>Amount</th>
                <th>Status</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {rows.length === 0 ? (
                <tr>
                  <td colSpan={5} className="text-center">
                    No invoices found.
                  </td>
                </tr>
              ) : (
                pagedRows.map((r) => (
                  <tr key={r.id}>
                    <td>{r.invoiceNumber}</td>
                    <td>{r.invoiceDateDisplay || '-'}</td>
                    <td>₹ {r.amount.toFixed(2)}</td>
                    <td>{r.status || '-'}</td>
                    <td>
                      <Link to={`/dashboard/invoices/${r.id}`} className="btn btn-sm btn-info me-2">
                        View
                      </Link>
                      <a
                        className="btn btn-sm btn-secondary"
                        href={`/api/v1/portal/invoices/${r.id}/html?email=${encodeURIComponent(session?.email || '')}`}
                        target="_blank"
                        rel="noreferrer"
                      >
                        Download
                      </a>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
          {pager.showPager ? (
            <TablePagination
              total={rows.length}
              page={pager.page}
              pageSize={pager.pageSize}
              onPageChange={pager.setPage}
              onPageSizeChange={pager.setPageSize}
            />
          ) : null}
        </div>
      </div>
    </>
  )
}

export function InvoiceDetailPage({ invoiceId }: { invoiceId: number }) {
  const session = useAuthSession()
  const [data, setData] = useState<Record<string, unknown> | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (!session?.email || !invoiceId) return
    ;(async () => {
      try {
        const raw = await apiGet<Record<string, unknown>>(`/api/v1/portal/invoices/${invoiceId}`, {
          email: session.email,
        })
        setData(raw)
      } catch (e) {
        setError(e instanceof Error ? e.message : 'Failed to load')
      }
    })()
  }, [session?.email, invoiceId])

  return (
    <>
      <PortalPageHeader title="Invoice Details" subtitle={invoiceId ? `Invoice #${invoiceId}` : undefined} />
      {error && <div className="alert alert-danger">{error}</div>}
      {data && (
        <div className="card">
          <div className="card-body">
            <p>
              <strong>Invoice #:</strong> {String(data.invoiceNumber ?? data.InvoiceNumber ?? '')}
            </p>
            <p>
              <strong>Date:</strong> {String(data.invoiceDateDisplay ?? data.InvoiceDateDisplay ?? '')}
            </p>
            <p>
              <strong>Customer:</strong> {String(data.userName ?? data.UserName ?? '')} (
              {String(data.userEmail ?? data.UserEmail ?? '')})
            </p>
            <p>
              <strong>Company:</strong> {String(data.companyName ?? data.CompanyName ?? '')}
            </p>
            <p>
              <strong>Amount:</strong> ₹ {Number(data.amount ?? data.Amount ?? 0).toFixed(2)}
            </p>
            <a
              className="btn btn-primary"
              href={`/api/v1/portal/invoices/${invoiceId}/html?email=${encodeURIComponent(session?.email || '')}`}
              target="_blank"
              rel="noreferrer"
            >
              Download / Print
            </a>
          </div>
        </div>
      )}
    </>
  )
}
