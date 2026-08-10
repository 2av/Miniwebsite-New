import { useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { apiGet, apiSend, ApiError } from '@/shared/api/client'
import { useAuthSession } from '@/shared/auth/useAuthSession'
import { siteUrl } from '@/lib/utils'
import { TablePagination, useClientPagination } from '@/components/table'

type CustomerRow = {
  id: number
  customerName: string
  phoneNumber: string
  emailId?: string | null
  companyName?: string | null
  website?: string | null
  addressLine1?: string | null
  areaCity?: string | null
  addressDisplay?: string | null
  comments?: string | null
  labelTag: string
  source: string
  status: string
  businessType?: string | null
  approachedFor?: string | null
  followupMethod?: string | null
  createdDisplay?: string | null
  lastSharedDisplay?: string | null
  lastUpdatedDisplay?: string | null
}

type FollowupRow = {
  id: number
  followupDatetimeDisplay?: string | null
  followupMethod: string
  followupStatus: string
  comments?: string | null
}

type PageData = {
  variant: 'customer' | 'team'
  showApproachedFor: boolean
  role: string
  ownerUserId: number
  businessTypeOptions: string[]
  customers: CustomerRow[]
}

type WaContext = 'whatsapp' | 'share_offer'

type WaTemplatesStore = {
  defaultId: string
  defaults: { share_offer: string; whatsapp: string }
  customs: Array<{ id: string; name: string; text: string }>
}

const DEFAULT_SHARE_TEMPLATE =
  'Hello 😊\nThis is [Business Name] from [Area, City].\n\nWe have added some latest special offers for you.👇\n👉 [MiniWebsite Link]\n\nIf anything interests you, feel free to message us on WhatsApp.\n\nLimited time offers hain, jaldi check karein 👍\n\nThank you 🙏'

const DEFAULT_WHATSAPP_TEMPLATE =
  'Hello 😊\nHope you are doing well.\n\nWe have added few new products & offers for you.👇\n👉 [MiniWebsite Link]\n\nYou can check. And please let us know if any requirements.\n\nThank you 🙏'

function seedWaTemplates(): WaTemplatesStore {
  return {
    defaultId: '',
    defaults: {
      share_offer: DEFAULT_SHARE_TEMPLATE,
      whatsapp: DEFAULT_WHATSAPP_TEMPLATE,
    },
    customs: [
      { id: 'custom_1', name: 'Custom Template 01', text: '' },
      { id: 'custom_2', name: 'Custom Template 02', text: '' },
      { id: 'custom_3', name: 'Custom Template 03', text: '' },
    ],
  }
}

function loadWaTemplates(ownerUserId: number): WaTemplatesStore {
  const seed = seedWaTemplates()
  if (!ownerUserId || typeof localStorage === 'undefined') return seed
  try {
    const raw = localStorage.getItem(`mw_wa_templates_${ownerUserId}`)
    if (!raw) return seed
    const parsed = JSON.parse(raw) as Partial<WaTemplatesStore> & {
      items?: Array<{ id: string; text?: string }>
    }
    seed.defaultId = parsed.defaultId || ''
    if (parsed.defaults?.share_offer) seed.defaults.share_offer = parsed.defaults.share_offer
    if (parsed.defaults?.whatsapp) seed.defaults.whatsapp = parsed.defaults.whatsapp
    if (Array.isArray(parsed.customs)) {
      parsed.customs.forEach((pc, i) => {
        if (seed.customs[i] && pc && pc.text !== undefined) seed.customs[i].text = pc.text || ''
      })
    } else if (Array.isArray(parsed.items)) {
      parsed.items.forEach((x) => {
        if (x.id === 'default_share_offer') seed.defaults.share_offer = x.text || seed.defaults.share_offer
        if (x.id === 'default_whatsapp') seed.defaults.whatsapp = x.text || seed.defaults.whatsapp
        const cu = seed.customs.find((c) => c.id === x.id)
        if (cu) cu.text = x.text || ''
      })
    }
    if (seed.defaultId === 'default_share_offer' || seed.defaultId === 'default_whatsapp') {
      seed.defaultId = 'default'
    }
    if (!['default', 'custom_1', 'custom_2', 'custom_3'].includes(seed.defaultId)) {
      seed.defaultId = ''
    }
    return seed
  } catch {
    return seed
  }
}

function saveWaTemplates(ownerUserId: number, store: WaTemplatesStore) {
  if (!ownerUserId || typeof localStorage === 'undefined') return
  localStorage.setItem(
    `mw_wa_templates_${ownerUserId}`,
    JSON.stringify({
      defaultId: store.defaultId,
      defaults: store.defaults,
      customs: store.customs,
    }),
  )
}

type CustomerForm = {
  customerName: string
  phoneNumber: string
  labelTag: string
  source: string
  status: string
  emailId: string
  companyName: string
  website: string
  addressLine1: string
  areaCity: string
  comments: string
  businessType: string
  approachedFor: string
  followupMethod: string
}

const EMPTY_FORM: CustomerForm = {
  customerName: '',
  phoneNumber: '',
  labelTag: 'Regular',
  source: 'Direct',
  status: 'Followup required',
  emailId: '',
  companyName: '',
  website: '',
  addressLine1: '',
  areaCity: '',
  comments: '',
  businessType: '',
  approachedFor: '',
  followupMethod: '',
}

const LABEL_OPTS = ['Regular', 'New', 'High Value', 'Repeat Customer']
const CUSTOMER_SOURCE = ['Direct', 'WhatsApp', 'Referral', 'Existing Contact', 'Walk-In', 'Website']
const TEAM_SOURCE = ['Direct', 'WhatsApp', 'Referral']
const CUSTOMER_STATUS = [
  'Followup required',
  'Phone Busy/Not Picked',
  'Important',
  'Deal Done',
  'Profile Shared',
  'Interested',
  'Not Interested',
]
const TEAM_STATUS = [
  'Followup required',
  'Phone Busy/Not Picked',
  'Important',
  'Joined',
  'Interested',
  'Not Interested',
]
const FOLLOWUP_METHODS = ['Call', 'Visited', 'WhatsApp', 'Email']
const APPROACHED_FOR = ['MW Sales', 'Franchise Sale']

function pick(o: Record<string, unknown>, camel: string, pascal: string) {
  return o[camel] ?? o[pascal]
}

function normalizePage(raw: unknown): PageData {
  const o = (raw ?? {}) as Record<string, unknown>
  const variant = String(pick(o, 'variant', 'Variant') ?? 'customer') === 'team' ? 'team' : 'customer'
  const customers = ((pick(o, 'customers', 'Customers') as unknown[]) || []).map((x) => {
    const r = (x ?? {}) as Record<string, unknown>
    return {
      id: Number(pick(r, 'id', 'Id') ?? 0),
      customerName: String(pick(r, 'customerName', 'CustomerName') ?? ''),
      phoneNumber: String(pick(r, 'phoneNumber', 'PhoneNumber') ?? ''),
      emailId: (pick(r, 'emailId', 'EmailId') as string | null) ?? null,
      companyName: (pick(r, 'companyName', 'CompanyName') as string | null) ?? null,
      website: (pick(r, 'website', 'Website') as string | null) ?? null,
      addressLine1: (pick(r, 'addressLine1', 'AddressLine1') as string | null) ?? null,
      areaCity: (pick(r, 'areaCity', 'AreaCity') as string | null) ?? null,
      addressDisplay: (pick(r, 'addressDisplay', 'AddressDisplay') as string | null) ?? null,
      comments: (pick(r, 'comments', 'Comments') as string | null) ?? null,
      labelTag: String(pick(r, 'labelTag', 'LabelTag') ?? 'Regular'),
      source: String(pick(r, 'source', 'Source') ?? 'Direct'),
      status: String(pick(r, 'status', 'Status') ?? 'Followup required'),
      businessType: (pick(r, 'businessType', 'BusinessType') as string | null) ?? null,
      approachedFor: (pick(r, 'approachedFor', 'ApproachedFor') as string | null) ?? null,
      followupMethod: (pick(r, 'followupMethod', 'FollowupMethod') as string | null) ?? null,
      createdDisplay: (pick(r, 'createdDisplay', 'CreatedDisplay') as string | null) ?? null,
      lastSharedDisplay: (pick(r, 'lastSharedDisplay', 'LastSharedDisplay') as string | null) ?? null,
      lastUpdatedDisplay: (pick(r, 'lastUpdatedDisplay', 'LastUpdatedDisplay') as string | null) ?? null,
    } satisfies CustomerRow
  })
  const biz = ((pick(o, 'businessTypeOptions', 'BusinessTypeOptions') as unknown[]) || []).map(String)
  return {
    variant,
    showApproachedFor: Boolean(pick(o, 'showApproachedFor', 'ShowApproachedFor')),
    role: String(pick(o, 'role', 'Role') ?? ''),
    ownerUserId: Number(pick(o, 'ownerUserId', 'OwnerUserId') ?? 0),
    businessTypeOptions: biz.length ? biz : ['Retail', 'Wholesale', 'Service', 'Online', 'Manufacturing', 'Other'],
    customers,
  }
}

function dash(v?: string | null) {
  const t = (v ?? '').trim()
  return t || '-'
}

export function CustomerManagerPage() {
  const session = useAuthSession()
  const [page, setPage] = useState<PageData | null>(null)
  const [search, setSearch] = useState('')
  const [searchInput, setSearchInput] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [msg, setMsg] = useState<string | null>(null)
  const [modalOpen, setModalOpen] = useState(false)
  const [expanded, setExpanded] = useState(false)
  const [editId, setEditId] = useState<number | null>(null)
  const [form, setForm] = useState<CustomerForm>(EMPTY_FORM)
  const [saving, setSaving] = useState(false)

  const [followupOpen, setFollowupOpen] = useState(false)
  const [followupCustomer, setFollowupCustomer] = useState<CustomerRow | null>(null)
  const [followups, setFollowups] = useState<FollowupRow[]>([])
  const [fuForm, setFuForm] = useState({
    followupDatetime: '',
    followupMethod: 'Call',
    followupStatus: 'Followup required',
    followupComments: '',
  })

  const [shareOpen, setShareOpen] = useState(false)
  const [shareCustomer, setShareCustomer] = useState<CustomerRow | null>(null)
  const [shareMsg, setShareMsg] = useState('')
  const [shareContext, setShareContext] = useState<WaContext>('whatsapp')
  const [waTemplates, setWaTemplates] = useState<WaTemplatesStore>(() => seedWaTemplates())
  const [selectedTemplateId, setSelectedTemplateId] = useState('default')
  const [templateNotice, setTemplateNotice] = useState<string | null>(null)

  const isTeam = page?.variant === 'team'
  const showApproached = Boolean(page?.showApproachedFor)
  const identityLocked = isTeam && editId != null
  const customers = page?.customers ?? []
  const pager = useClientPagination(customers.length, {
    resetKey: `${session?.email ?? ''}:${search}:${page?.variant ?? ''}`,
  })
  const pagedCustomers = pager.slice(customers)
  const followupPager = useClientPagination(followups.length, {
    resetKey: followupCustomer?.id ?? 0,
  })
  const pagedFollowups = followupPager.slice(followups)

  async function load(q = search) {
    if (!session?.email) return
    try {
      setError(null)
      const raw = await apiGet<unknown>('/api/v1/portal/customers', {
        email: session.email,
        ...(q.trim() ? { search: q.trim() } : {}),
      })
      setPage(normalizePage(raw))
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to load customers')
    }
  }

  useEffect(() => {
    void load()
  }, [session?.email])

  function openAdd() {
    setEditId(null)
    setForm({
      ...EMPTY_FORM,
      labelTag: isTeam ? '' : 'Regular',
      status: 'Followup required',
      source: 'Direct',
    })
    setExpanded(false)
    setModalOpen(true)
  }

  function openEdit(c: CustomerRow) {
    setEditId(c.id)
    setForm({
      customerName: c.customerName,
      phoneNumber: c.phoneNumber,
      labelTag: c.labelTag || 'Regular',
      source: c.source || 'Direct',
      status: c.status || 'Followup required',
      emailId: c.emailId || '',
      companyName: c.companyName || '',
      website: c.website || '',
      addressLine1: c.addressLine1 || '',
      areaCity: c.areaCity || '',
      comments: c.comments || '',
      businessType: c.businessType || '',
      approachedFor: c.approachedFor || '',
      followupMethod: c.followupMethod || '',
    })
    setExpanded(true)
    setModalOpen(true)
  }

  async function saveCustomer(e: React.FormEvent) {
    e.preventDefault()
    if (!session?.email) return
    setSaving(true)
    setMsg(null)
    setError(null)
    try {
      const res = await apiSend('post', '/api/v1/portal/customers', {
        ownerEmail: session.email,
        id: editId,
        ...form,
      })
      setMsg(res.message || 'Customer saved.')
      setModalOpen(false)
      await load()
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Save failed')
    } finally {
      setSaving(false)
    }
  }

  async function remove(id: number) {
    if (!session?.email || !confirm('Are you sure you want to delete this customer?')) return
    setError(null)
    setMsg(null)
    try {
      await apiSend('delete', `/api/v1/portal/customers/${id}?email=${encodeURIComponent(session.email)}`)
      setMsg('Customer deleted.')
      await load()
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Delete failed')
    }
  }

  async function openFollowup(c: CustomerRow) {
    if (!session?.email) return
    setFollowupCustomer(c)
    setFuForm({
      followupDatetime: '',
      followupMethod: 'Call',
      followupStatus: 'Followup required',
      followupComments: '',
    })
    setFollowupOpen(true)
    try {
      const raw = await apiGet<unknown[]>(`/api/v1/portal/customers/${c.id}/followups`, {
        email: session.email,
      })
      const list = Array.isArray(raw) ? raw : []
      setFollowups(
        list.map((x) => {
          const r = (x ?? {}) as Record<string, unknown>
          return {
            id: Number(pick(r, 'id', 'Id') ?? 0),
            followupDatetimeDisplay: (pick(r, 'followupDatetimeDisplay', 'FollowupDatetimeDisplay') as string) ?? null,
            followupMethod: String(pick(r, 'followupMethod', 'FollowupMethod') ?? ''),
            followupStatus: String(pick(r, 'followupStatus', 'FollowupStatus') ?? ''),
            comments: (pick(r, 'comments', 'Comments') as string) ?? null,
          }
        }),
      )
    } catch {
      setFollowups([])
    }
  }

  async function saveFollowup(e: React.FormEvent) {
    e.preventDefault()
    if (!session?.email || !followupCustomer) return
    try {
      const res = await apiSend('post', '/api/v1/portal/customers/followups', {
        ownerEmail: session.email,
        customerId: followupCustomer.id,
        ...fuForm,
      })
      setMsg(res.message || 'Followup added.')
      setFollowupOpen(false)
      await load()
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Followup failed')
    }
  }

  function openShare(c: CustomerRow, mode: 'wa' | 'offer') {
    const ctx: WaContext = mode === 'offer' ? 'share_offer' : 'whatsapp'
    const store = loadWaTemplates(page?.ownerUserId || 0)
    const allowed = ['default', 'custom_1', 'custom_2', 'custom_3']
    let pickId = store.defaultId
    if (!pickId || !allowed.includes(pickId)) pickId = 'default'
    const text =
      pickId === 'default'
        ? store.defaults[ctx]
        : store.customs.find((x) => x.id === pickId)?.text || store.defaults[ctx]

    setShareContext(ctx)
    setWaTemplates(store)
    setSelectedTemplateId(pickId)
    setShareMsg(text)
    setTemplateNotice(null)
    setShareCustomer(c)
    setShareOpen(true)
  }

  function selectTemplate(id: string) {
    setSelectedTemplateId(id)
    const text =
      id === 'default'
        ? waTemplates.defaults[shareContext]
        : waTemplates.customs.find((x) => x.id === id)?.text || ''
    setShareMsg(text)
  }

  function saveSelectedTemplate() {
    const next: WaTemplatesStore = {
      ...waTemplates,
      defaults: { ...waTemplates.defaults },
      customs: waTemplates.customs.map((c) => ({ ...c })),
    }
    if (selectedTemplateId === 'default') {
      next.defaults[shareContext] = shareMsg
    } else {
      const target = next.customs.find((x) => x.id === selectedTemplateId)
      if (!target) return
      target.text = shareMsg
    }
    setWaTemplates(next)
    saveWaTemplates(page?.ownerUserId || 0, next)
    setTemplateNotice('Template saved.')
  }

  function saveAsDefaultTemplate() {
    const next = { ...waTemplates, defaultId: selectedTemplateId || 'default' }
    setWaTemplates(next)
    saveWaTemplates(page?.ownerUserId || 0, next)
    setTemplateNotice('Default template updated.')
  }

  function sendWhatsApp() {
    if (!session?.email || !shareCustomer) return
    const digits = shareCustomer.phoneNumber.replace(/\D/g, '')
    if (!digits) {
      setError('Phone number missing.')
      return
    }
    const text = shareMsg
      .replaceAll('[Customer Name]', shareCustomer.customerName || 'Customer')
      .replaceAll('[Business Name]', 'Our Business')
      .replaceAll('[Area, City]', 'your area')
      .replaceAll('[MiniWebsite Link]', siteUrl(''))

    // Open during the click event so browsers do not block the WhatsApp tab.
    window.open(`https://wa.me/${digits}?text=${encodeURIComponent(text)}`, '_blank', 'noopener,noreferrer')
    setShareOpen(false)

    void apiSend(
      'post',
      `/api/v1/portal/customers/${shareCustomer.id}/last-shared?email=${encodeURIComponent(session.email)}`,
    )
      .then(() => load())
      .catch(() => {
        // Sharing already succeeded; a tracking failure should not interrupt it.
      })
  }

  const waTemplateRows = useMemo(() => {
    return [
      {
        id: 'default',
        name: 'Default Message',
        text: waTemplates.defaults[shareContext],
      },
      ...waTemplates.customs.map((c) => ({
        id: c.id,
        name: c.name,
        text: c.text,
      })),
    ]
  }, [waTemplates, shareContext])

  const sourceOpts = isTeam ? TEAM_SOURCE : CUSTOMER_SOURCE
  const statusOpts = isTeam ? TEAM_STATUS : CUSTOMER_STATUS

  const columns = useMemo(() => {
    if (isTeam) {
      const cols = [
        'SN',
        'Date Added',
        'Customer Name',
        'Phone Number',
        'Business Type',
        ...(showApproached ? ['Approached For'] : []),
        'Follow-up Method',
        'Status',
        'Source',
        'Email ID',
        'Company Name',
        'Website',
        'Address',
        'Last updated',
        'Comment',
        'Action',
      ]
      return cols
    }
    return [
      'SN',
      'Date Added',
      'Customer Name',
      'Phone Number',
      'Label',
      'Source',
      'Status',
      'Comment',
      'Email ID',
      'Company Name',
      'Website',
      'Address',
      'Last shared',
      'Action',
      'Manage',
    ]
  }, [isTeam, showApproached])

  return (
    <>
      <div className="main-top mw-page-header">
        <h1 className="heading mw-page-title">Customer Tracker</h1>
        <nav aria-label="breadcrumb">
          <ol className="breadcrumb mw-breadcrumb">
            <li className="breadcrumb-item mw-breadcrumb-item">
              <Link to="/dashboard">Mini Website</Link>
            </li>
            <li className="breadcrumb-item mw-breadcrumb-item active" aria-current="page">
              Customer Manager
            </li>
          </ol>
        </nav>
      </div>

      {error ? (
        <div className="mw-alert mw-alert-danger mb-3" role="alert">
          <i className="fa fa-exclamation-circle mw-alert-icon" aria-hidden />
          <div className="mw-alert-body">{error}</div>
        </div>
      ) : null}
      {msg ? (
        <div className="mw-alert mw-alert-success mb-3" role="alert">
          <i className="fa fa-check-circle mw-alert-icon" aria-hidden />
          <div className="mw-alert-body">{msg}</div>
        </div>
      ) : null}

      <div className="card mb-4">
        <div className="card-body">
          <div className="d-flex flex-wrap align-items-center gap-2 mb-3">
            <button type="button" className="mw-btn mw-btn-accent" onClick={openAdd}>
              <i className="fa fa-plus" aria-hidden /> Add Customer
            </button>
            <form
              id="customerTrackerSearchForm"
              className="d-flex flex-wrap align-items-center gap-2 flex-grow-1"
              onSubmit={(e) => {
                e.preventDefault()
                setSearch(searchInput)
                void load(searchInput)
              }}
            >
              <div className="tracker-search-input-wrap">
                <input
                  className="form-control"
                  placeholder="Search customers…"
                  value={searchInput}
                  onChange={(e) => setSearchInput(e.target.value)}
                />
                {searchInput ? (
                  <button
                    type="button"
                    className="tracker-search-input-clear"
                    aria-label="Clear search"
                    onClick={() => {
                      setSearchInput('')
                      setSearch('')
                      void load('')
                    }}
                  >
                    <i className="fa fa-times" aria-hidden />
                  </button>
                ) : null}
              </div>
              <button type="submit" className="mw-btn mw-btn-secondary">
                Search
              </button>
            </form>
          </div>

          <div className="ManageUsers">
            <h4 className="heading">Customers: </h4>
            <div id="customerTrackerTableWrap" className="table-responsive">
              <table id="customerTrackerTable" className="display table mb-0">
                <thead className="bg-secondary">
                  <tr>
                    {columns.map((h) => (
                      <th key={h}>{h}</th>
                    ))}
                  </tr>
                </thead>
              <tbody>
                {!page || customers.length === 0 ? (
                  <tr>
                    <td colSpan={columns.length} className="text-center">
                      No customers found.
                    </td>
                  </tr>
                ) : (
                  pagedCustomers.map((c, i) => (
                    <tr key={c.id}>
                      <td>{(pager.page - 1) * pager.pageSize + i + 1}</td>
                      <td>{dash(c.createdDisplay)}</td>
                      <td>{c.customerName}</td>
                      <td>{c.phoneNumber}</td>
                      {isTeam ? (
                        <>
                          <td>{dash(c.businessType)}</td>
                          {showApproached ? <td>{dash(c.approachedFor)}</td> : null}
                          <td>{dash(c.followupMethod)}</td>
                          <td>{dash(c.status)}</td>
                          <td>{dash(c.source)}</td>
                          <td>{dash(c.emailId)}</td>
                          <td>{dash(c.companyName)}</td>
                          <td>{dash(c.website)}</td>
                          <td>{dash(c.addressDisplay)}</td>
                          <td>{dash(c.lastUpdatedDisplay)}</td>
                          <td className="ct-table-comment">{dash(c.comments)}</td>
                          <td className="ct-cell-action">
                            <button type="button" className="btn btn-sm btn-info me-1" title="Edit" onClick={() => openEdit(c)}>
                              <i className="fa fa-pen" aria-hidden />
                            </button>
                            <button
                              type="button"
                              className="btn btn-sm btn-warning"
                              title="Add/View Followup"
                              onClick={() => void openFollowup(c)}
                            >
                              <i className="fa fa-clock" aria-hidden />
                            </button>
                          </td>
                        </>
                      ) : (
                        <>
                          <td>{c.labelTag || 'Regular'}</td>
                          <td>{dash(c.source)}</td>
                          <td>{dash(c.status)}</td>
                          <td className="ct-table-comment">{dash(c.comments)}</td>
                          <td>{dash(c.emailId)}</td>
                          <td>{dash(c.companyName)}</td>
                          <td>{dash(c.website)}</td>
                          <td>{dash(c.addressDisplay)}</td>
                          <td>{dash(c.lastSharedDisplay)}</td>
                          <td className="ct-cell-action">
                            <a
                              className={`btn btn-sm btn-outline-secondary me-1${c.phoneNumber ? '' : ' disabled'}`}
                              href={c.phoneNumber ? `tel:${c.phoneNumber}` : undefined}
                              aria-disabled={!c.phoneNumber}
                              title={c.phoneNumber ? `Call ${c.customerName}` : 'Phone number missing'}
                            >
                              <i className="fa fa-phone" aria-hidden />
                            </a>
                            <button
                              type="button"
                              className="btn btn-sm btn-success me-1"
                              title="WhatsApp"
                              disabled={!c.phoneNumber}
                              onClick={() => openShare(c, 'wa')}
                            >
                              <i className="fa-brands fa-whatsapp" aria-hidden />
                            </button>
                            <button
                              type="button"
                              className="btn btn-sm btn-warning"
                              title="Share Offer"
                              disabled={!c.phoneNumber}
                              onClick={() => openShare(c, 'offer')}
                            >
                              <i className="fa fa-gift" aria-hidden />
                            </button>
                          </td>
                          <td className="ct-cell-action">
                            <button type="button" className="btn btn-sm btn-info me-1" title="Edit" onClick={() => openEdit(c)}>
                              <i className="fa fa-pen" aria-hidden />
                            </button>
                            <button type="button" className="btn btn-sm btn-danger" title="Delete" onClick={() => void remove(c.id)}>
                              <i className="fa fa-trash" aria-hidden />
                            </button>
                          </td>
                        </>
                      )}
                    </tr>
                  ))
                )}
              </tbody>
            </table>
            {pager.showPager ? (
              <TablePagination
                total={customers.length}
                page={pager.page}
                pageSize={pager.pageSize}
                onPageChange={pager.setPage}
                onPageSizeChange={pager.setPageSize}
              />
            ) : null}
            </div>
          </div>
        </div>
      </div>

      {/* Add / Edit modal */}
      {modalOpen ? (
        <div className={`mw-modal mw-modal-light is-open${expanded ? ' expanded-view' : ''}`} role="dialog" aria-modal>
          <div className="mw-modal-backdrop" onClick={() => setModalOpen(false)} />
          <div className={`mw-modal-panel ${expanded ? 'mw-modal-lg' : 'mw-modal-sm'}`}>
            <div className="mw-modal-header">
              <div className="mw-modal-header-main">
                <div className="mw-modal-header-icon">
                  <i className="fa fa-user-plus" aria-hidden />
                </div>
                <div className="mw-modal-header-text-wrap">
                  <h2 className="mw-modal-title">{editId ? 'Edit Customer' : 'Add Customer'}</h2>
                </div>
              </div>
              <button type="button" className="mw-modal-close" aria-label="Close" onClick={() => setModalOpen(false)}>
                ×
              </button>
            </div>
            <form className="mw-modal-form" onSubmit={saveCustomer}>
              <div className="mw-modal-body">
                <div className="row g-3">
                  <div className="col-md-6">
                    <label className="mw-label">Customer Name <span className="req">*</span></label>
                    <input
                      className="form-control mw-input"
                      required
                      readOnly={identityLocked}
                      value={form.customerName}
                      onChange={(e) => setForm((f) => ({ ...f, customerName: e.target.value }))}
                    />
                  </div>
                  <div className="col-md-6">
                    <label className="mw-label">Phone Number <span className="req">*</span></label>
                    <div className="phone-group d-flex gap-2">
                      <select className="form-select country-code-select" disabled aria-label="Country code" defaultValue="+91">
                        <option value="+91">+91</option>
                      </select>
                      <input
                        className="form-control mw-input phone-input"
                        required
                        readOnly={identityLocked}
                        value={form.phoneNumber}
                        onChange={(e) => setForm((f) => ({ ...f, phoneNumber: e.target.value.replace(/\D/g, '').slice(0, 12) }))}
                      />
                    </div>
                  </div>

                  {isTeam ? (
                    <>
                      <div className="col-md-6">
                        <label className="mw-label">Business Type</label>
                        <select
                          className="form-select mw-input"
                          value={form.businessType}
                          onChange={(e) => setForm((f) => ({ ...f, businessType: e.target.value }))}
                        >
                          <option value="">-- Select Primary Category --</option>
                          {(page?.businessTypeOptions || []).map((b) => (
                            <option key={b} value={b}>
                              {b}
                            </option>
                          ))}
                        </select>
                      </div>
                      {showApproached ? (
                        <div className="col-md-6">
                          <label className="mw-label">Approached For</label>
                          <select
                            className="form-select mw-input"
                            value={form.approachedFor}
                            onChange={(e) => setForm((f) => ({ ...f, approachedFor: e.target.value }))}
                          >
                            <option value="">-- Select --</option>
                            {APPROACHED_FOR.map((a) => (
                              <option key={a} value={a}>
                                {a}
                              </option>
                            ))}
                          </select>
                        </div>
                      ) : null}
                      <div className="col-md-6">
                        <label className="mw-label">Followed-Up Method</label>
                        <select
                          className="form-select mw-input"
                          value={form.followupMethod}
                          onChange={(e) => setForm((f) => ({ ...f, followupMethod: e.target.value }))}
                        >
                          <option value="">-- Select --</option>
                          {FOLLOWUP_METHODS.map((m) => (
                            <option key={m} value={m}>
                              {m}
                            </option>
                          ))}
                        </select>
                      </div>
                    </>
                  ) : (
                    <div className="col-md-6">
                      <label className="mw-label">Label</label>
                      <select
                        className="form-select mw-input"
                        value={form.labelTag}
                        onChange={(e) => setForm((f) => ({ ...f, labelTag: e.target.value }))}
                      >
                        {LABEL_OPTS.map((l) => (
                          <option key={l} value={l}>
                            {l}
                          </option>
                        ))}
                      </select>
                    </div>
                  )}

                  <div className="col-md-6">
                    <label className="mw-label">Status</label>
                    <select
                      className="form-select mw-input"
                      value={form.status}
                      onChange={(e) => setForm((f) => ({ ...f, status: e.target.value }))}
                    >
                      {statusOpts.map((s) => (
                        <option key={s} value={s}>
                          {s}
                        </option>
                      ))}
                    </select>
                  </div>

                  {!expanded && !isTeam ? (
                    <>
                      <div className="col-md-6">
                        <label className="mw-label">Source</label>
                        <select
                          className="form-select mw-input"
                          value={form.source}
                          onChange={(e) => setForm((f) => ({ ...f, source: e.target.value }))}
                        >
                          {sourceOpts.map((s) => (
                            <option key={s} value={s}>
                              {s}
                            </option>
                          ))}
                        </select>
                      </div>
                      <div className="col-12">
                        <label className="mw-label">Comment</label>
                        <textarea
                          className="form-control mw-input"
                          rows={2}
                          value={form.comments}
                          onChange={(e) => setForm((f) => ({ ...f, comments: e.target.value }))}
                        />
                      </div>
                    </>
                  ) : null}

                  {expanded || isTeam ? (
                    <>
                      <div className="col-12">
                        <h3 className="mw-modal-section-title">Additional details</h3>
                      </div>
                      <div className="col-md-6">
                        <label className="mw-label">Source</label>
                        <select
                          className="form-select mw-input"
                          value={form.source}
                          onChange={(e) => setForm((f) => ({ ...f, source: e.target.value }))}
                        >
                          {sourceOpts.map((s) => (
                            <option key={s} value={s}>
                              {s}
                            </option>
                          ))}
                        </select>
                      </div>
                      <div className="col-md-6">
                        <label className="mw-label">Email ID</label>
                        <input
                          type="email"
                          className="form-control mw-input"
                          value={form.emailId}
                          onChange={(e) => setForm((f) => ({ ...f, emailId: e.target.value }))}
                        />
                      </div>
                      <div className="col-md-6">
                        <label className="mw-label">Company Name</label>
                        <input
                          className="form-control mw-input"
                          value={form.companyName}
                          onChange={(e) => setForm((f) => ({ ...f, companyName: e.target.value }))}
                        />
                      </div>
                      <div className="col-md-6">
                        <label className="mw-label">Website</label>
                        <input
                          className="form-control mw-input"
                          value={form.website}
                          onChange={(e) => setForm((f) => ({ ...f, website: e.target.value }))}
                        />
                      </div>
                      <div className="col-12">
                        <label className="mw-label">Address</label>
                        <input
                          className="form-control mw-input"
                          value={form.addressLine1}
                          onChange={(e) => setForm((f) => ({ ...f, addressLine1: e.target.value }))}
                        />
                      </div>
                      <div className="col-12">
                        <label className="mw-label">Comment</label>
                        <textarea
                          className="form-control mw-input"
                          rows={2}
                          value={form.comments}
                          onChange={(e) => setForm((f) => ({ ...f, comments: e.target.value }))}
                        />
                      </div>
                    </>
                  ) : null}

                  {!isTeam && !editId ? (
                    <div className="col-12">
                      <button type="button" className="mw-modal-expand-trigger btn btn-link p-0" onClick={() => setExpanded((v) => !v)}>
                        {expanded ? 'Hide additional fields' : 'Show more fields'}
                      </button>
                    </div>
                  ) : null}
                </div>
              </div>
              <div className="mw-modal-footer">
                <button type="button" className="mw-btn mw-btn-cancel" onClick={() => setModalOpen(false)}>
                  Cancel
                </button>
                <button type="submit" className="mw-btn mw-btn-accent" disabled={saving}>
                  {saving ? 'Saving…' : editId ? 'Update' : 'Save'}
                </button>
              </div>
            </form>
          </div>
        </div>
      ) : null}

      {/* Followup modal (team) */}
      {followupOpen && followupCustomer ? (
        <div className="mw-modal mw-modal-light is-open" role="dialog" aria-modal>
          <div className="mw-modal-backdrop" onClick={() => setFollowupOpen(false)} />
          <div className="mw-modal-panel mw-modal-lg">
            <div className="mw-modal-header">
              <div className="mw-modal-header-main">
                <div className="mw-modal-header-icon">
                  <i className="fa fa-clock" aria-hidden />
                </div>
                <div className="mw-modal-header-text-wrap">
                  <h2 className="mw-modal-title">Add Followup — {followupCustomer.customerName}</h2>
                </div>
              </div>
              <button type="button" className="mw-modal-close" onClick={() => setFollowupOpen(false)}>
                ×
              </button>
            </div>
            <form className="mw-modal-form" onSubmit={saveFollowup}>
              <div className="mw-modal-body">
                <div className="row g-3 mb-4">
                  <div className="col-md-6">
                    <label className="mw-label">Date &amp; Time <span className="req">*</span></label>
                    <input
                      type="datetime-local"
                      className="form-control mw-input"
                      required
                      value={fuForm.followupDatetime}
                      onChange={(e) => setFuForm((f) => ({ ...f, followupDatetime: e.target.value }))}
                    />
                  </div>
                  <div className="col-md-6">
                    <label className="mw-label">Followup Method <span className="req">*</span></label>
                    <select
                      className="form-select mw-input"
                      required
                      value={fuForm.followupMethod}
                      onChange={(e) => setFuForm((f) => ({ ...f, followupMethod: e.target.value }))}
                    >
                      {FOLLOWUP_METHODS.map((m) => (
                        <option key={m} value={m}>
                          {m}
                        </option>
                      ))}
                    </select>
                  </div>
                  <div className="col-md-6">
                    <label className="mw-label">Status <span className="req">*</span></label>
                    <select
                      className="form-select mw-input"
                      required
                      value={fuForm.followupStatus}
                      onChange={(e) => setFuForm((f) => ({ ...f, followupStatus: e.target.value }))}
                    >
                      {TEAM_STATUS.map((s) => (
                        <option key={s} value={s}>
                          {s}
                        </option>
                      ))}
                    </select>
                  </div>
                  <div className="col-12">
                    <label className="mw-label">Comments</label>
                    <textarea
                      className="form-control mw-input"
                      rows={2}
                      value={fuForm.followupComments}
                      onChange={(e) => setFuForm((f) => ({ ...f, followupComments: e.target.value }))}
                    />
                  </div>
                </div>

                <h3 className="mw-modal-section-title">Previous followups</h3>
                {followups.length === 0 ? (
                  <p className="mw-followup-empty-text">No followups yet.</p>
                ) : (
                  <div className="table-responsive">
                    <table className="table mw-followup-history-table">
                      <thead>
                        <tr>
                          <th>Date &amp; Time</th>
                          <th>Method</th>
                          <th>Status</th>
                          <th>Comments</th>
                        </tr>
                      </thead>
                      <tbody>
                        {pagedFollowups.map((f) => (
                          <tr key={f.id}>
                            <td>{dash(f.followupDatetimeDisplay)}</td>
                            <td>{f.followupMethod}</td>
                            <td>{f.followupStatus}</td>
                            <td>{dash(f.comments)}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                    {followupPager.showPager ? (
                      <TablePagination
                        total={followups.length}
                        page={followupPager.page}
                        pageSize={followupPager.pageSize}
                        onPageChange={followupPager.setPage}
                        onPageSizeChange={followupPager.setPageSize}
                      />
                    ) : null}
                  </div>
                )}
              </div>
              <div className="mw-modal-footer">
                <button type="button" className="mw-btn mw-btn-cancel" onClick={() => setFollowupOpen(false)}>
                  Cancel
                </button>
                <button type="submit" className="mw-btn mw-btn-accent">
                  Save Followup
                </button>
              </div>
            </form>
          </div>
        </div>
      ) : null}

      {/* WhatsApp share modal — matches PHP shareOfferModal */}
      {shareOpen && shareCustomer ? (
        <div className="mw-modal mw-modal-light share-offer-modal is-open" id="shareOfferModal" role="dialog" aria-modal>
          <div className="mw-modal-backdrop" onClick={() => setShareOpen(false)} />
          <div className="mw-modal-panel mw-modal-xl">
            <div className="mw-modal-header">
              <h2 className="mw-modal-title mw-modal-action-title">
                <i className="fa-brands fa-whatsapp" aria-hidden />{' '}
                <span>
                  {shareContext === 'whatsapp' ? 'Send WhatsApp Message' : 'Share Offer on WhatsApp'}
                </span>
              </h2>
              <button type="button" className="mw-modal-close" aria-label="Close" onClick={() => setShareOpen(false)}>
                <span aria-hidden>×</span>
              </button>
            </div>

            <div className="mw-modal-body">
              <div className="grid grid-cols-1 gap-4 lg:grid-cols-12 lg:gap-6">
                <div className="lg:col-span-7">
                  <label className="mb-2 block text-sm font-semibold text-slate-800" htmlFor="offerMessage">
                    Message
                  </label>
                  <div className="wa-message-field relative">
                    <textarea
                      id="offerMessage"
                      className="form-control min-h-[220px] w-full resize-y rounded-lg border border-slate-200 pb-7 text-sm leading-relaxed"
                      rows={12}
                      maxLength={2000}
                      placeholder="Your message…"
                      value={shareMsg}
                      onChange={(e) => setShareMsg(e.target.value)}
                    />
                    <div className="wa-char-count pointer-events-none absolute bottom-2 right-2.5 text-[11px] text-slate-400">
                      {shareMsg.length}/2000
                    </div>
                  </div>
                  <p className="mt-2 mb-0 text-xs text-slate-500">
                    Placeholders: [Business Name], [Area, City], [MiniWebsite Link], [Customer Name]
                  </p>
                  <div className="tracker-subtle-box mt-3 rounded-lg border border-[#d9e7ff] bg-[#f2f7ff] px-3 py-2.5 text-xs text-[#2c456e]">
                    <strong>Use templates for fast messaging</strong>
                    <ul className="mt-2 mb-0 list-disc pl-4">
                      <li>Save your custom message as template.</li>
                      <li>Set any template as default for quick use.</li>
                    </ul>
                  </div>
                  {templateNotice ? (
                    <p className="mt-2 mb-0 text-sm font-medium text-emerald-600">{templateNotice}</p>
                  ) : null}
                </div>

                <div className="lg:col-span-5">
                  <div className="wa-template-head mb-2.5">
                    <span className="wa-template-title block text-sm font-bold text-[#1a2744]">Choose Template</span>
                  </div>
                  <div id="waTemplateList" role="radiogroup" aria-label="Templates" className="space-y-2">
                    {waTemplateRows.map((item) => {
                      const active = selectedTemplateId === item.id
                      const isDefault = (waTemplates.defaultId || 'default') === item.id
                      const preview = (item.text || '').replace(/\s+/g, ' ').trim().slice(0, 80)
                      return (
                        <label
                          key={item.id}
                          className={`template-item flex w-full cursor-pointer items-start gap-2 rounded-lg border p-3 ${
                            active
                              ? 'border-[#6ea8ff] bg-[#eef5ff]'
                              : 'border-[#e2e7f2] bg-white hover:border-slate-300'
                          }`}
                        >
                          <input
                            type="radio"
                            name="waTemplateRadio"
                            className="mt-1"
                            checked={active}
                            onChange={() => selectTemplate(item.id)}
                          />
                          <span className="min-w-0 flex-1">
                            <span className="template-name-row flex items-center justify-between gap-2">
                              <span className="template-name text-[13px] font-bold text-[#0d4fbf]">{item.name}</span>
                              {isDefault ? (
                                <span className="template-default-badge rounded bg-[#28a745] px-1.5 py-0.5 text-[10px] font-bold text-white">
                                  DEFAULT
                                </span>
                              ) : null}
                            </span>
                            <div className="template-preview mt-1 text-xs leading-snug text-[#5c6b85]">
                              {preview || <span className="text-muted">Empty template</span>}
                            </div>
                          </span>
                        </label>
                      )
                    })}
                  </div>
                </div>
              </div>
            </div>

            <div className="mw-modal-footer wa-popup-footer flex flex-col items-stretch justify-end gap-2 md:flex-row md:items-center">
              <button type="button" className="btn btn-success order-1" onClick={sendWhatsApp}>
                <i className="fa-brands fa-whatsapp me-1" aria-hidden />
                Send on WhatsApp
              </button>
              <button type="button" className="btn btn-outline-primary order-2" onClick={saveSelectedTemplate}>
                Save Template
              </button>
              <button type="button" className="btn btn-outline-primary order-3" onClick={saveAsDefaultTemplate}>
                Save as Default
              </button>
              <button type="button" className="btn btn-danger order-4" onClick={() => setShareOpen(false)}>
                Cancel
              </button>
            </div>
          </div>
        </div>
      ) : null}
    </>
  )
}

export function CustomerTrackerRedirectPage() {
  return (
    <div className="mw-alert mw-alert-info">
      Customer Tracker has moved to <Link to="/customer-manager">Customer Manager</Link>.
    </div>
  )
}

export function GrowWithMwPage() {
  return (
    <>
      <div className="main-top mw-page-header mb-4">
        <h1 className="heading mw-page-title">Grow with MW</h1>
      </div>
      <div className="mw-card">
        <div className="mw-card-body">
          <p>Documentation hub is available on the live site while the public docs API is finalized.</p>
          <a className="mw-btn mw-btn-accent" href={siteUrl('user/grow-with-mw/')}>
            Open Grow with MW
          </a>
          <Link className="btn btn-link" to="/docs">
            Public docs
          </Link>
        </div>
      </div>
    </>
  )
}
