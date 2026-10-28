export type ApiResult<T = unknown> = {
  success: boolean
  message?: string | null
  data?: T | null
  errors?: Record<string, string[]> | null
}

export type ManageUsersQuery = {
  page?: number
  pageSize?: number
  search?: string
  statusFilter?: string
  dealFilter?: string
  websiteFilter?: string
  dateFilter?: string
}

export type ManageUsersStats = {
  totalUsers: number
  activeUsers: number
  usersWithDeals: number
  totalWebsites: number
}

export type DealOption = {
  id: number
  dealName: string
  couponCode?: string | null
  planType: string
}

export type MappedDeal = {
  mappingId: number
  dealId: number
  dealName: string
  couponCode?: string | null
}

export type ManageUserRow = {
  id: number
  userCode: string
  email: string
  name: string
  phone?: string | null
  state?: string | null
  district?: string | null
  status: string
  createdAt: string
  collaborationEnabled: string
  saleskitEnabled: string
  refundStatus?: string | null
  refundStatusDate?: string | null
  referredBy?: string | null
  referralSourceDisplay: string
  websiteCount: number
  pendingReferralAmount: number
  totalReferralAmount: number
  totalPaidAmount: number
  lastPaymentDate?: string | null
  mwPaymentStatusLabel: string
  commissionStatus: string
  mwDeal?: MappedDeal | null
  franchiseDeal?: MappedDeal | null
  documentStatus?: string
  documentStatusTone?: string
  totalReferred?: number
  payoutStatus?: string
  invoiceId?: number | null
}

export type ManageUsersPage = {
  stats: ManageUsersStats
  mwDealCount: number
  franchiseDealCount: number
  mwDeals: DealOption[]
  franchiseDeals: DealOption[]
  users: ManageUserRow[]
  totalCount: number
  page: number
  pageSize: number
}

export type DashboardWebsite = {
  id: number
  companyName?: string | null
  cardId?: string | null
  cardStatus?: string | null
  paymentStatus?: string | null
  paymentDate?: string | null
  uploadedDate?: string | null
  validityDate?: string | null
  complimentaryEnabled?: string | null
  fUserEmail?: string | null
  statusClass: string
  statusText: string
  validityDisplay: string
  paymentLabel: string
  orderIdDisplay?: string | null
}

export type DashboardDetails = {
  userEmail: string
  websites: DashboardWebsite[]
}

export type BankDetails = {
  accountHolderName?: string | null
  accountNumber?: string | null
  ifscCode?: string | null
  bankName?: string | null
  upiId?: string | null
  upiName?: string | null
}

export type ReferredUser = {
  referralId: number
  referredEmail: string
  referralDate: string
  amount: number
  isCollaboration?: string | null
  customerId?: number | null
  franchiseeId?: number | null
  userName?: string | null
  userContact?: string | null
  cardId?: number | null
  cardUploadedDate?: string | null
  cardValidityDate?: string | null
  complimentaryEnabled?: string | null
  paymentStatus?: string | null
  paymentDate?: string | null
  fUserEmail?: string | null
  mwStatusText: string
  validityDisplay: string
}

export type ReferralDetails = {
  referrerEmail: string
  userName: string
  collaborationEnabled: string
  saleskitEnabled: string
  totalReferralAmount: number
  totalPaidAmount: number
  pendingAmount: number
  regularReferrals: number
  collaborationReferrals: number
  bank: BankDetails
  referredUsers: ReferredUser[]
}

export type ManageCardRow = {
  id: number
  cardId?: string | null
  userEmail?: string | null
  fUserEmail?: string | null
  userId?: number | null
  userName?: string | null
  userPhone?: string | null
  referralSourceDisplay: string
  companyName: string
  uploadedDate?: string | null
  validityDate?: string | null
  paymentDate?: string | null
  paymentStatus?: string | null
  complimentaryEnabled?: string | null
  statusText: string
  statusTone: string
  isTrial: boolean
  validityDisplay: string
  validityTone: string
  paymentLabel: string
  orderAmount?: string | null
  hasInvoice: boolean
  canToggleComplimentary: boolean
  publicUrl: string
  editUrl: string
  userIdDisplay?: string
  dbIdDisplay?: string
  dateCreatedDisplay?: string
  expiryDateDisplay?: string
  couponCode?: string
  orderValueDisplay?: string
}

export type ManageCardsPage = {
  cards: ManageCardRow[]
  totalCount: number
  page: number
  pageSize: number
}

export type FranchiseDistributorRow = {
  id: number
  email: string
  name: string
  phone?: string | null
  createdAt: string
  referralSourceDisplay: string
  companyName: string
  collaborationEnabled: string
  influencer: string
  frdStatusLabel: string
  cardPaymentStatus: string
  frdFeeDisplay: string
  franchiseInvoiceId?: number | null
  joiningDealInvoiceId?: number | null
  websiteCount: number
  pendingReferralAmount: number
  mwDeal?: MappedDeal | null
  franchiseDeal?: MappedDeal | null
}

export type FranchiseDistributorPage = {
  users: FranchiseDistributorRow[]
  mwDeals: DealOption[]
  franchiseDeals: DealOption[]
  totalCount: number
  page: number
  pageSize: number
}

export type AllOrderRow = {
  invoiceId: number
  orderIdDisplay: string
  orderDateDisplay: string
  userIdDisplay: string
  userEmail: string
  cardId?: number | null
  dbIdDisplay: string
  referralSource: string
  orderDescription: string
  listPriceDisplay: string
  promoDiscountDisplay: string
  netPriceDisplay: string
  cgstDisplay: string
  sgstDisplay: string
  igstDisplay: string
  totalAmount: number
  totalAmountDisplay: string
  invoiceNumber: string
  comment: string
}

export type AllOrdersPage = {
  orders: AllOrderRow[]
  totalCount: number
  page: number
  pageSize: number
}

export type FranchiseeRow = {
  id: number
  email: string
  name: string
  phone?: string | null
  createdAt?: string | null
  referralSourceDisplay: string
  companyName: string
  status: string
  isActive: boolean
  firstCardId?: number | null
  firstCardUserEmail?: string | null
  publicUrl: string
  editUrl: string
  paymentStatusLabel: string
  paymentStatusTone: string
  paidOnDisplay?: string | null
  franchiseFee: number
  franchiseFeeDisplay: string
  franchiseInvoiceId?: number | null
  websiteCount: number
  documentStatus: string
  documentStatusTone: string
}

export type FranchiseePage = {
  franchisees: FranchiseeRow[]
  totalCount: number
  page: number
  pageSize: number
}

export type FranchiseeWebsite = {
  id: number
  companyName?: string | null
  uploadedDate?: string | null
  validityDate?: string | null
  statusText: string
  paymentLabel: string
  publicUrl: string
}

export type FranchiseeDashboard = {
  email: string
  websites: FranchiseeWebsite[]
}

export type ManageReferralRow = {
  referralId: number
  commissionId: string
  userIdDisplay: string
  referrerEmail: string
  referredCustomer: string
  referredEmail: string
  orderDescription: string
  listPriceDisplay: string
  promoDiscountDisplay: string
  netPriceDisplay: string
  gstDisplay: string
  totalAmountDisplay: string
  referralAmount: number
  referralAmountDisplay: string
  renewalAmountDisplay: string
  commissionStatus: string
  sbPaymentStatus: string
  sbPaymentStatusTone: string
  paidAmount: number
  pendingAmount: number
  canAddPayment: boolean
  hasPaymentHistory: boolean
  comment: string
  cgstDisplay?: string
  sgstDisplay?: string
  igstDisplay?: string
  documentStatus?: string
  documentStatusTone?: string
  commissionGstDisplay?: string
  tdsDisplay?: string
  totalPayableDisplay?: string
  partnerInvoiceApproval?: string
  partnerInvoiceApprovalTone?: string
  partnerInvoiceId?: number | null
}

export type ManageReferralsPage = {
  referrals: ManageReferralRow[]
  totalCount: number
  page: number
  pageSize: number
}

export type ReferrerPaymentLine = {
  referralId: number
  referredEmail: string
  referredName: string
  referralDate: string
  userPaymentStatusLabel: string
  userPaymentStatusTone: string
  totalAmount: number
  paidAmount: number
  pendingAmount: number
  statusLabel: string
  statusTone: string
  canAddPayment: boolean
  hasHistory: boolean
}

export type ReferrerPaymentDetails = {
  referrerEmail: string
  referrerName: string
  lines: ReferrerPaymentLine[]
}

export type ReferralPaymentHistoryItem = {
  paymentDate: string
  amount: number
  transactionNumber: string
  paymentMethod: string
  paymentNotes?: string | null
  processedBy: string
}

export type ReferralPaymentHistory = {
  referralId: number
  referrerName: string
  referredName: string
  referralAmount: number
  items: ReferralPaymentHistoryItem[]
}

export type ManageReferralBankDetails = {
  userEmail: string
  accountHolderName?: string | null
  accountNumber?: string | null
  ifscCode?: string | null
  bankName?: string | null
  upiId?: string | null
  upiName?: string | null
}

export type PromoDiscountType = 'Amount' | 'Percentage'

export type CouponRow = {
  id: number
  roleId?: number | null
  planType: string
  couponName: string
  couponCode: string
  couponType: string
  createdAt?: string | null
  createdAtDisplay: string
  validityDate?: string | null
  validityDateDisplay: string
  promoDiscountType: PromoDiscountType
  promoDiscountValue: number
  promoDiscountDisplay: string
  usageLimitPerCoupon: number
  usageLimitPerCouponDisplay: string
  usageLimitPerUser: number
  usageLimitPerUserDisplay: string
  currentUsage: number
  /** Stored status: Active or InActive. */
  status: string
  /** Active, InActive or Expired (Expired once the validity date has passed). */
  statusDisplay: string
  statusTone: string
}

export type CouponsPage = {
  coupons: CouponRow[]
  totalCount: number
  page: number
  pageSize: number
}

export type CouponRoleOption = {
  roleId: number
  roleName: string
}

export type CouponsMeta = {
  roles: CouponRoleOption[]
  couponTypes: string[]
  statuses: string[]
}

export type UpsertCouponPayload = {
  roleId: number
  couponName: string
  couponCode: string
  couponType: string
  validityDate: string
  promoDiscountType: PromoDiscountType
  promoDiscountValue: number
  usageLimitPerCoupon: number
  usageLimitPerUser: number
  status?: string
}

export type PricingPlanKey = {
  id: number
  planName: string
}

export type ManagePricingMeta = {
  categories: string[]
  existingPlans: PricingPlanKey[]
}

export type ManagePricingRow = {
  id: number
  planName: string
  planCategory: string
  /** Null means lifetime validity. */
  validityMonths: number | null
  validityDisplay: string
  originalPrice: number
  originalPriceDisplay: string
  originalPriceGst: number
  originalPriceGstDisplay: string
  offeredPrice: number
  offeredPriceDisplay: string
  offeredPriceGst: number
  offeredPriceGstDisplay: string
  status: string
  statusTone: string
  createdAtDisplay?: string | null
}

export type ManagePricingPage = {
  items: ManagePricingRow[]
  totalCount: number
  page: number
  pageSize: number
}

export type UpsertPricingPayload = {
  planName: string
  planCategory: string
  /** Null means lifetime validity. */
  validityMonths: number | null
  originalPrice: number
  offeredPrice: number
  status?: string
}

export type PlanCommissionRoleOption = {
  roleId: number
  roleName: string
}

export type PlanCommissionPlanOption = {
  id: number
  planName: string
  planCategory: string
  validityDisplay: string
  originalPrice: number
  offeredPrice: number
  status: string
}

export type PlanCommissionsMeta = {
  roles: PlanCommissionRoleOption[]
  plans: PlanCommissionPlanOption[]
  discountTypes: string[]
  earningTypes: string[]
  gstRate: number
}

/** Stored terms plus amounts the API calculates from the plan price. */
export type PlanCommissionRow = {
  id: number
  roleId: number
  roleName: string
  pricingPlanId: number
  planName: string
  planCategory: string
  validityDisplay: string
  originalPrice: number
  originalPriceDisplay: string
  offeredPrice: number
  offeredPriceDisplay: string
  discountType: string
  discountAmount: number
  discountDisplay: string
  earningType: string
  earningAmount: number
  earningDisplay: string
  earningGst: number | null
  earningGstDisplay: string | null
  sbRevenue: number | null
  sbRevenueDisplay: string | null
  renewalPrice: number | null
  renewalPriceDisplay: string | null
  renewalPriceGst: number | null
  renewalPriceGstDisplay: string | null
  renewalEarningPercent: number | null
  renewalEarning: number | null
  renewalEarningDisplay: string | null
  /** Active or InActive. */
  status: string
  statusTone: string
  createdAtDisplay?: string | null
}

export type PlanCommissionsPage = {
  items: PlanCommissionRow[]
  totalCount: number
  page: number
  pageSize: number
}

export type UpsertPlanCommissionPayload = {
  /** Create adds one row per role; update takes exactly one. */
  roleIds: number[]
  pricingPlanId: number
  discountType: string
  discountAmount: number
  earningType: string
  earningAmount: number
  /** Renewal price without GST for this role on this plan. Null when not renewed. */
  renewalPrice: number | null
  /** Null means no renewal earning. Requires renewalPrice. */
  renewalEarningPercent: number | null
  status?: string
}

export type ManageFaqRow = {
  id: number
  pageType: string
  pageTypeDisplay: string
  question: string
  answer: string
  answerPreview: string
  sortOrder: number
  status: string
  statusTone: string
}

export type ManageFaqsPage = {
  faqs: ManageFaqRow[]
  totalCount: number
  page: number
  pageSize: number
}

export type FaqPageTypeOption = {
  value: string
  label: string
}

export type ManageFaqsMeta = {
  pageTypes: FaqPageTypeOption[]
}

export type UpsertFaqPayload = {
  pageType: string
  question: string
  answer: string
  sortOrder: number
  status?: string
}

export type EmailTemplateCategoryOption = {
  value: string
  label: string
}

export type EmailTemplateMeta = {
  categories: EmailTemplateCategoryOption[]
}

export type EmailTemplateRow = {
  id: number
  templateKey: string
  name: string
  category: string
  categoryDisplay: string
  subject: string
  bodyHtml: string
  bodyPreview: string
  availableVariables?: string | null
  description?: string | null
  status: string
  statusTone: string
  isSystem: boolean
  updatedAt?: string | null
  updatedBy?: string | null
}

export type EmailTemplatesPage = {
  templates: EmailTemplateRow[]
  totalCount: number
  page: number
  pageSize: number
}

export type UpsertEmailTemplatePayload = {
  templateKey: string
  name: string
  category: string
  subject: string
  bodyHtml: string
  availableVariables?: string
  description?: string
  status?: string
  updatedBy?: string
}

export type PreviewEmailTemplatePayload = {
  subject: string
  bodyHtml: string
  sampleVariables?: Record<string, string>
}

export type PreviewEmailTemplateResult = {
  subject: string
  bodyHtml: string
}

export type ContentTypeOption = {
  value: string
  label: string
  badge: string
}

export type ManageContentMeta = {
  contentTypes: ContentTypeOption[]
}

export type ManageContentItem = {
  contentType: string
  title: string
  content: string
  metaDescription: string
  metaKeywords: string
  lastUpdated?: string | null
  lastUpdatedDisplay?: string | null
  updatedBy?: string | null
}

export type ManageContentList = {
  items: ManageContentItem[]
}

export type UpsertContentPayload = {
  contentType: string
  title: string
  content: string
  metaDescription?: string
  metaKeywords?: string
  updatedBy?: string
}

export type ManageCategoryRow = {
  id: number
  businessProfileType: string
  businessHeading: string
  businessCategory: string
  businessCategorySlug: string
  productCategory: string
  productCategorySlug: string
  directoryPriority: number
  isActive: boolean
  activeLabel: string
  activeTone: string
  keywords: string
  tags: string
}

export type ManageCategoriesPage = {
  categories: ManageCategoryRow[]
  totalCount: number
  page: number
  pageSize: number
}

export type ManageCategoriesMeta = {
  csvHeaders: string[]
}

export type UpsertCategoryPayload = {
  businessProfileType: string
  businessHeading: string
  businessCategory: string
  businessCategorySlug?: string
  productCategory: string
  productCategorySlug?: string
  directoryPriority: number
  isActive: boolean
  keywords?: string
  tags?: string
  createdBy?: string
}

export type CategoryImportResult = {
  imported: number
  skipped: number
  errors: string[]
}

export type ManageTeamRow = {
  id: number
  legacyTeamId?: number | null
  firstName: string
  lastName: string
  name: string
  email: string
  phone: string
  district: string
  state: string
  status: string
  statusTone: string
  createdAtDisplay: string
  totalSales: number
  totalMwCreated: number
  referralCount: number
  trackerCount: number
  ownMwIds: number[]
}

export type ManageTeamsPage = {
  members: ManageTeamRow[]
  totalCount: number
  page: number
  pageSize: number
}

export type TeamReferralRow = {
  userId?: number | null
  referredName: string
  referredEmail: string
  phone: string
  type: string
  mwPaymentStatus: string
  mwPaymentStatusTone: string
  paidOnDisplay: string
  referralDateDisplay: string
}

export type TeamReferrals = {
  memberName: string
  memberEmail: string
  totalSales: number
  totalMwCreated: number
  rows: TeamReferralRow[]
}

export type TeamTrackerRow = {
  id: number
  shopName: string
  contactNumber: string
  approachedFor: string
  address: string
  dateVisitedDisplay: string
  finalStatus: string
  finalStatusTone: string
  lastUpdatedDisplay: string
}

export type TeamTracker = {
  memberName: string
  memberEmail: string
  rows: TeamTrackerRow[]
}

export type DocSectionOption = {
  id: number
  title: string
}

export type GrowWithMwMeta = {
  sections: DocSectionOption[]
  publicDocsPrefix: string
  growWithMwPrefix: string
}

export type DocPageListItem = {
  id: number
  title: string
  slug: string
  status: string
  statusTone: string
  sortOrder: number
  updatedAtDisplay: string
  sectionId: number
  sectionTitle: string
}

export type DocPagesPage = {
  pages: DocPageListItem[]
  totalCount: number
  page: number
  pageSize: number
}

export type DocPageOrderItem = {
  id: number
  title: string
  slug: string
  status: string
  sortOrder: number
}

export type DocPageDetail = {
  id: number
  sectionId: number
  sectionTitle: string
  title: string
  slug: string
  status: string
  contentHtml: string
  metaTitle: string
  metaDescription: string
  metaKeywords: string
  sortOrder: number
  publishedAtDisplay?: string | null
  updatedAtDisplay: string
  publicUrl: string
  sectionPages: DocPageOrderItem[]
}

export type UpsertDocPagePayload = {
  sectionId: number
  title: string
  slug?: string
  contentHtml?: string
  metaTitle?: string
  metaDescription?: string
  metaKeywords?: string
  action: 'draft' | 'publish'
}

export type DocSection = {
  id: number
  title: string
  slug: string
  description: string
  sortOrder: number
  collapsedDefault: boolean
  pageCount: number
}

export type UpsertDocSectionPayload = {
  title: string
  slug?: string
  description?: string
  collapsedDefault: boolean
}

export type DocMediaItem = {
  id: number
  filename: string
  relPath: string
  url: string
  mime: string
  sizeBytes: number
  uploadedBy?: string | null
  createdAtDisplay: string
  isImage: boolean
}

export type DocMediaList = {
  items: DocMediaItem[]
}

export type KitCategoryMeta = {
  key: string
  label: string
  itemCount: number
}

export type KitManagementMeta = {
  categories: KitCategoryMeta[]
  uploadsPublicPrefix: string
}

export type KitStats = {
  folders: number
  images: number
  videos: number
  files: number
  activeItems: number
  totalItems: number
}

export type KitBreadcrumb = {
  id: number
  title: string
}

export type KitFolderTile = {
  id: number
  title: string
  displayOrder: number
  status: string
  directItemCount: number
  subfolderCount: number
}

export type KitFolderOption = {
  id: number
  title: string
  depth: number
  parentId?: number | null
}

export type KitItem = {
  id: number
  type: string
  title: string
  filePath?: string | null
  fileUrl?: string | null
  videoUrl?: string | null
  displayOrder: number
  status: string
  folderId?: number | null
  createdAt?: string | null
}

export type KitExplorer = {
  category: string
  categoryLabel: string
  folderId: number
  stats: KitStats
  breadcrumb: KitBreadcrumb[]
  subfolders: KitFolderTile[]
  items: KitItem[]
  folderOptions: KitFolderOption[]
}

export type CreateKitFolderPayload = {
  category: string
  title: string
  parentId?: number | null
  displayOrder: number
}

export type UpdateKitFolderPayload = {
  category: string
  title: string
  parentId?: number | null
  displayOrder: number
  status: string
}

export type UpdateKitVideoPayload = {
  category: string
  title: string
  videoUrl?: string | null
  folderId?: number | null
  displayOrder: number
  status: string
}

export type MoveKitItemPayload = {
  category: string
  folderId?: number | null
}

export type ManageRoleRow = {
  roleId: number
  roleName: string
  roleDescription: string
  prefix: string
  salesType: string
}

export type ManageRolesPage = {
  roles: ManageRoleRow[]
  totalCount: number
  page: number
  pageSize: number
}

export type RoleSalesTypeOption = {
  value: string
  label: string
}

export type ManageRolesMeta = {
  salesTypes: RoleSalesTypeOption[]
}

export type UpsertRolePayload = {
  roleName: string
  roleDescription?: string | null
  prefix?: string | null
  salesType?: string | null
}

export type SalesChannelPartnerRole = {
  slug: string
  roleId: number
  roleName: string
  roleDescription: string
  prefix: string
  salesType: string
}

export type SalesChannelPartnerRow = {
  id: number
  userCode: string
  firstName: string
  lastName: string
  name: string
  email: string
  phone: string
  district: string
  state: string
  status: string
  statusTone: string
  createdAtDisplay: string
  joinedOnDisplay?: string
  referralSource?: string
  dateCreatedDisplay?: string
  expiryDateDisplay?: string
  userStatusLabel?: string
  userPaymentStatus?: string
  joiningFees?: number
  joiningFeesDisplay?: string
  promoDiscount?: number
  promoDiscountDisplay?: string
  couponCode?: string
  invoiceId?: number | null
  dbCount?: number
  documentStatus?: string
  documentStatusTone?: string
  panDocumentUrl?: string | null
  frontDocumentUrl?: string | null
  backDocumentUrl?: string | null
  totalReferred?: number
  totalReferralEarning?: number
  totalReferralEarningDisplay?: string
  commissionStatus?: string
  refundStatus?: string
  planStatus?: string
  planPaymentStatus?: string
  payoutStatus?: string
  partnerInvoiceId?: number | null
  totalSales?: number
  totalSalesAmount?: number
  totalSalesAmountDisplay?: string
}

export type SalesChannelPartnersPage = {
  role: SalesChannelPartnerRole
  partners: SalesChannelPartnerRow[]
  totalCount: number
  page: number
  pageSize: number
}

export type UpsertSalesChannelPartnerPayload = {
  firstName: string
  lastName: string
  email: string
  phone?: string
  district?: string
  state?: string
}
