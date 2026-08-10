import { Link } from 'react-router-dom'
import { FaqAccordion } from '@/components/FaqAccordion'
import { assetUrl } from '@/lib/utils'
import { useSiteStyles } from '@/shared/site/useSiteStyles'

const referAsset = (path: string) => assetUrl(`images/REFER & EARN/${path}`)

export function ReferAndEarnPage() {
  useSiteStyles()

  return (
    <main>
      <section className="banner">
        <div className="container">
          <div className="banner-wrap">
            <div className="banner-content">
              <h3>Enjoy Unlimited</h3>
              <h1 className="heading Referral_Reward_text">Referral Rewards !</h1>
              <Link to="/login/customer" className="btn btn-primary" style={{ width: 'max-content', textDecoration: 'none', color: '#0a0a0b' }}>
                Start Referring
              </Link>
            </div>
            <div className="banner-img">
              <div className="icon-facebook">
                <img className="img-fluid" src={referAsset('01 Start Referring/facebook.png')} alt="" />
              </div>
              <div className="icon-wallet">
                <img className="img-fluid" src={referAsset('01 Start Referring/wallet.png')} alt="" />
              </div>
              <div className="main-img">
                <img className="img-fluid" src={referAsset('01 Start Referring/img.png')} alt="" />
              </div>
              <div className="icon-wahtsapp">
                <img className="img-fluid" src={referAsset('01 Start Referring/wahtsapp.png')} alt="" />
              </div>
              <div className="icon-sahre">
                <img className="img-fluid" src={referAsset('01 Start Referring/sahre.png')} alt="" />
              </div>
            </div>
          </div>
        </div>
      </section>

      <section className="why-miniweb">
        <div className="container">
          <h2 className="heading">
            Benefits of <span className="theme-color">Refer &amp; Earn </span> Program
          </h2>
          <img className="img-fluid referEarnImageDesktop" src={referAsset('02 Benefits of program/Referandearn.jpg')} alt="" />
          <img className="img-fluid referEarnImageMobile" src={referAsset('02 Benefits of program/Referearn-mobile.jpg')} alt="" />
        </div>
      </section>

      <section className="textimonial">
        <div className="container">
          <div className="textimonial-wrap">
            <h2 className="heading blue-bg">
              How - <span className="theme-color">Refer &amp; Earn </span>Works?
            </h2>
            <div className="refer_earn_works_desktop">
              <ul>
                <li>
                  <img className="img-fluid" src={referAsset('03 How refer and earn works/icon.png')} alt="" />
                </li>
                <li>
                  <img className="img-fluid" src={referAsset('03 How refer and earn works/icon.png')} alt="" />
                </li>
                <li>
                  <img className="img-fluid" src={referAsset('03 How refer and earn works/icon.png')} alt="" />
                </li>
              </ul>
              <div className="testimonial-carousel">
                <div className="testimonial-card">
                  <img
                    className="img-fluid"
                    src={referAsset('03 How refer and earn works/01 create website.gif')}
                    alt="Create your Mini Website"
                  />
                  <h3>
                    Create your <br />
                    Mini Website
                  </h3>
                </div>
                <div className="testimonial-card">
                  <img
                    className="img-fluid"
                    src={referAsset('03 How refer and earn works/02 Share.gif')}
                    alt="Share referral link"
                  />
                  <h3>Share your referral link with businesses, working professionals and friends</h3>
                </div>
                <div className="testimonial-card">
                  <img className="img-fluid" src={referAsset('03 How refer and earn works/03 Earn.gif')} alt="Earn" />
                  <h3>Earn Rs. 250/- when someone subscribe to our paid plan using your referral link</h3>
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>

      <section className="faq">
        <div className="container">
          <h2 className="heading">Frequently Asked Questions (FAQ)</h2>
          <FaqAccordion pageType="refer_earn" accordionId="referFaq" />
        </div>
      </section>
    </main>
  )
}
