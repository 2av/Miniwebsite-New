import { useEffect, useMemo, useState } from 'react'

export const TABLE_PAGE_SIZE_OPTIONS = [10, 25, 50, 100] as const

export type TablePageSize = (typeof TABLE_PAGE_SIZE_OPTIONS)[number]

export type ClientPagination = {
  page: number
  pageSize: number
  total: number
  totalPages: number
  from: number
  to: number
  setPage: (page: number) => void
  setPageSize: (size: number) => void
  slice: <T>(rows: T[]) => T[]
  /** Show pager when total exceeds the default page size (10). */
  showPager: boolean
}

type Options = {
  initialPageSize?: TablePageSize
  /** When this changes (e.g. email / filter), page resets to 1. */
  resetKey?: string | number | null
}

/** Client-side pagination state for any table. */
export function useClientPagination(total: number, options: Options = {}): ClientPagination {
  const { initialPageSize = 10, resetKey } = options
  const [page, setPage] = useState(1)
  const [pageSize, setPageSizeState] = useState<number>(initialPageSize)

  useEffect(() => {
    setPage(1)
  }, [resetKey])

  const totalPages = Math.max(1, Math.ceil(Math.max(0, total) / pageSize))
  const safePage = Math.min(Math.max(1, page), totalPages)

  const from = total === 0 ? 0 : (safePage - 1) * pageSize + 1
  const to = Math.min(safePage * pageSize, total)

  const setPageSize = (size: number) => {
    setPageSizeState(size)
    setPage(1)
  }

  const slice = useMemo(() => {
    return <T,>(rows: T[]): T[] => {
      const start = (safePage - 1) * pageSize
      return rows.slice(start, start + pageSize)
    }
  }, [safePage, pageSize])

  return {
    page: safePage,
    pageSize,
    total,
    totalPages,
    from,
    to,
    setPage,
    setPageSize,
    slice,
    showPager: total > 10,
  }
}
