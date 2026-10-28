import { Link } from 'react-router-dom'
import { TablePagination } from '@/components/table/TablePagination'
import { useClientPagination } from '@/components/table/useClientPagination'

/** Columns shared by every signed-in dashboard profile table. */
export const DASHBOARD_PROFILE_COLUMNS = [
  'DB ID',
  'User Email',
  'User Name',
  'User Number',
  'Company Name',
  'Date Created',
  'Expiry Date',
  'DB Profile Status',
  'Actions',
  'Profile Payment Status',
  'Product Invoice Details',
] as const

export type DashboardProfileRow = {
  key: string
  dbId?: number | string | null
  email?: string | null
  name?: string | null
  phone?: string | null
  companyName?: string | null
  createdAt?: string | null
  expiryDate?: string | null
  expiryDisplay?: string | null
  paymentStatus?: string | null
  cardStatus?: string | null
  paymentDate?: string | null
  cardSlug?: string | null
  cardId?: number | null
}

type DashboardProfileTableProps = {
  rows: DashboardProfileRow[]
  emptyMessage: string
  /** Resets to page 1 when the signed-in user or filter changes. */
  resetKey?: string | number | null
  title?: string
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

function cell(value?: string | number | null) {
  if (value == null) return '-'
  const text = String(value).trim()
  return text || '-'
}

function profileStatus(paymentStatus?: string | null, cardStatus?: string | null) {
  if (paymentStatus === 'Success') return { className: 'bg-success', text: cardStatus || 'Active' }
  if (paymentStatus === 'Failed') return { className: 'not-eligible', text: 'Inactive' }
  return { className: 'not-eligible', text: 'Pending Payment' }
}

function ProfileActions({ row }: { row: DashboardProfileRow }) {
  if (row.cardId == null) return <>-</>
  const viewUrl = `/n/${encodeURIComponent(row.cardSlug || String(row.cardId))}`
  const editUrl = `/website/business-name?card_number=${row.cardId}`
  const shareUrl = `https://api.whatsapp.com/send?text=${encodeURIComponent(`${window.location.origin}${viewUrl}`)}`
  return (
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
  )
}

function PaymentCell({ row }: { row: DashboardProfileRow }) {
  if (row.cardId == null) return <>-</>
  if (row.paymentStatus !== 'Success') {
    return (
      <Link className="btn btn-primary paynow_btn" to={`/payment/db-profile?id=${row.cardId}`}>
        Pay Now
      </Link>
    )
  }
  return (
    <span className="badge bg-success paidOn_text">
      {row.paymentDate ? `Paid on ${fmtDate(row.paymentDate)}` : 'Paid'}
    </span>
  )
}

function InvoiceCell({ row }: { row: DashboardProfileRow }) {
  if (row.cardId != null && row.paymentStatus === 'Success') {
    return (
      <Link
        className="btn btn-info btn-sm view_btn"
        to={`/dashboard/invoices?cardId=${row.cardId}`}
        title="View Invoice History"
      >
        View
      </Link>
    )
  }
  return (
    <span className="download" title="Payment required to download invoice">
      <span className="download_icon_style" style={{ filter: 'grayscale(100%)', opacity: 0.5 }}>
        <i className="fa-solid fa-arrow-down" />
      </span>
    </span>
  )
}

export function DashboardProfileTable({ rows, emptyMessage, resetKey, title }: DashboardProfileTableProps) {
  const pager = useClientPagination(rows.length, { resetKey })
  const pageRows = pager.slice(rows)

  return (
    <div className={title ? 'ManageUsers' : undefined}>
      {title ? <h4 className="heading">{title}</h4> : null}
      <div className="table-responsive mw-table-scroll mw-table-scroll-wide mw-dashboard-table-wrap" id="dashboardTableWrap">
        <table id="ReferredUsers" className="display table mb-0" style={{ textAlign: 'center' }}>
          <thead className="bg-secondary">
            <tr>
              {DASHBOARD_PROFILE_COLUMNS.map((heading) => (
                <th key={heading}>{heading}</th>
              ))}
            </tr>
          </thead>
          <tbody>
            {rows.length === 0 ? (
              <tr>
                <td colSpan={DASHBOARD_PROFILE_COLUMNS.length} className="text-center">
                  {emptyMessage}
                </td>
              </tr>
            ) : (
              pageRows.map((row) => {
                const status = profileStatus(row.paymentStatus, row.cardStatus)
                const expiry = row.expiryDisplay && row.expiryDisplay !== '-' ? row.expiryDisplay : fmtDate(row.expiryDate)
                return (
                  <tr key={row.key}>
                    <td>{cell(row.dbId)}</td>
                    <td>{cell(row.email)}</td>
                    <td>{cell(row.name)}</td>
                    <td>{cell(row.phone)}</td>
                    <td>{cell(row.companyName)}</td>
                    <td>{fmtDate(row.createdAt)}</td>
                    <td>{expiry}</td>
                    <td>
                      <span className={status.className}>{status.text}</span>
                    </td>
                    <td>
                      <ProfileActions row={row} />
                    </td>
                    <td>
                      <PaymentCell row={row} />
                    </td>
                    <td style={{ textAlign: 'left' }}>
                      <InvoiceCell row={row} />
                    </td>
                  </tr>
                )
              })
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
  )
}
