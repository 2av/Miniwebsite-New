import { useState } from 'react'
import { Link, NavLink, useLocation } from 'react-router-dom'
import { assetUrl, cn } from '@/lib/utils'

export function PublicHeader() {
  const location = useLocation()
  const [open, setOpen] = useState(false)
  const [earnOpen, setEarnOpen] = useState(false)

  const hideHomeAnchors =
    location.pathname === '/franchisee' || location.pathname === '/refer-and-earn'

  return (
    <header className="sticky top-0 z-30 border-b border-sb-border bg-white/95 backdrop-blur">
      <nav className="px-4">
        <div className="mx-auto flex h-16 max-w-[1140px] items-center justify-between gap-3">
          <Link to="/" className="shrink-0" onClick={() => setOpen(false)}>
            <img
              src={assetUrl('images/main-logo.png')}
              alt="ShareBizz"
              className="h-10 w-auto max-w-[180px]"
            />
          </Link>

          <button
            type="button"
            className="lg:hidden"
            aria-label="Toggle navigation"
            onClick={() => setOpen((v) => !v)}
          >
            <img
              src={assetUrl('images/navbar-img.png')}
              alt="Menu"
              width={30}
              height={30}
              className="h-[30px] w-[30px]"
            />
          </button>

          <div
            className={cn(
              'w-full basis-full lg:flex lg:w-auto lg:basis-auto lg:items-center',
              open ? 'block' : 'hidden lg:flex',
            )}
          >
            <ul className="ml-auto flex flex-col gap-1 py-3 lg:flex-row lg:items-center lg:gap-0 lg:py-0">
              {!hideHomeAnchors && (
                <>
                  <li className="lg:mx-2.5">
                    <a
                      href="/#demo-samples"
                      className="relative block py-2 text-[15px] font-medium text-sb-ink transition hover:text-sb-primary"
                      onClick={() => setOpen(false)}
                    >
                      Demo Samples
                    </a>
                  </li>
                  <li className="lg:mx-2.5">
                    <a
                      href="/#our-plans"
                      className="relative block py-2 text-[15px] font-medium text-sb-ink transition hover:text-sb-primary"
                      onClick={() => setOpen(false)}
                    >
                      Our Plans
                    </a>
                  </li>
                </>
              )}

              <li className="relative lg:mx-2.5">
                <button
                  type="button"
                  className="sb-btn-secondary inline-flex items-center gap-1 text-[15px]"
                  onClick={() => setEarnOpen((v) => !v)}
                >
                  Earn With Us <i className="fa fa-angle-double-down" />
                </button>
                {earnOpen && (
                  <div className="absolute left-0 z-40 mt-1 min-w-[200px] rounded-xl border border-sb-border bg-white py-2 shadow-lg">
                    <NavLink
                      to="/refer-and-earn"
                      className="block px-4 py-2 text-[15px] font-medium text-sb-ink hover:bg-sb-active hover:text-sb-primary"
                      onClick={() => {
                        setEarnOpen(false)
                        setOpen(false)
                      }}
                    >
                      Refer & Earn
                    </NavLink>
                    <NavLink
                      to="/franchisee"
                      className="block px-4 py-2 text-[15px] font-medium text-sb-ink hover:bg-sb-active hover:text-sb-primary"
                      onClick={() => {
                        setEarnOpen(false)
                        setOpen(false)
                      }}
                    >
                      Franchise Partner
                    </NavLink>
                  </div>
                )}
              </li>

              <li className="lg:mx-2.5">
                <NavLink
                  to="/login"
                  className="block py-2 text-[15px] font-medium text-sb-ink transition hover:text-sb-primary"
                  onClick={() => {
                    setEarnOpen(false)
                    setOpen(false)
                  }}
                >
                  Login
                </NavLink>
              </li>
              <li className="lg:ml-2">
                <NavLink to="/registration" className="sb-btn-primary text-[15px]" onClick={() => setOpen(false)}>
                  Get Started
                  <i className="fa fa-arrow-right" aria-hidden />
                </NavLink>
              </li>
            </ul>
          </div>
        </div>
      </nav>
    </header>
  )
}
