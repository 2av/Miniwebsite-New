import { useEffect, useMemo, useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { apiGet } from '@/shared/api/client'
import { useAuthSession } from '@/shared/auth/useAuthSession'

type Folder = {
  id: number
  title: string
  status: string
  itemCount: number
  subfolderCount: number
}
type Item = {
  id: number
  title: string
  type: string
  status: string
  url: string
  filePath: string
  videoUrl: string | null
}
type Crumb = { id: number; title: string }

/**
 * Map menu ?kit= param → DB category (matches user/kit/index.php).
 */
function resolveKitCategory(
  kitParam: string | null,
  role: string,
  collaborationEnabled: boolean,
): string {
  const r = (role || '').toUpperCase()
  const kit = (kitParam || '').trim()

  if (r === 'FRANCHISEE') return 'marketing'
  if (r === 'TEAM') return kit === 'franchise_sales' ? 'marketing' : 'sales'
  if (collaborationEnabled) return kit === 'mw_sales' ? 'sales' : 'marketing'
  return 'sales'
}

function pageHeading(category: string, kitParam: string | null, role: string): string {
  const r = (role || '').toUpperCase()
  const kit = (kitParam || '').trim()
  if (kit === 'franchise_sales') return 'Franchisee Sales Kit'
  if (category === 'marketing' && r === 'CUSTOMER') return 'Creator Kit'
  if (category === 'marketing') return 'MW Sales Kit'
  if (category === 'sales') return r === 'CUSTOMER' && !kit ? 'Sales Kit' : 'MW Sales Kit'
  return 'Kit'
}

function WinFolderIcon() {
  return (
    <svg className="win-folder-svg" viewBox="0 0 48 40" xmlns="http://www.w3.org/2000/svg" aria-hidden>
      <path d="M3 6a3 3 0 0 1 3-3h11.2a3 3 0 0 1 2.1.9L23 7h19a3 3 0 0 1 3 3v3H3z" fill="#E8A33D" />
      <path d="M3 11a3 3 0 0 1 3-3h36a3 3 0 0 1 3 3v23a3 3 0 0 1-3 3H6a3 3 0 0 1-3-3z" fill="#FBC55B" />
      <path d="M3 12h42v2H3z" fill="#ffffff" opacity="0.25" />
    </svg>
  )
}

function fileIconClass(path: string) {
  const ext = (path || '').split('.').pop()?.toLowerCase() || ''
  const map: Record<string, string> = {
    pdf: 'fa-file-pdf',
    doc: 'fa-file-word',
    docx: 'fa-file-word',
    xls: 'fa-file-excel',
    xlsx: 'fa-file-excel',
    ppt: 'fa-file-powerpoint',
    pptx: 'fa-file-powerpoint',
    zip: 'fa-file-archive',
    rar: 'fa-file-archive',
    txt: 'fa-file-alt',
    mp4: 'fa-file-video',
    avi: 'fa-file-video',
    mov: 'fa-file-video',
    mp3: 'fa-file-audio',
    wav: 'fa-file-audio',
  }
  return map[ext] || 'fa-file'
}

function youtubeId(url: string) {
  const m = url.match(/(youtu\.be\/|v=)([^&]+)/)
  return m?.[2] || ''
}

export function KitPage() {
  const session = useAuthSession()
  const [params] = useSearchParams()
  const kitParam = params.get('kit')
  const role = session?.role || ''
  const collaborationEnabled = Boolean(session?.collaborationEnabled)

  const category = useMemo(
    () => resolveKitCategory(kitParam, role, collaborationEnabled),
    [kitParam, role, collaborationEnabled],
  )

  const [folderId, setFolderId] = useState(0)
  const [folders, setFolders] = useState<Folder[]>([])
  const [items, setItems] = useState<Item[]>([])
  const [apiLabel, setApiLabel] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)
  const [trail, setTrail] = useState<Crumb[]>([])

  const heading = useMemo(
    () => pageHeading(category, kitParam, role) || apiLabel || 'Kit',
    [category, kitParam, role, apiLabel],
  )

  // Reset explorer when switching MW ↔ Franchisee kit
  useEffect(() => {
    setFolderId(0)
    setTrail([])
    setFolders([])
    setItems([])
    setError(null)
  }, [category, kitParam])

  useEffect(() => {
    let cancelled = false
    ;(async () => {
      setLoading(true)
      try {
        const raw = await apiGet<Record<string, unknown>>('/api/v1/portal/kit', {
          category,
          folderId,
        })
        if (cancelled) return
        setApiLabel(String(raw.categoryLabel ?? raw.CategoryLabel ?? ''))
        const f = ((raw.subfolders ?? raw.Subfolders ?? raw.folders ?? raw.Folders) as unknown[]) || []
        const i = ((raw.items ?? raw.Items) as unknown[]) || []
        setFolders(
          f
            .map((x) => {
              const o = (x ?? {}) as Record<string, unknown>
              return {
                id: Number(o.id ?? o.Id),
                title: String(o.title ?? o.Title ?? 'Folder'),
                status: String(o.status ?? o.Status ?? 'active').toLowerCase(),
                itemCount: Number(o.directItemCount ?? o.DirectItemCount ?? o.itemCount ?? 0),
                subfolderCount: Number(o.subfolderCount ?? o.SubfolderCount ?? 0),
              }
            })
            .filter((x) => x.id > 0 && x.status !== 'inactive'),
        )
        setItems(
          i
            .map((x) => {
              const o = (x ?? {}) as Record<string, unknown>
              return {
                id: Number(o.id ?? o.Id),
                title: String(o.title ?? o.Title ?? 'Item'),
                type: String(o.type ?? o.Type ?? '').toLowerCase(),
                status: String(o.status ?? o.Status ?? 'active').toLowerCase(),
                url: String(o.fileUrl ?? o.FileUrl ?? o.url ?? o.Url ?? ''),
                filePath: String(o.filePath ?? o.FilePath ?? ''),
                videoUrl: (o.videoUrl ?? o.VideoUrl) as string | null,
              }
            })
            .filter((x) => x.id > 0 && x.status !== 'inactive'),
        )
        const crumbs = ((raw.breadcrumb ?? raw.Breadcrumb) as unknown[]) || []
        setTrail(
          crumbs
            .map((c) => {
              const o = (c ?? {}) as Record<string, unknown>
              const id = Number(o.id ?? o.Id ?? 0)
              const title = String(o.title ?? o.Title ?? '')
              return { id, title }
            })
            .filter((c) => c.id > 0 && c.title.toLowerCase() !== 'root'),
        )
      } catch (e) {
        if (!cancelled) setError(e instanceof Error ? e.message : 'Failed to load kit')
      } finally {
        if (!cancelled) setLoading(false)
      }
    })()
    return () => {
      cancelled = true
    }
  }, [category, folderId])

  function openFolder(id: number) {
    setFolderId(id)
  }

  function goHome() {
    setFolderId(0)
    setTrail([])
  }

  function goCrumb(id: number) {
    setFolderId(id)
  }

  const images = items.filter((x) => x.type === 'image')
  const videos = items.filter((x) => x.type === 'video')
  const files = items.filter((x) => x.type === 'file')
  const other = items.filter((x) => !['image', 'video', 'file'].includes(x.type))
  const empty = !loading && folders.length === 0 && items.length === 0 && !error

  return (
    <>
      <div className="main-top mw-page-header mb-4">
        <span className="heading mw-page-title">{heading}</span>
        <nav aria-label="breadcrumb">
          <ol className="breadcrumb mw-breadcrumb">
            <li className="breadcrumb-item">
              <Link to="/dashboard">Mini Website</Link>
            </li>
            <li className="breadcrumb-item active" aria-current="page">
              {heading}
            </li>
          </ol>
        </nav>
      </div>

      <div className="card mb-4">
        <div className="card-body">
          <p className="text-muted mb-4 sub_title">
            You can download these resources &amp; share with your customers which will help you grow.
          </p>

          {error && <div className="alert alert-danger">{error}</div>}

          <nav className="kit-explorer-bar" aria-label="Folder navigation">
            <button type="button" className="kit-explorer-up" title="Back to kit home" onClick={goHome}>
              <i className="fa fa-home" aria-hidden />
            </button>
            <ol className="kit-explorer-breadcrumb mb-0">
              <li>
                <button type="button" className="kit-crumb-btn" onClick={goHome}>
                  {heading}
                </button>
              </li>
              {trail.map((t) => (
                <li key={t.id}>
                  <button type="button" className="kit-crumb-btn" onClick={() => goCrumb(t.id)}>
                    {t.title}
                  </button>
                </li>
              ))}
            </ol>
          </nav>

          {loading && <p className="text-muted mb-0">Loading…</p>}

          {!loading && folders.length > 0 && (
            <div className="kit-explorer-folders row g-3 mb-4">
              {folders.map((f) => {
                const count = f.itemCount
                const countLabel = `${count} ${count === 1 ? 'item' : 'items'}`
                const detailParts = [countLabel]
                if (f.subfolderCount > 0) {
                  detailParts.push(
                    `${f.subfolderCount} ${f.subfolderCount === 1 ? 'folder' : 'folders'}`,
                  )
                }
                return (
                  <div className="col-6 col-sm-4 col-md-3 col-lg-2" key={f.id}>
                    <div
                      className="kit-folder-tile"
                      role="button"
                      tabIndex={0}
                      title={f.title}
                      onClick={() => openFolder(f.id)}
                      onKeyDown={(e) => {
                        if (e.key === 'Enter' || e.key === ' ') {
                          e.preventDefault()
                          openFolder(f.id)
                        }
                      }}
                    >
                      <span className="kit-folder-icon" aria-hidden>
                        <WinFolderIcon />
                      </span>
                      <span className="kit-folder-name">{f.title}</span>
                      <span className="kit-folder-count">{detailParts.join(' · ')}</span>
                    </div>
                  </div>
                )
              })}
            </div>
          )}

          {!loading && images.length > 0 && (
            <div className="row mb-5">
              <div className="col-12">
                <h4 className="mb-3 font22">Promotional Images</h4>
                <div className="row">
                  {images.map((image) => (
                    <div className="col-md-3 col-sm-6 mb-4" key={image.id}>
                      <div>
                        {image.url ? (
                          <img
                            src={image.url}
                            className="card-img-top"
                            style={{ height: 200, objectFit: 'cover', width: '100%' }}
                            alt={image.title || 'Promotional Image'}
                          />
                        ) : null}
                        <div
                          className="mt-auto d-flex"
                          style={{ padding: 10, justifyContent: 'space-between' }}
                        >
                          <h6 className="card-title mb-0 bottom_title">
                            {image.title || 'Promotional Image'}
                          </h6>
                          {image.url ? (
                            <a href={image.url} download title="Download">
                              <i className="fa fa-download" />
                            </a>
                          ) : null}
                        </div>
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            </div>
          )}

          {!loading && videos.length > 0 && (
            <div className="row mb-5">
              <div className="col-12">
                <h4 className="heading">Videos</h4>
                <div className="row">
                  {videos.map((video) => {
                    const yt = video.videoUrl ? youtubeId(video.videoUrl) : ''
                    return (
                      <div className="col-lg-3 col-md-4 col-sm-6 mb-4" key={video.id}>
                        <div className="card h-100">
                          <div className="card-body p-2 video_section">
                            {video.url ? (
                              <video controls style={{ width: '100%', borderRadius: 8 }}>
                                <source src={video.url} />
                              </video>
                            ) : yt ? (
                              <div className="ratio ratio-16x9">
                                <iframe
                                  src={`https://www.youtube.com/embed/${yt}`}
                                  allowFullScreen
                                  title={video.title}
                                />
                              </div>
                            ) : video.videoUrl ? (
                              <a href={video.videoUrl} target="_blank" rel="noreferrer">
                                Open video
                              </a>
                            ) : (
                              <p className="text-danger text-center mb-0">Unsupported video</p>
                            )}
                          </div>
                        </div>
                      </div>
                    )
                  })}
                </div>
              </div>
            </div>
          )}

          {!loading && files.length > 0 && (
            <div className="row mb-5">
              <div className="col-12">
                <h4 className="heading">Downloadable Files</h4>
                <div className="row">
                  {files.map((file) => {
                    const ext = (file.filePath || file.url).split('.').pop()?.toUpperCase() || 'FILE'
                    return (
                      <div
                        className="col-md-4 col-sm-6 mb-4 small_device_center downloadfileSection"
                        key={file.id}
                      >
                        <div className="card height-100">
                          <div className="card-body text-center downloded_files">
                            <div className="mb-3">
                              <i
                                className={`fa ${fileIconClass(file.filePath || file.url)} fa-3x text-primary mb-2`}
                              />
                            </div>
                            <h6 className="card-title">{file.title || 'Downloadable File'}</h6>
                            <p className="card-text text-muted small">{ext} File</p>
                            {file.url ? (
                              <a href={file.url} download className="btn last_download btn-sm">
                                <i className="fa fa-download me-1" />
                                <span>Download</span>
                              </a>
                            ) : (
                              <span className="text-muted">No file</span>
                            )}
                          </div>
                        </div>
                      </div>
                    )
                  })}
                </div>
              </div>
            </div>
          )}

          {!loading && other.length > 0 && (
            <div className="row">
              {other.map((item) => (
                <div className="col-md-3 mb-3" key={item.id}>
                  <div className="card p-3 h-100">
                    <div className="fw-semibold mb-2">{item.title}</div>
                    {item.videoUrl || item.url ? (
                      <a href={item.videoUrl || item.url} target="_blank" rel="noreferrer">
                        Open
                      </a>
                    ) : (
                      <span className="text-muted">No file</span>
                    )}
                  </div>
                </div>
              ))}
            </div>
          )}

          {empty && <p className="text-muted mb-0">This folder is empty.</p>}
        </div>
      </div>
    </>
  )
}
