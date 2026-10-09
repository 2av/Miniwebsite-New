import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { assetUrl } from '@/lib/utils'

export function PublicFooter() {
  const [showTop, setShowTop] = useState(false)

  useEffect(() => {
    const onScroll = () => setShowTop(window.scrollY > 200)
    window.addEventListener('scroll', onScroll)
    return () => window.removeEventListener('scroll', onScroll)
  }, [])

  return (
    <>
      {showTop && (
        <button
          type="button"
          id="backToTop"
          onClick={() => window.scrollTo({ top: 0, behavior: 'smooth' })}
          className="fixed right-5 bottom-24 z-40 rounded-full bg-white p-2 shadow-md"
          aria-label="Back to top"
        >
          <img src={assetUrl('images/arrow.png')} className="h-[30px] w-[30px]" width={30} alt="" />
        </button>
      )}

      <div className="fixed right-5 bottom-5 z-40 flex flex-col gap-3">
        <a href="https://wa.me/919429693574" target="_blank" rel="noreferrer" id="whatsapp-btn">
          <img src={assetUrl('images/whatsapp.png')} className="h-[30px] w-[30px]" width={30} alt="WhatsApp" />
        </a>
        <a href="tel:+91-9429693574" id="call-btn">
          <img src={assetUrl('images/phone.png')} className="h-[30px] w-[30px]" width={30} alt="Call" />
        </a>
      </div>

      <footer className="footer bg-[var(--mw-navy)] text-white">
        <section className="main-foot py-10">
          <div className="mx-auto max-w-[1140px] px-4">
            <div className="grid gap-8 md:grid-cols-2">
              <div>
                <img
                  className="mb-8 w-[300px] max-w-full"
                  src={assetUrl('images/09Footer/Miniwebsite logo_1800x.png')}
                  alt="ShareBizz"
                />
                <p className="mb-3 text-base">
                  <a href="tel:+91-9429693574" className="inline-flex items-center gap-2 hover:underline">
                    <img src={assetUrl('images/09Footer/foot-call.png')} alt="" className="h-5 w-5" />
                    +91-9429693574
                  </a>
                </p>
                <p className="mb-3 text-base">
                  <a href="mailto:support@miniwebsite.in" className="inline-flex items-center gap-2 hover:underline">
                    <img src={assetUrl('images/09Footer/foot-mail.png')} alt="" className="h-5 w-5" />
                    support@miniwebsite.in
                  </a>
                </p>
                <p className="mb-3 flex items-start gap-2 text-base">
                  <img src={assetUrl('images/09Footer/foot-map.png')} alt="" className="mt-1 h-5 w-5" />
                  plot no 535 Sanjay colony sector 23 near madrasi mandir, Faridabad - 121005, Haryana, India
                </p>
              </div>

              <div>
                <h4 className="mb-2 text-xl font-bold text-white">
                  <strong>Post a comment</strong>
                </h4>
                <p className="mb-4 text-base text-white/90">
                  We are always available for you. Required fields are marked *
                </p>
                <form
                  className="space-y-3"
                  onSubmit={(e) => {
                    e.preventDefault()
                    // Wired in a later phase (contact API)
                  }}
                >
                  <div className="grid grid-cols-2 gap-3">
                    <input
                      className="w-full rounded-[5px] border-0 px-3 py-2.5 text-sm text-slate-900"
                      type="text"
                      name="first_name"
                      placeholder="First Name*"
                      required
                    />
                    <input
                      className="w-full rounded-[5px] border-0 px-3 py-2.5 text-sm text-slate-900"
                      type="text"
                      name="last_name"
                      placeholder="Last Name*"
                      required
                    />
                  </div>
                  <div className="grid grid-cols-2 gap-3">
                    <input
                      className="w-full rounded-[5px] border-0 px-3 py-2.5 text-sm text-slate-900"
                      type="email"
                      name="email"
                      placeholder="Email*"
                      required
                    />
                    <input
                      className="w-full rounded-[5px] border-0 px-3 py-2.5 text-sm text-slate-900"
                      type="text"
                      name="contact"
                      placeholder="Contact"
                      required
                    />
                  </div>
                  <textarea
                    className="w-full rounded-[5px] border-0 px-3 py-2.5 text-sm text-slate-900"
                    rows={3}
                    name="message"
                    placeholder="Any Message"
                  />
                  <div className="text-center">
                    <button type="submit" className="mw-btn-primary px-5 py-2 text-sm font-bold text-[var(--mw-navy)]">
                      SEND US MESSAGE
                    </button>
                  </div>
                </form>
              </div>
            </div>
          </div>
        </section>

        <div className="border-t border-white/10 py-4">
          <div className="mx-auto flex max-w-[1140px] flex-wrap items-center justify-between gap-3 px-4 text-sm">
            <p className="m-0">© {new Date().getFullYear()} ShareBizz</p>
            <div className="flex gap-4">
              <Link to="/privacy-policy" className="hover:underline">
                Privacy Policy
              </Link>
              <Link to="/terms-conditions" className="hover:underline">
                Terms & Conditions
              </Link>
            </div>
          </div>
        </div>
      </footer>
    </>
  )
}
