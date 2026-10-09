import { useCallback, useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { FaqAccordion } from '@/components/FaqAccordion'
import { apiGet, apiSend, ApiError } from '@/shared/api/client'
import { assetUrl } from '@/lib/utils'
import { useSiteStyles } from '@/shared/site/useSiteStyles'

const ADVANTAGES = [
  { img: '01_EasyStart.gif', text: 'Easy To Start' },
  { img: '02_Hassle.gif', text: 'Hassle Free Business Model' },
  { img: '03_Can Start.gif', text: 'Can Start In Just 2 Days', box: 'feature-box3' },
  { img: '04_Lesscompetitin.gif', text: 'Very Less Competition', box: 'feature-box4' },
  { img: '05_Affordable.gif', text: 'Huge Profit Margins' },
  { img: '06_Nominal.gif', text: 'Very Nominal Investment' },
  { img: '07_High.gif', text: 'High Demand Business', box: 'feature-box3' },
  { img: '08_Notechnicalskills.gif', text: 'No Technical Skills Required', box: 'feature-box4' },
  { img: '09_Reliable_support.gif', text: 'Reliable Support System' },
]

const WHATSAPP =
  'https://wa.me/9429693574?text=' +
  encodeURIComponent('Hi, I am interested to take franchise of ShareBizz, Please provide more details.')

export function FranchiseeMarketingPage() {
  useSiteStyles()
  const [name, setName] = useState('')
  const [email, setEmail] = useState('')
  const [contact, setContact] = useState('')
  const [address, setAddress] = useState('')
  const [message, setMessage] = useState('')
  const [captchaId, setCaptchaId] = useState('')
  const [challenge, setChallenge] = useState('')
  const [captchaInput, setCaptchaInput] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [success, setSuccess] = useState(false)
  const [busy, setBusy] = useState(false)

  const loadCaptcha = useCallback(async () => {
    try {
      const raw = await apiGet<Record<string, unknown>>('/api/v1/public/captcha')
      setCaptchaId(String(raw.captchaId ?? raw.CaptchaId ?? ''))
      setChallenge(String(raw.challenge ?? raw.Challenge ?? ''))
      setCaptchaInput('')
    } catch {
      setChallenge('')
    }
  }, [])

  useEffect(() => {
    void loadCaptcha()
  }, [loadCaptcha])

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError(null)
    setBusy(true)
    try {
      await apiSend('post', '/api/v1/public/franchise-enquiry', {
        name,
        email,
        contact,
        address,
        message,
        captchaId,
        captchaInput,
      })
      setSuccess(true)
      setName('')
      setEmail('')
      setContact('')
      setAddress('')
      setMessage('')
      await loadCaptcha()
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Failed to send enquiry')
      await loadCaptcha()
    } finally {
      setBusy(false)
    }
  }

  return (
    <main className="franchasee">
      <section className="banner">
        <div className="container">
          <div className="banner-wrap">
            <div className="banner-content">
              <h3>Start Earning with</h3>
              <h1 className="heading">ShareBizz</h1>
              <p>
                &quot;Every Business needs an online presence, and we make the easier than ever. As a ShareBizz
                franchisee you&apos;ll be part of a rapidly growing network that is transforming how a small businesses
                and enterprenures establish their digital footprints.&quot;
              </p>
              <a className="btn btn-primary" href={WHATSAPP} target="_blank" rel="noreferrer">
                Contact Us
              </a>
            </div>

            <div className="banner-form blue-bg">
              <h2 className="heading">Franchise Enquiry</h2>
              <small>Fill out the form, and we’ll respond asap.</small>
              <form onSubmit={onSubmit} autoComplete="off" id="enquiryForm">
                <div className="form-group">
                  <input
                    type="text"
                    name="name"
                    placeholder="Name*"
                    className="form-control"
                    required
                    pattern="[A-Za-z\s]+"
                    title="Only letters and spaces allowed"
                    value={name}
                    onChange={(e) => setName(e.target.value)}
                  />
                </div>
                <div className="form-group">
                  <input
                    type="email"
                    name="email"
                    placeholder="Email Id*"
                    className="form-control"
                    required
                    value={email}
                    onChange={(e) => setEmail(e.target.value)}
                  />
                </div>
                <div className="form-group">
                  <input
                    type="tel"
                    name="contact"
                    placeholder="Contact*"
                    className="form-control"
                    required
                    pattern="\d{10}"
                    title="Enter 10-digit mobile number"
                    value={contact}
                    onChange={(e) => setContact(e.target.value)}
                  />
                </div>
                <div className="form-group">
                  <input
                    type="text"
                    name="address"
                    placeholder="Your Address"
                    className="form-control"
                    value={address}
                    onChange={(e) => setAddress(e.target.value)}
                  />
                </div>
                <div className="form-group">
                  <textarea
                    name="message"
                    placeholder="Any Message (Optional)"
                    className="form-control"
                    value={message}
                    onChange={(e) => setMessage(e.target.value)}
                  />
                </div>
                <div className="form-group">
                  <div className="captcha-wrapper">
                    <input
                      type="text"
                      name="captcha"
                      className="form-control"
                      required
                      placeholder="Enter the code shown below"
                      style={{ marginBottom: 10 }}
                      value={captchaInput}
                      onChange={(e) => setCaptchaInput(e.target.value)}
                    />
                    <div className="captcha-container" style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
                      <div
                        style={{
                          background: '#fff',
                          color: '#002169',
                          fontWeight: 700,
                          letterSpacing: 4,
                          padding: '8px 16px',
                          borderRadius: 6,
                          fontSize: 20,
                          fontFamily: 'monospace',
                        }}
                      >
                        {challenge || '······'}
                      </div>
                      <button type="button" className="refresh-captcha" onClick={() => void loadCaptcha()}>
                        <i className="fa fa-refresh" /> Refresh
                      </button>
                    </div>
                    {error && <div className="error-message">{error}</div>}
                  </div>
                </div>
                <input className="btn btn-primary" type="submit" value={busy ? 'SENDING...' : 'SEND'} disabled={busy} />
              </form>
            </div>
          </div>
        </div>
      </section>

      <section className="why-miniweb">
        <div className="container">
          <h2 className="heading">
            <span className="theme-color">ShareBizz Franchise</span> - Advantages
          </h2>
          <div className="features-container">
            {ADVANTAGES.map((a) => (
              <div className={`feature-box ${a.box ?? ''}`} key={a.img}>
                <img className="img-fluid" src={assetUrl(`images/05AWhyMiniwebsite/${a.img}`)} alt={a.text} />
                <span className="feature-text">{a.text}</span>
              </div>
            ))}
          </div>
        </div>
      </section>

      <section className="textimonial">
        <div className="container">
          <div className="textimonial-wrap">
            <h2 className="heading blue-bg">
              How to Become - <span className="theme-color">Our Franchise Partner</span>
            </h2>
            <ul>
              <li>
                <img className="img-fluid" src={assetUrl('images/03Howtobecome/icon.png')} alt="" />
              </li>
              <li>
                <img className="img-fluid" src={assetUrl('images/03Howtobecome/icon.png')} alt="" />
              </li>
              <li>
                <img className="img-fluid" src={assetUrl('images/03Howtobecome/icon.png')} alt="" />
              </li>
            </ul>
            <div className="testimonial-carousel">
              <div className="testimonial-card">
                <img className="img-fluid" src={assetUrl('images/03Howtobecome/info.gif')} alt="Fill the Enquiry Form or Call Us" />
                <h3>Fill the Enquiry Form or Call Us </h3>
              </div>
              <div className="testimonial-card">
                <img className="img-fluid" src={assetUrl('images/03Howtobecome/assign.gif')} alt="Do Simple Formalities" />
                <h3>Do Simple Formalities </h3>
              </div>
              <div className="testimonial-card">
                <img
                  className="img-fluid"
                  src={assetUrl('images/03Howtobecome/strategic-alliance.gif')}
                  alt="Become Our Franchise Partner"
                />
                <h3>Become Our Franchise Partner</h3>
              </div>
            </div>
            <div style={{ marginTop: 24, textAlign: 'center' }}>
              <Link to="/registration/franchisee" className="btn btn-primary">
                Register as Franchisee
              </Link>
            </div>
          </div>
        </div>
      </section>

      <section className="faq">
        <div className="container">
          <h2 className="heading">Frequently Asked Questions (FAQ)</h2>
          <FaqAccordion pageType="franchise" accordionId="franchiseFaq" />
        </div>
      </section>

      {success && (
        <div
          style={{
            display: 'block',
            position: 'fixed',
            zIndex: 9999,
            left: 0,
            top: 0,
            width: '100%',
            height: '100%',
            overflow: 'auto',
            backgroundColor: 'rgba(0,0,0,0.4)',
          }}
        >
          <div
            style={{
              backgroundColor: 'white',
              margin: '15% auto',
              padding: 20,
              border: '1px solid #888',
              width: '80%',
              maxWidth: 500,
              borderRadius: 5,
              textAlign: 'center',
            }}
          >
            <div style={{ marginBottom: 15 }}>
              <i className="fa fa-check-circle" style={{ fontSize: 48, color: '#28a745' }} />
            </div>
            <h4 style={{ marginBottom: 15, color: '#155724' }}>Enquiry Sent Successfully!</h4>
            <p style={{ color: 'black' }}>
              Thank you for your interest in our franchise. We will get back to you as soon as possible.
            </p>
            <button
              type="button"
              onClick={() => setSuccess(false)}
              style={{
                marginTop: 15,
                padding: '8px 16px',
                backgroundColor: '#28a745',
                color: 'white',
                border: 'none',
                borderRadius: 4,
                cursor: 'pointer',
              }}
            >
              Close
            </button>
          </div>
        </div>
      )}
    </main>
  )
}
