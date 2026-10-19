import { useEffect } from 'react'
import { Link } from 'react-router-dom'

type PortalPageHeaderProps = {
  title: string
  subtitle?: string
}

/** Same title row the dashboard uses: heading, optional line, and ShareBizz breadcrumb. */
export function PortalPageHeader({ title, subtitle }: PortalPageHeaderProps) {
  useEffect(() => {
    document.title = title
  }, [title])

  return (
    <div className="sb-dash-head">
      <div>
        <h1 className="sb-dash-title">{title}</h1>
        {subtitle ? <p className="sb-dash-sub">{subtitle}</p> : null}
      </div>
      <div>
        <nav className="sb-crumbs" aria-label="breadcrumb">
          <Link to="/dashboard">ShareBizz</Link>
          <span aria-hidden="true">/</span>
          <span className="is-current">{title}</span>
        </nav>
      </div>
    </div>
  )
}
