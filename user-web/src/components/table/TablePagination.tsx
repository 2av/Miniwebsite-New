import { TABLE_PAGE_SIZE_OPTIONS } from '@/components/table/useClientPagination'
import './TablePagination.css'

type Props = {
  total: number
  page: number
  pageSize: number
  onPageChange: (page: number) => void
  onPageSizeChange: (size: number) => void
  /** Hide when false (e.g. total ≤ 10). Default: show when total > 0. */
  visible?: boolean
}

/**
 * Shared table footer: page-size dropdown + Previous/Next.
 * Use with `useClientPagination` on any portal table.
 */
export function TablePagination({
  total,
  page,
  pageSize,
  onPageChange,
  onPageSizeChange,
  visible,
}: Props) {
  if (visible === false || total <= 0) return null

  const totalPages = Math.max(1, Math.ceil(total / pageSize))
  const safePage = Math.min(Math.max(1, page), totalPages)
  const from = (safePage - 1) * pageSize + 1
  const to = Math.min(safePage * pageSize, total)

  return (
    <div className="mw-table-pagination" role="navigation" aria-label="Table pagination">
      <div className="mw-table-pagination__size">
        <span className="mw-table-pagination__label">Show</span>
        <select
          className="mw-table-pagination__select"
          value={pageSize}
          onChange={(e) => onPageSizeChange(Number(e.target.value))}
          aria-label="Rows per page"
        >
          {TABLE_PAGE_SIZE_OPTIONS.map((n) => (
            <option key={n} value={n}>
              {n}
            </option>
          ))}
        </select>
        <span className="mw-table-pagination__label">entries</span>
      </div>

      <div className="mw-table-pagination__info">
        Showing {from} to {to} of {total} entries
      </div>

      <div className="mw-table-pagination__nav">
        <button
          type="button"
          className="mw-table-pagination__btn"
          disabled={safePage <= 1}
          onClick={() => onPageChange(safePage - 1)}
        >
          Previous
        </button>
        <span className="mw-table-pagination__page">
          Page {safePage} of {totalPages}
        </span>
        <button
          type="button"
          className="mw-table-pagination__btn"
          disabled={safePage >= totalPages}
          onClick={() => onPageChange(safePage + 1)}
        >
          Next
        </button>
      </div>
    </div>
  )
}
