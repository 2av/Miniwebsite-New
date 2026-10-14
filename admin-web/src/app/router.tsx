import { Navigate, Route, Routes, useLocation } from 'react-router-dom'
import { AdminLayout } from '@/layouts/AdminLayout'
import { DashboardPage } from '@/features/dashboard/DashboardPage'
import { ManageUsersPage } from '@/features/manage-users/ManageUsersPage'
import { ManageCardsPage } from '@/features/manage-cards/ManageCardsPage'
import { FranchiseDistributorsPage } from '@/features/franchise-distributors/FranchiseDistributorsPage'
import { FranchiseesPage } from '@/features/franchisees/FranchiseesPage'
import { AllOrdersPage } from '@/features/all-orders/AllOrdersPage'
import { ManageReferralsPage } from '@/features/manage-referrals/ManageReferralsPage'
import { CouponsPage } from '@/features/coupons/CouponsPage'
import { ManagePricingPage } from '@/features/manage-pricing/ManagePricingPage'
import { RechargeWalletPage } from '@/features/wallet-recharge/RechargeWalletPage'
import { ManageEmailTemplatesPage } from '@/features/email-templates/ManageEmailTemplatesPage'
import { ManageFaqsPage } from '@/features/manage-faqs/ManageFaqsPage'
import { ManageContentPage } from '@/features/manage-content/ManageContentPage'
import { ManageCategoriesPage } from '@/features/manage-categories/ManageCategoriesPage'
import { ManageTeamsPage } from '@/features/manage-teams/ManageTeamsPage'
import { GrowWithMwPagesPage } from '@/features/grow-with-mw/GrowWithMwPagesPage'
import { GrowWithMwSectionsPage } from '@/features/grow-with-mw/GrowWithMwSectionsPage'
import { GrowWithMwMediaPage } from '@/features/grow-with-mw/GrowWithMwMediaPage'
import { GrowWithMwPageEditorPage } from '@/features/grow-with-mw/GrowWithMwPageEditorPage'
import { KitManagementInvalidCategory, KitManagementRedirect } from '@/features/kit-management/KitManagementPage'
import { RoleManagementPage } from '@/features/role-management/RoleManagementPage'
import { SalesChannelPartnerRoute } from '@/features/sales-channel-partners/SalesChannelPartnerPage'
import { LoginPage } from '@/features/auth/LoginPage'
import { RequireAuth } from '@/features/auth/RequireAuth'
import { PlaceholderPage } from '@/features/common/PlaceholderPage'

function RedirectWithSearch({ to }: { to: string }) {
  const { search } = useLocation()
  return <Navigate to={`${to}${search}`} replace />
}

export function AppRouter() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route
        element={
          <RequireAuth>
            <AdminLayout />
          </RequireAuth>
        }
      >
        <Route index element={<DashboardPage />} />
        <Route path="sb-user-details" element={<ManageUsersPage />} />
        <Route path="user-details" element={<Navigate to="/sb-user-details" replace />} />
        <Route path="manage-users" element={<Navigate to="/sb-user-details" replace />} />
        <Route path="db-profile-details" element={<ManageCardsPage />} />
        <Route path="sb-profile-details" element={<RedirectWithSearch to="/db-profile-details" />} />
        <Route path="miniwebsite-details" element={<RedirectWithSearch to="/db-profile-details" />} />
        <Route path="franchisee-details" element={<FranchiseesPage />} />
        <Route path="franchise-details" element={<Navigate to="/franchisee-details" replace />} />
        <Route path="franchisee-distributor-details" element={<FranchiseDistributorsPage />} />
        <Route path="sales-channel/influencer-partner" element={<FranchiseDistributorsPage />} />
        <Route path="all-orders" element={<AllOrdersPage />} />
        <Route path="manage-referrals" element={<ManageReferralsPage />} />
        <Route path="coupons" element={<CouponsPage />} />
        <Route path="manage-deals" element={<Navigate to="/coupons" replace />} />
        <Route path="deals" element={<Navigate to="/coupons" replace />} />
        <Route path="manage-pricing" element={<ManagePricingPage />} />
        <Route path="pricing" element={<Navigate to="/manage-pricing" replace />} />
        <Route path="recharge-wallet" element={<RechargeWalletPage />} />
        <Route path="add-money" element={<Navigate to="/recharge-wallet" replace />} />
        <Route path="manage-faqs" element={<ManageFaqsPage />} />
        <Route path="manage-faq" element={<Navigate to="/manage-faqs" replace />} />
        <Route path="email-templates" element={<ManageEmailTemplatesPage />} />
        <Route path="manage-email-templates" element={<Navigate to="/email-templates" replace />} />
        <Route path="manage-categories" element={<ManageCategoriesPage />} />
        <Route path="sales-channel-partner" element={<Navigate to="/sales-channel-partner/franchisee-partner" replace />} />
        <Route path="sales-channel-partner/:slug" element={<SalesChannelPartnerRoute />} />
        <Route path="role-management" element={<RoleManagementPage />} />
        <Route path="manage-roles" element={<Navigate to="/role-management" replace />} />
        <Route path="category-list" element={<Navigate to="/manage-categories" replace />} />
        <Route path="manage-teams" element={<ManageTeamsPage />} />
        <Route path="teams-management" element={<Navigate to="/manage-teams" replace />} />
        <Route path="grow-with-mw" element={<GrowWithMwPagesPage />} />
        <Route path="grow-with-mw/sections" element={<GrowWithMwSectionsPage />} />
        <Route path="grow-with-mw/media" element={<GrowWithMwMediaPage />} />
        <Route path="grow-with-mw/pages/new" element={<GrowWithMwPageEditorPage />} />
        <Route path="grow-with-mw/pages/:id" element={<GrowWithMwPageEditorPage />} />
        <Route path="kit-management" element={<KitManagementRedirect />} />
        <Route path="kit-management/:categorySlug" element={<KitManagementInvalidCategory />} />
        <Route path="documentation" element={<Navigate to="/grow-with-mw" replace />} />
        <Route path="manage-content" element={<Navigate to="/manage-content/terms-conditions" replace />} />
        <Route path="manage-content/terms-conditions" element={<ManageContentPage contentType="terms_conditions" />} />
        <Route path="manage-content/privacy-policy" element={<ManageContentPage contentType="privacy_policy" />} />
        <Route path="manage-content/franchisee-agreement" element={<ManageContentPage contentType="franchisee_agreement" />} />
        <Route path="manage-content/franchisee-distributer" element={<ManageContentPage contentType="franchisee_distributer" />} />
        <Route
          path="manage-content/mw-full-franchise-agreement"
          element={<ManageContentPage contentType="mw_full_franchise_agreement" />}
        />
        <Route
          path="manage-content/mw-franchisee-operation-policy"
          element={<ManageContentPage contentType="mw_franchisee_operation_policy" />}
        />
        <Route
          path="manage-content/influencer-partner-agreement"
          element={<PlaceholderPage title="Influencer Partner Agreement" />}
        />
        <Route
          path="manage-content/digital-marketing-partner-agreement"
          element={<PlaceholderPage title="Digital Marketing Partner Agreement" />}
        />
        <Route path="payments" element={<PlaceholderPage title="Payments" />} />
        <Route path="settings" element={<PlaceholderPage title="Settings" />} />
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}
