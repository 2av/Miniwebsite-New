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
    <header className="bg-white">
      <nav className="border-b border-transparent px-3">
        <div className="mx-auto flex max-w-[1140px] items-center justify-between gap-3 py-2">
          <Link to="/" className="shrink-0" onClick={() => setOpen(false)}>
            <img
              src={assetUrl('images/main-logo.png')}
              alt="ShareBizz"
              className="h-auto w-[200px] max-w-full"
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
                      className="relative block py-2 text-lg font-medium text-[var(--mw-navy)] transition hover:text-[var(--mw-gold)]"
                      onClick={() => setOpen(false)}
                    >
                      Demo Samples
                    </a>
                  </li>
                  <li className="lg:mx-2.5">
                    <a
                      href="/#our-plans"
                      className="relative block py-2 text-lg font-medium text-[var(--mw-navy)] transition hover:text-[var(--mw-gold)]"
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
                  className="mw-btn-primary inline-flex items-center gap-1 text-lg font-medium"
                  onClick={() => setEarnOpen((v) => !v)}
                >
                  Earn With Us <i className="fa fa-angle-double-down" />
                </button>
                {earnOpen && (
                  <div className="absolute left-0 z-40 mt-1 min-w-[200px] rounded-lg bg-[var(--mw-navy)] py-2 shadow-lg">
                    <NavLink
                      to="/refer-and-earn"
                      className="block px-4 py-2 text-lg font-medium text-white hover:bg-white/10"
                      onClick={() => {
                        setEarnOpen(false)
                        setOpen(false)
                      }}
                    >
                      Refer & Earn
                    </NavLink>
                    <NavLink
                      to="/franchisee"
                      className="block px-4 py-2 text-lg font-medium text-white hover:bg-white/10"
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
                  className="block py-2 text-lg font-medium text-[var(--mw-navy)] transition hover:text-[var(--mw-gold)]"
                  onClick={() => {
                    setEarnOpen(false)
                    setOpen(false)
                  }}
                >
                  Login
                </NavLink>
              </li>
            </ul>
          </div>
        </div>
      </nav>
    </header>
  )
}
