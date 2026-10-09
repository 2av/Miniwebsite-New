import { api } from '@/shared/api/client'

/** Opens tax invoice HTML from the .NET API (no PHP). */
export function openInvoiceById(invoiceId: number) {
  if (!invoiceId) return
  void openInvoice(`/api/v1/admin/invoices/${invoiceId}/download`)
}

/** Opens latest invoice for a digi_card / MW id. */
export function openInvoiceByCardId(cardId: number) {
  if (!cardId) return
  void openInvoice(`/api/v1/admin/invoices/by-card/${cardId}/download`)
}

/** The endpoint needs the bearer token, so the HTML is fetched and shown from a blob URL. */
async function openInvoice(url: string) {
  // Opened synchronously so popup blockers treat it as part of the click.
  const win = window.open('', '_blank')
  try {
    const res = await api.get<string>(url, { responseType: 'text', headers: { Accept: 'text/html' } })
    const blobUrl = URL.createObjectURL(new Blob([res.data], { type: 'text/html' }))
    if (win) win.location.href = blobUrl
    else window.open(blobUrl, '_blank')
    setTimeout(() => URL.revokeObjectURL(blobUrl), 60_000)
  } catch (e) {
    win?.close()
    const body = (e as { response?: { data?: unknown } }).response?.data
    let message = 'Could not open the invoice.'
    if (typeof body === 'string') {
      try {
        message = (JSON.parse(body) as { message?: string }).message || message
      } catch {
        // Non-JSON error body; keep the generic message.
      }
    }
    window.alert(message)
  }
}
