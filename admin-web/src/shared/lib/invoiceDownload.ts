/** Opens tax invoice HTML from the .NET API (no PHP). */
export function openInvoiceById(invoiceId: number) {
  if (!invoiceId) return
  const url = `/api/v1/admin/invoices/${invoiceId}/download`
  window.open(url, '_blank', 'noopener,noreferrer')
}

/** Opens latest invoice for a digi_card / MW id. */
export function openInvoiceByCardId(cardId: number) {
  if (!cardId) return
  const url = `/api/v1/admin/invoices/by-card/${cardId}/download`
  window.open(url, '_blank', 'noopener,noreferrer')
}
