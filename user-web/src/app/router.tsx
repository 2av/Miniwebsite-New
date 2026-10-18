import { Navigate, Route, Routes, useParams } from 'react-router-dom'
import { PublicLayout } from '@/layouts/PublicLayout'
import { UserPortalLayout } from '@/layouts/UserPortalLayout'
import { SharebizzMaintenancePage } from '@/features/marketing/SharebizzMaintenancePage'
import { LoginPage } from '@/features/auth/LoginPage'
import { ForgotPasswordPage } from '@/features/auth/ForgotPasswordPage'
import { DashboardPage } from '@/features/dashboard/DashboardPage'
import { FranchiseeMarketingPage } from '@/features/marketing/FranchiseeMarketingPage'
import { ReferAndEarnPage } from '@/features/marketing/ReferAndEarnPage'
import { WebsiteDevelopmentPage } from '@/features/marketing/WebsiteDevelopmentPage'
import { NotFoundPage } from '@/features/marketing/NotFoundPage'
import { RegisterPage } from '@/features/registration/RegisterPage'
import { RegistrationSuccessPage } from '@/features/registration/RegistrationSuccessPage'
import { RegistrationHubPage } from '@/features/registration/RegistrationHubPage'
import { CmsContentPage } from '@/features/content/CmsContentPage'
import { AgreementPage } from '@/features/content/AgreementPage'
import { DocsRedirectPage } from '@/features/content/DocsRedirectPage'
import { ChangePasswordPage } from '@/features/portal/ChangePasswordPage'
import { ReferralPage } from '@/features/portal/ReferralPage'
import { KitPage } from '@/features/portal/KitPage'
import { VerificationPage } from '@/features/portal/VerificationPage'
import { IdCardPage } from '@/features/portal/IdCardPage'
import { InvoiceHistoryPage, InvoiceDetailPage } from '@/features/portal/InvoicePages'
import {
  CustomerManagerPage,
  CustomerTrackerRedirectPage,
  GrowWithMwPage,
} from '@/features/portal/CustomerManagerPage'
import { WebsiteBuilderLayout } from '@/features/website/WebsiteBuilderLayout'
import { BusinessNamePage } from '@/features/website/BusinessNamePage'
import { SelectThemePage } from '@/features/website/SelectThemePage'
import { CompanyDetailsPage } from '@/features/website/CompanyDetailsPage'
import {
  ServicesPage,
  ProductsPage,
  SpecialOffersPage,
  ImageGalleryPage,
} from '@/features/website/EntityPages'
import { VideosPage, PaymentDetailsPage, SocialLinksPage } from '@/features/website/SimpleStepPages'
import { PaymentBridgePage } from '@/features/payment/PaymentBridgePage'
import { DbProfilePaymentPage } from '@/features/payment/DbProfilePaymentPage'
import { PaymentSuccessPage } from '@/features/payment/PaymentSuccessPage'
import { PublicCardPage } from '@/features/public-card/PublicCardPage'
import { clearAuthSession, LOGIN_PATH } from '@/shared/auth/session'

function LogoutPage() {
  clearAuthSession()
  return <Navigate to={LOGIN_PATH} replace />
}

function DocsSlugPage() {
  const { slug } = useParams()
  return <DocsRedirectPage slug={slug} />
}

function InvoiceDetailRoute() {
  const { id } = useParams()
  return <InvoiceDetailPage invoiceId={Number(id || 0)} />
}

export function AppRouter() {
  return (
    <Routes>
      <Route path="n/:slug" element={<PublicCardPage />} />
      <Route path="demo/:slug" element={<PublicCardPage demo />} />

      <Route path="payment/pay" element={<PaymentBridgePage kind="pay" />} />
      <Route path="payment/miniwebsite" element={<DbProfilePaymentPage />} />
      <Route path="payment/db-profile" element={<DbProfilePaymentPage />} />
      <Route path="payment/joining-deal" element={<PaymentBridgePage kind="joining-deal" />} />
      <Route path="payment/joining-deal/success" element={<PaymentSuccessPage kind="joining-deal" />} />
      <Route path="payment/distributor/success" element={<PaymentSuccessPage kind="distributor" />} />

      <Route index element={<SharebizzMaintenancePage />} />

      <Route element={<PublicLayout />}>
        <Route path="refer-and-earn" element={<ReferAndEarnPage />} />
        <Route path="franchisee" element={<FranchiseeMarketingPage />} />
        <Route path="website-development" element={<WebsiteDevelopmentPage />} />
        <Route path="*" element={<NotFoundPage />} />
      </Route>

      <Route path="login" element={<LoginPage />} />
      <Route path="login/customer" element={<Navigate to={LOGIN_PATH} replace />} />
      <Route path="login/franchisee" element={<Navigate to={LOGIN_PATH} replace />} />
      <Route path="login/team" element={<Navigate to={LOGIN_PATH} replace />} />
      <Route path="login/forgot-password" element={<ForgotPasswordPage />} />
      <Route path="logout" element={<LogoutPage />} />

      <Route path="registration" element={<RegistrationHubPage />} />
      <Route path="registration/customer" element={<RegisterPage kind="customer" />} />
      <Route path="registration/customer/success" element={<RegistrationSuccessPage />} />
      <Route path="registration/franchisee" element={<RegisterPage kind="franchisee" />} />
      <Route path="registration/franchisee/success" element={<RegistrationSuccessPage />} />

      <Route path="privacy-policy" element={<CmsContentPage contentType="privacy_policy" />} />
      <Route path="terms-conditions" element={<CmsContentPage contentType="terms_conditions" />} />
      <Route
        path="franchise-agreement"
        element={
          <AgreementPage
            contentType="franchisee_agreement"
            paymentPath="franchise_agreement.php"
            agreeLabel="Continue to Payment"
          />
        }
      />
      <Route
        path="franchisee-distributer-agreement"
        element={
          <AgreementPage
            contentType="franchisee_distributer"
            paymentPath="franchisee-distributer-agreement.php"
            agreeLabel="Agree & Continue"
          />
        }
      />
      <Route path="mw-full-franchise-agreement" element={<CmsContentPage contentType="mw_full_franchise_agreement" />} />
      <Route
        path="mw-franchisee-operation-policy"
        element={<CmsContentPage contentType="mw_franchisee_operation_policy" />}
      />

      <Route path="docs" element={<DocsRedirectPage />} />
      <Route path="docs/:slug" element={<DocsSlugPage />} />

      <Route element={<UserPortalLayout />}>
        <Route path="dashboard" element={<DashboardPage />} />
        <Route path="dashboard/invoices" element={<InvoiceHistoryPage />} />
        <Route path="dashboard/invoices/:id" element={<InvoiceDetailRoute />} />
        <Route path="change-password" element={<ChangePasswordPage />} />
        <Route path="referral" element={<ReferralPage />} />
        <Route path="collaboration" element={<ReferralPage />} />
        <Route path="kit" element={<KitPage />} />
        <Route path="verification" element={<VerificationPage />} />
        <Route path="id-card" element={<IdCardPage />} />
        <Route path="idcard" element={<Navigate to="/id-card" replace />} />
        <Route path="customer-manager" element={<CustomerManagerPage />} />
        <Route path="customer-tracker" element={<CustomerTrackerRedirectPage />} />
        <Route path="grow-with-mw" element={<GrowWithMwPage />} />

        <Route path="website" element={<WebsiteBuilderLayout />}>
          <Route path="business-name" element={<BusinessNamePage />} />
          <Route path="select-theme" element={<SelectThemePage />} />
          <Route path="company-details" element={<CompanyDetailsPage />} />
          <Route path="services" element={<ServicesPage />} />
          <Route path="special-offers" element={<SpecialOffersPage />} />
          <Route path="products" element={<ProductsPage />} />
          <Route path="videos" element={<VideosPage />} />
          <Route path="image-gallery" element={<ImageGalleryPage />} />
          <Route path="payment-details" element={<PaymentDetailsPage />} />
          <Route path="social-links" element={<SocialLinksPage />} />
        </Route>
      </Route>
    </Routes>
  )
}
