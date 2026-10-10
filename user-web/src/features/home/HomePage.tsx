import { useEffect } from 'react'
import { Link } from 'react-router-dom'
import { FaqAccordion } from '@/components/FaqAccordion'
import { assetUrl } from '@/lib/utils'
import { initOwlCarousels, useSiteStyles } from '@/shared/site/useSiteStyles'

const DEMOS = [
  { img: '01.png', slug: 'Kaka-furnitures' },
  { img: '02.png', slug: 'Business-Assistant-' },
  { img: '03.png', slug: 'Ajay-Electronics' },
  { img: '04.png', slug: 'Riya-Beauty-Parlour' },
  { img: '05.png', slug: 'Agrawal-insurance' },
  { img: '06.png', slug: 'Money-India-Pvt-Ltd-' },
  { img: '07.png', slug: 'Dinesh-masale' },
]

const FEATURES = [
  { img: '01Clickcall.png', title: 'Click to Call', text: 'Your customers can contact you instantly by simply tapping on the call icon on your ShareBizz.' },
  { img: '02Clickwhatsapp.png', title: 'Click To WhatsApp', text: 'Through this ShareBizz, your customers can WhatsApp you directly without saving your number.' },
  { img: '03Clickemail.png', title: 'Click to Email', text: 'Your customers can send you an email with just one click directly from the ShareBizz.' },
  { img: '04GetDirection.png', title: 'Get Directions', text: 'People can navigate to your store using Google Maps from the ShareBizz.' },
  { img: '05savecontact.png', title: 'Save to Contacts', text: 'People can store all your contact information in their phone.' },
  { img: '06website.png', title: 'Website & Social Links', text: 'Customers can visit your website and social media pages through the ShareBizz.' },
  { img: '07share.png', title: 'Unlimited Share', text: 'You can share your ShareBizz as many times as you like on social media and WhatsApp.' },
  { img: '08easy.png', title: 'Easy To Update', text: 'You can change your details anytime as many times as you want.' },
  { img: '09photogallery.png', title: 'Photo Gallery', text: 'You can display your business realted images to your customers.' },
  { img: '10Youtubevideos.png', title: 'Youtube Videos', text: "Capture your customers' attention by featuring your business YouTube videos on your ShareBizz." },
  { img: '11Payment.png', title: 'Payment Info', text: 'Display your payment options to customers, such as Paytm, PhonePe, Google Pay, and bank account details (including QR code).' },
  { img: '12E-commerce.png', title: 'E-commerce', text: 'List your products or services with price in your ShareBizz.' },
  { img: '13qr-code-scan.png', title: 'QR Scan', text: 'Get a QR scan code of your ShareBizz to enhance your online presence.' },
  { img: '14visits.png', title: 'Visitation', text: 'You can track the number of unique visitors on your ShareBizz.' },
]

const WHY = [
  { img: '01_LaunchMinutes.gif', alt: 'Create in Minutes', text: 'Create in Minutes' },
  { img: '02_Notechnicalskills.gif', alt: 'No Technical Skills Required', text: 'No Technical Skills Required' },
  { img: '03_ReadyToUse.gif', alt: 'Ready To Use Templates', text: 'Ready To Use Templates', box: 'feature-box3' },
  { img: '04_MobileFriendly.gif', alt: 'Mobile Friendly', text: <>Mobile <br /> Friendly</>, box: 'feature-box4' },
  { img: '05_Affordable.gif', alt: 'Affordable Pricing', text: 'Affordable Pricing' },
  { img: '06_Reliable_support_17905424.gif', alt: 'Reliable Support', text: 'Reliable Support' },
]

const PLAN_FEATURES = [
  'Check Visitors Count',
  '60+ impressive themes',
  'Create Complete Profile',
  'Click to Call',
  'Click to Email',
  'Click to WhatsApp',
  'Connect all Social Links',
  'Share ShareBizz QR Code',
  'Add Products with Prices',
  'Add Services',
  'Add Videos',
  'Add Payment Details',
  'Unlimited Share',
  'Update Details Anytime',
]

const TESTIMONIALS = [
  { img: '01.png', name: 'Rahul Mehra', company: 'Mehra Consulting', text: '"ShareBizz transformed our online presence effortlessly! Their design and speed are unmatched!"' },
  { img: '02.png', name: 'Priya Sharma', company: 'Art Haven', text: '"Incredible service! The team delivered a stunning website that perfectly fits our brand."' },
  { img: '03.png', name: 'Amit Gupta', company: 'Gupta Groceries', text: '"Highly professional and efficient. ShareBizz made creating a website hassle-free and affordable for our small business."' },
  { img: '04.png', name: 'Sneha Kapoor', company: 'Eventify Solutions', text: '"ShareBizz delivered a sleek, user-friendly website in no time! Truly a game-changer for our business."' },
  { img: '05.jpg', name: 'Riya Ahuja', company: 'Riya beauty parlour', text: '"No fluff, just results. Got exactly what I needed-fast, good-looking, and reliable."' },
  { img: '06.jpg', name: 'Ajay Gupta', company: 'Ajay electronics', text: '"Perfect for small businesses like mine. Fast, affordable, and user-friendly-everything I needed in one place."' },
]

function demoUrl(slug: string) {
  return `/demo/${encodeURIComponent(slug)}`
}

export function HomePage() {
  useSiteStyles({ owl: true })

  useEffect(() => {
    const t = window.setTimeout(() => {
      void initOwlCarousels()
    }, 300)
    return () => window.clearTimeout(t)
  }, [])

  return (
    <main>
      <section className="banner">
        <div className="container">
          <div className="banner-wrap">
            <div className="banner-content">
              <h1 className="heading">
                Create your <span>SHAREBIZZ</span>
              </h1>
              <p>
                Your business deserves a powerful online presence that captures attention, drives engagement, and
                converts visitors into loyal customers.
              </p>
              <ul>
                <li>Are you struggling to have an online presence?</li>
                <li>Do you still use paper visiting cards?</li>
              </ul>
              <Link className="btn btn-primary d-lg-block d-none" to="/login" style={{ textDecoration: 'none' }}>
                CREATE YOUR SHAREBIZZ
              </Link>
            </div>
            <div className="banner-img">
              <img className="img-fluid" src={assetUrl('images/01Page01/Image.png')} alt="" />
            </div>
            <div className="banner-content d-lg-none d-md-block">
              <Link className="btn btn-primary" to="/login" style={{ textDecoration: 'none', color: '#002169' }}>
                CREATE YOUR SHAREBIZZ
              </Link>
            </div>
          </div>
        </div>
      </section>

      <section className="how-web-works">
        <div className="container">
          <div className="web-works">
            <h2 className="heading">How ShareBizz Works ?</h2>
            <div className="web-works-card">
              <div className="cards">
                <img className="img-fluid" src={assetUrl('images/02HowMiniWebsiteWork/create_01.png')} alt="" />
                <h3 className="card-heading">
                  CREATE YOUR <span>SHAREBIZZ</span>
                </h3>
                <p>Create your ShareBizz within minutes with impressive rich content &amp; design.</p>
              </div>
              <div className="cards">
                <img className="img-fluid" src={assetUrl('images/02HowMiniWebsiteWork/share_02.png')} alt="" />
                <h3 className="card-heading">
                  SHARE YOUR <span>SHAREBIZZ</span>
                </h3>
                <p>You can share your ShareBizz from anywhere &amp; anytime.</p>
              </div>
              <div className="cards">
                <img className="img-fluid" src={assetUrl('images/02HowMiniWebsiteWork/growth_11462571.png')} alt="" />
                <h3 className="card-heading">
                  SUCCEED WITH <span>SHAREBIZZ</span>
                </h3>
                <p>Attract more customers and boost your online presence with a professional ShareBizz.</p>
              </div>
            </div>
          </div>
        </div>
      </section>

      <section className="demo-samples" id="demo-samples">
        <div className="container">
          <div className="demo-samples-wrap">
            <div className="demo-heading-wrap">
              <img className="img-fluid" src={assetUrl('images/03Miniwebsitesamples/phone.png')} alt="" />
              <h2 className="heading"> ShareBizz Samples </h2>
            </div>
            <div className="screenshots-carousel owl-carousel owl-theme">
              {DEMOS.map((d) => (
                <div className="screenshots" key={d.slug}>
                  <img className="img-fluid" src={assetUrl(`images/03Miniwebsitesamples/${d.img}`)} alt="" />
                  <a className="btn btn-primary" href={demoUrl(d.slug)} target="_blank" rel="noreferrer" style={{ textDecoration: 'none', color: '#002169' }}>
                    VIEW DEMO
                  </a>
                </div>
              ))}
            </div>
          </div>
        </div>
      </section>

      <section className="Website-Features">
        <div className="container">
          <div className="Website-FeaturesWrap">
            <h2 className="heading">Our ShareBizz Features</h2>
            <div className="WebsiteFeatures">
              {FEATURES.map((f) => (
                <div className="cards" key={f.title}>
                  <img className="img-fluid" src={assetUrl(`images/04OurMiniwebsiteFeatures/${f.img}`)} alt="" />
                  <h3 className="card-heading">{f.title}</h3>
                  <p>{f.text}</p>
                </div>
              ))}
            </div>
          </div>
        </div>
      </section>

      <section className="go-digital">
        <div className="container">
          <div className="go-digital-wrap">
            <h2 className="heading">Go Digital</h2>
            <div className="info-card-wrap">
              <div className="info-card purple">
                <div>
                  <h3>
                    Say Goodbye to <span>Paper Visiting Cards</span>
                  </h3>
                  <p>Paper visiting cards are usually thrown away in dustbin, and no one cares about them.</p>
                </div>
                <img className="img-fluid" src={assetUrl('images/05GoDigital/01.png')} alt="Trash bin" />
              </div>
              <div className="info-card red">
                <img className="img-fluid" src={assetUrl('images/05GoDigital/02.png')} alt="ShareBizz" />
                <div>
                  <h3>Use ShareBizz</h3>
                  <p>It’s convenient, super affordable, and hassle-free.</p>
                </div>
              </div>
              <div className="info-card orange">
                <div>
                  <h3>Get More Attention</h3>
                  <p>You can get more attention than before. More attention means more customers, more profits.</p>
                </div>
                <img className="img-fluid" src={assetUrl('images/05GoDigital/03.png')} alt="More Attention" />
              </div>
            </div>
          </div>
        </div>
      </section>

      <section className="why-miniweb">
        <div className="container">
          <h2 className="heading">Why ShareBizz?</h2>
          <div className="features-container">
            {WHY.map((w) => (
              <div className={`feature-box ${w.box ?? ''}`} key={w.img}>
                <img className="img-fluid" src={assetUrl(`images/05AWhyMiniwebsite/${w.img}`)} alt={w.alt} />
                <span className="feature-text">{w.text}</span>
              </div>
            ))}
          </div>
        </div>
      </section>

      <section className="pricing" id="our-plans">
        <div className="container">
          <h2 className="heading">Choose Your Plan</h2>
          <div className="pricing-container">
            <div className="pricing-card">
              <div className="card purple-light">
                <h3 className="plan-1">Beginner Plan</h3>
                <p className="price">FREE</p>
                <p className="trail">
                  <span>For 7 days</span>
                </p>
                <p className="price">
                  <small>(For Each User)</small>
                </p>
                <Link className="btn-plan btn btn-primary" to="/login" style={{ textDecoration: 'none', color: '#002169' }}>
                  GET PLAN
                </Link>
              </div>
              <div className="card-content">
                <ul>
                  {PLAN_FEATURES.map((f) => (
                    <li key={f}>{f}</li>
                  ))}
                </ul>
              </div>
            </div>

            <div className="pricing-card">
              <div className="card red">
                <h3 className="plan-1">Professional Plan </h3>
                <p className="old-price">Rs 2000/-</p>
                <p className="price">
                  Rs 999/- <sub>per year</sub>
                  <small>(For Each ShareBizz)</small>
                </p>
                <Link className="btn-plan btn btn-primary" to="/login" style={{ textDecoration: 'none', color: '#002169' }}>
                  GET PLAN
                </Link>
              </div>
              <div className="card-content">
                <ul>
                  {PLAN_FEATURES.map((f) => (
                    <li key={`pro-${f}`}>{f}</li>
                  ))}
                </ul>
              </div>
            </div>
          </div>
        </div>
      </section>

      <section className="textimonial">
        <div className="container">
          <div className="textimonial-wrap">
            <h2 className="heading">What Our Customer Says</h2>
            <div className="testimonial-carousel owl-carousel owl-theme" id="testimonial-carousel">
              {TESTIMONIALS.map((t) => (
                <div className="testimonial-card" key={t.name}>
                  <img src={assetUrl(`images/07Testimonials/${t.img}`)} alt={t.name} />
                  <h3>
                    <strong>{t.name}</strong>({t.company})
                  </h3>
                  <div className="testimonial-text">{t.text}</div>
                </div>
              ))}
            </div>
          </div>
        </div>
      </section>

      <section className="franchise">
        <div className="container">
          <div className="franchise-section">
            <img src={assetUrl('images/08Lookingfornewwaystowork/01.png')} alt="Franchise" className="franchise-img" />
            <div className="franchise-text">
              <div className="text">
                <h2>
                  Looking For New Ways to Earn? <span />
                </h2>
              </div>
              <h5>Partner with us and launch your own ShareBizz business with our trusted Franchise model.</h5>
              <br />
              <Link to="/franchisee" className="btn btn-primary">
                GET YOUR FRANCHISE
              </Link>
            </div>
          </div>
        </div>
      </section>

      <section className="faq">
        <div className="container">
          <h2 className="heading">Frequently Asked Questions (FAQ)</h2>
          <FaqAccordion pageType="home" accordionId="homeFaq" />
        </div>
      </section>
    </main>
  )
}
