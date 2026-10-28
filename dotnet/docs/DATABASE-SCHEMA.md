# Database schema

MySQL schema for ShareBizz, taken from the current EF Core model (`ApplicationDbContextModelSnapshot`). There are 44 tables. Integer primary keys are `AUTO_INCREMENT`, except where the primary key is also the foreign key (`card_slug_history.CardId`, and `UserId` on the three owned user tables). `tinyint(1)` is a boolean. `datetime(6)` stores microseconds.

`users`, `refresh_tokens`, `password_reset_tokens`, and `registration_pending` have `IsDeleted`. EF hides rows where that flag is true.

When a table, column, key, or relationship changes, update this file and `database-schema.html` in the same change.

Open [database-schema.html](database-schema.html) to select a table and see its foreign keys.

## Repeated data

These are the overlaps to plan against. A foreign key plus a copied email is still two copies of the same person.

### User, email, and name

`users` is the account (`Id`, `Email`, `UserCode`, `FirstName`, `LastName`, `Phone`). These tables store that person again:

| Table | Id column | Copied text |
|---|---|---|
| cards | `UserId`, `FranchiseeUserId` | `UserEmail`, `FranchiseeEmail`, `Email`, `Contact`, `FirstName`, `LastName`, `DisplayName` |
| invoices | `UserId` | `UserEmail`, `UserName`, `UserContact` |
| orders | `UserId`, `InvoiceId` | `UserEmail`, `CustomerName`, `CustomerEmail`, `CustomerContact` |
| referral_earnings | `ReferrerUserId`, `ReferredUserId` | `ReferrerEmail`, `ReferredEmail` |
| registration_pending | `ReferrerUserId` | `ReferrerEmail`, plus the pending user's own `Email` and `Phone` |
| notifications | `UserId` | `UserEmail` |
| deal_customer_mappings | `UserId` | `CustomerEmail` |
| customers | `OwnerUserId` (the owner, not the customer) | `CustomerName`, `EmailId`, `PhoneNumber` |
| email_logs | none | `UserEmail` |

`user_bank_details`, `user_custom_categories`, `user_joining_deals`, `franchise_verifications`, and `uploaded_files` keep only `UserId`. Their copied `UserEmail` columns were removed. `uploaded_files` stores the R2 object key, name, size, and folder. The file itself stays in Cloudflare.

`customer_followups.OwnerUserId` points at `users.Id`. Deleting a user does not delete the followup through that link; the customer delete does.

### Address

| Table | Columns |
|---|---|
| user_addresses | `AddressLine`, `Address2`, `City`, `District`, `State`, `Pincode`, `Country` |
| cards | `Location` (map link) |
| invoices | `BillingAddress`, `BillingCity`, `BillingState`, `BillingPincode` (frozen on the day of the sale; not a live join) |
| customers | `AddressLine1`, `AreaCity` |

### GST, bank, and UPI

| Table | Columns |
|---|---|
| user_addresses | `Gst` (the live GST number) |
| user_bank_details | `BankName`, `AccountHolderName`, `AccountNumber`, `AccountType`, `IfscCode`, `UpiId`, `UpiName` |
| user_payment_details | `GooglePay`, `Paytm`, `PhonePay`, `RzPay`, `RzPay2` |
| invoices | `BillingGstNumber` (frozen on the day of the sale) |

### One sale record

`invoices` is the sale. It holds the amount, payment status, Razorpay ids, and the billing snapshot.

`orders.InvoiceId` points at that sale. An order keeps the customer text. It does not store an amount or a payment status.

`user_joining_deals` records which joining deal a user has. `InvoiceId` points at the sale. It does not store an amount or a payment status.

`franchise_payments` was removed. A franchise payment that was not already an invoice was copied onto an invoice.

`referral_payments` is a payout against one `referral_earnings` row, not a second sale.

`invoices.OrderCode` is the invoice's public code. `orders.ExternalId` is the old external id on the order row.

### Card children repeat the parent

`card_products`, `card_services`, `card_offers`, `card_gallery_images`, and `card_views` store `CardId`. The card reaches the user. `card_slug_history` still stores the previous public slug.

### Coupons and joining deals are different catalogs

`coupons` is the coupon table. `deal_customer_mappings.CouponId` and `referral_earnings.CouponId` point at `coupons.Id`.

`joining_deals` is a separate catalog. `user_joining_deals.DealCode` copies `joining_deals.DealCode`. `joining_deals.MwDealId` and `FranchiseDealId` point at another `joining_deals` row.

### Referral link in three places

| Table | Columns |
|---|---|
| user_referral_settings | `ReferralCode`, `ReferredBy`, `ReferrerUserId`, `SenderUserId`, `MwReferralId` |
| referral_earnings | referrer and referred user ids and emails, plus `CouponId` and `InvoiceId` |
| registration_pending | `ReferrerUserId`, `ReferrerEmail` |

`SenderUserId` points at `users.Id`. `MwReferralId` is not a foreign key.

### Category id with no foreign key

`product_categories` is its own table. `card_products.ProductCategory` and `card_services.ProductCategory` point at `product_categories.Id`. A value that is not a category is cleared.

### Short text and status checks

City, email, and status use `varchar`, the same kind of short text as `users.Email` (`varchar(255)`), `users` city (`varchar(100)`), and `users.Status` (`varchar(20)`).

| Table | Column | Allowed values |
|---|---|---|
| cards | `CardStatus` | `Active`, `Suspended`, `Inactive`, `InActive`, or empty |
| invoices | `PaymentStatus` | `Created`, `Pending`, `Success`, `Paid`, `Failed`, `Rejected`, `Refunded` |
| customers | `Status` | `Followup required`, `Phone Busy/Not Picked`, `Important`, `Joined`, `Deal Done`, `Profile Shared`, `Interested`, `Not Interested` |
| coupons | `Status` | `Active`, `InActive`, `Expired` |
| referral_earnings | `Status` | `Pending`, `Paid`, `Partial`, `Rejected` |

A stored value outside that list is rewritten to the normal default before the check is added. `orders` has no status column. Its customer email is `varchar(255)`.

## Foreign keys

`|o` in the diagram means the foreign key can be null. Owned tables are deleted with the user.

| From | Column | To | On delete | Required |
|---|---|---|---|---|
| users | RoleId | roles.RoleId | Restrict | Yes |
| user_addresses | UserId | users.Id | Cascade (owned) | Yes, and it is the PK |
| user_payment_details | UserId | users.Id | Cascade (owned) | Yes, and it is the PK |
| user_referral_settings | UserId | users.Id | Cascade (owned) | Yes, and it is the PK |
| user_referral_settings | ReferrerUserId | users.Id | Set null | No |
| user_bank_details | UserId | users.Id | Cascade | Yes, unique |
| user_custom_categories | UserId | users.Id | Cascade | Yes |
| refresh_tokens | UserId | users.Id | Cascade | Yes |
| password_reset_tokens | UserId | users.Id | Cascade | Yes |
| registration_pending | ReferrerUserId | users.Id | Set null | No |
| franchise_verifications | UserId | users.Id | Cascade | Yes, unique |
| notifications | UserId | users.Id | Set null | No |
| cards | UserId | users.Id | Set null | No |
| cards | FranchiseeUserId | users.Id | Set null | No |
| card_products | CardId | cards.Id | Cascade | Yes |
| card_services | CardId | cards.Id | Cascade | Yes |
| card_offers | CardId | cards.Id | Cascade | Yes |
| card_gallery_images | CardId | cards.Id | Cascade | Yes |
| card_videos | CardId | cards.Id | Cascade | Yes |
| card_slug_history | CardId | cards.Id | Cascade | Yes, and it is the PK |
| card_views | CardId | cards.Id | Set null | No |
| invoices | UserId | users.Id | Set null | No |
| invoices | CardId | cards.Id | Set null | No |
| orders | UserId | users.Id | Set null | No |
| orders | CardId | cards.Id | Set null | No |
| orders | InvoiceId | invoices.Id | Set null | No |
| coupons | RoleId | roles.RoleId | Restrict | No |
| deal_customer_mappings | CouponId | coupons.Id | Cascade | Yes |
| deal_customer_mappings | UserId | users.Id | Set null | No |
| plan_commissions | PricingPlanId | pricing_plans.Id | Restrict | Yes |
| plan_commissions | RoleId | roles.RoleId | Restrict | Yes |
| user_joining_deals | JoiningDealId | joining_deals.Id | Restrict | Yes |
| user_joining_deals | UserId | users.Id | Cascade | Yes |
| user_joining_deals | InvoiceId | invoices.Id | Set null | No |
| referral_earnings | CouponId | coupons.Id | Set null | No |
| referral_earnings | InvoiceId | invoices.Id | Set null | No |
| referral_earnings | ReferrerUserId | users.Id | Set null | No |
| referral_earnings | ReferredUserId | users.Id | Set null | No |
| referral_payments | ReferralEarningId | referral_earnings.Id | Restrict | Yes |
| customers | OwnerUserId | users.Id | Cascade | Yes |
| uploaded_files | UserId | users.Id | Cascade | Yes |
| customer_followups | CustomerId | customers.Id | Cascade | Yes |
| customer_followups | OwnerUserId | users.Id | Restrict | Yes |
| card_products | ProductCategory | product_categories.Id | Set null | No |
| card_services | ProductCategory | product_categories.Id | Set null | No |
| joining_deals | MwDealId | joining_deals.Id | Set null | No |
| joining_deals | FranchiseDealId | joining_deals.Id | Set null | No |
| user_referral_settings | SenderUserId | users.Id | Set null | No |
| doc_pages | SectionId | doc_sections.Id | Cascade | Yes |
| kit_folders | ParentId | kit_folders.Id | Set null | No |
| kit_items | FolderId | kit_folders.Id | Set null | No |

No foreign keys: `content_pages`, `faqs`, `email_logs`, `email_templates`, `doc_media`, `product_categories`, `theme_catalog`. `pricing_plans` is a parent only. `joining_deals` points at itself for the paired deal. `doc_sections` is a parent only.

## Column catalog

Types below are the MySQL types. `identity` means `AUTO_INCREMENT`.

### cards
Entity `Card`. Primary key `Id`. Indexes: UNIQUE(DbCode) | INDEX(FranchiseeEmail) | INDEX(FranchiseeUserId) | UNIQUE(Slug) | INDEX(UserEmail) | INDEX(UserId).

| Column | Type | Null |
|---|---|---|
| Id | int identity | No |
| AboutUs | varchar(2000) | Yes |
| BusinessHours | longtext | Yes |
| BusinessOperationArea | longtext | No |
| BusinessOperationLocations | longtext | Yes |
| BusinessProfileType | longtext | No |
| BusinessType | longtext | No |
| CardStatus | varchar(20) | Yes |
| CollaborationEnabled | varchar(3) | No |
| CompanyEstDate | longtext | Yes |
| CompanyName | varchar(200) | Yes |
| ComplimentaryEnabled | varchar(10) | No |
| Contact | longtext | Yes |
| Contact2 | longtext | Yes |
| CreatedAt | datetime(6) | Yes |
| DbCode | varchar(16) | Yes |
| DisplayName | varchar(199) | Yes |
| DisplayNameChangeCount | int | No |
| Email | varchar(255) | Yes |
| Facebook | longtext | Yes |
| FirstName | longtext | Yes |
| FranchiseeEmail | varchar(200) | Yes |
| FranchiseeUserId | int | Yes |
| HeroImagePath | varchar(255) | Yes |
| Instagram | longtext | Yes |
| Ip | longtext | Yes |
| LastName | longtext | Yes |
| LinkedIn | longtext | Yes |
| Location | longtext | Yes |
| LogoPath | varchar(1000) | Yes |
| MobileCss | varchar(120) | Yes |
| NameChangeCount | int | No |
| NatureOfBusiness | longtext | Yes |
| Password | longtext | Yes |
| Pinterest | longtext | Yes |
| Position | longtext | Yes |
| PositionPrimary | longtext | No |
| PositionSecondary | longtext | No |
| Slug | varchar(200) | Yes |
| Speciality | longtext | Yes |
| ThemeCss | varchar(120) | Yes |
| Title | longtext | Yes |
| Twitter | longtext | Yes |
| UserEmail | varchar(200) | Yes |
| UserId | int | Yes |
| ValidityDate | datetime(6) | Yes |
| Website | longtext | Yes |
| Website2 | longtext | Yes |
| Whatsapp | longtext | Yes |
| Youtube | longtext | Yes |

### card_gallery_images
Entity `CardGalleryImage`. Primary key `Id`. Indexes: INDEX(CardId).

| Column | Type | Null |
|---|---|---|
| Id | int identity | No |
| CardId | int | No |
| CreatedAt | datetime(6) | Yes |
| DisplayOrder | int | No |
| GalleryImage | varchar(500) | Yes |
| UpdatedAt | datetime(6) | Yes |

### card_offers
Entity `CardOffer`. Primary key `Id`. Indexes: INDEX(CardId).

| Column | Type | Null |
|---|---|---|
| Id | int identity | No |
| Badge | longtext | Yes |
| CardId | int | No |
| CreatedAt | datetime(6) | Yes |
| DiscountPercentage | int | No |
| DisplayOrder | int | No |
| EndDate | date | Yes |
| EndTime | time(6) | Yes |
| OfferDescription | longtext | Yes |
| OfferImage | varchar(255) | Yes |
| OfferTitle | varchar(255) | No |
| StartDate | date | Yes |
| StartTime | time(6) | Yes |
| Status | longtext | Yes |
| UpdatedAt | datetime(6) | Yes |

### card_products
Entity `CardProduct`. Primary key `Id`. Indexes: INDEX(CardId) | INDEX(ProductCategory).

| Column | Type | Null |
|---|---|---|
| Id | int identity | No |
| CardId | int | No |
| CategorySource | longtext | Yes |
| CreatedAt | datetime(6) | Yes |
| CtaText | longtext | Yes |
| DisplayOrder | int | No |
| Mrp | decimal(12,2) | No |
| PriceType | longtext | Yes |
| PricingUnit | longtext | Yes |
| ProductCategory | int | Yes |
| ProductDescription | longtext | Yes |
| ProductImage | varchar(500) | Yes |
| ProductName | longtext | Yes |
| SellingPrice | decimal(12,2) | No |
| TaxRate | decimal(12,2) | No |
| UpdatedAt | datetime(6) | Yes |

### card_services
Entity `CardService`. Primary key `Id`. Indexes: INDEX(CardId) | INDEX(ProductCategory).

| Column | Type | Null |
|---|---|---|
| Id | int identity | No |
| CardId | int | No |
| CategorySource | longtext | Yes |
| CreatedAt | datetime(6) | Yes |
| DisplayOrder | int | No |
| ProductCategory | int | Yes |
| ProductDescription | longtext | Yes |
| ProductImage | varchar(500) | Yes |
| ProductName | longtext | Yes |
| UpdatedAt | datetime(6) | Yes |

### card_slug_history
Entity `CardSlugHistory`. Primary key `CardId`. Indexes: UNIQUE(PreviousSlug).

| Column | Type | Null |
|---|---|---|
| CardId | int | No |
| PreviousSlug | varchar(255) | No |

### card_videos
Entity `CardVideo`. Primary key `Id`. Indexes: INDEX(CardId).

| Column | Type | Null |
|---|---|---|
| Id | int identity | No |
| CardId | int | No |
| CreatedAt | datetime(6) | Yes |
| DisplayOrder | int | No |
| ThumbnailPath | varchar(500) | Yes |
| UpdatedAt | datetime(6) | Yes |
| Url | varchar(500) | Yes |
| VideoType | varchar(20) | Yes |

### card_views
Entity `CardView`. Primary key `Id`. Indexes: INDEX(CardId).

| Column | Type | Null |
|---|---|---|
| Id | int identity | No |
| CardId | int | Yes |
| CreatedAt | datetime(6) | No |
| Ip | varchar(100) | Yes |

### content_pages
Entity `ContentPage`. Primary key `Id`. Indexes: UNIQUE(ContentType).

| Column | Type | Null |
|---|---|---|
| Id | int identity | No |
| Content | longtext | No |
| ContentType | varchar(80) | No |
| IsActive | tinyint(1) | No |
| LastUpdated | datetime(6) | Yes |
| MetaDescription | longtext | Yes |
| MetaKeywords | longtext | Yes |
| Title | varchar(255) | No |
| UpdatedBy | longtext | Yes |

### coupons
Entity `Coupon`. Primary key `Id`. Indexes: INDEX(CouponCode) | INDEX(RoleId).

| Column | Type | Null |
|---|---|---|
| Id | int identity | No |
| CouponCode | varchar(100) | No |
| CouponName | varchar(200) | No |
| CouponType | varchar(50) | No |
| CreatedAt | datetime(6) | Yes |
| CreatedBy | varchar(200) | Yes |
| CurrentUsage | int | No |
| PromoDiscountType | varchar(20) | No |
| PromoDiscountValue | decimal(12,2) | No |
| RoleId | int | Yes |
| Status | varchar(20) | No |
| UsageLimitPerCoupon | int | No |
| UsageLimitPerUser | int | No |
| ValidityDate | datetime(6) | Yes |

### customers
Entity `Customer`. Primary key `Id`. Indexes: INDEX(OwnerUserId).

| Column | Type | Null |
|---|---|---|
| Id | int identity | No |
| AddressLine1 | longtext | Yes |
| ApproachedFor | longtext | Yes |
| AreaCity | varchar(100) | Yes |
| BusinessType | longtext | Yes |
| Comments | longtext | Yes |
| CompanyName | longtext | Yes |
| CreatedAt | datetime(6) | No |
| CustomerName | varchar(150) | No |
| EmailId | varchar(255) | Yes |
| FollowupMethod | longtext | Yes |
| LabelTag | longtext | Yes |
| LastSharedAt | datetime(6) | Yes |
| OwnerRole | varchar(20) | No |
| OwnerUserId | int | No |
| PhoneNumber | varchar(25) | No |
| Source | longtext | No |
| Status | varchar(40) | No |
| UpdatedAt | datetime(6) | Yes |
| Website | longtext | Yes |

### customer_followups
Entity `CustomerFollowup`. Primary key `Id`. Indexes: INDEX(CustomerId) | INDEX(OwnerUserId).

| Column | Type | Null |
|---|---|---|
| Id | int identity | No |
| Comments | longtext | Yes |
| CreatedAt | datetime(6) | No |
| CustomerId | int | No |
| FollowupDatetime | datetime(6) | No |
| FollowupMethod | longtext | No |
| FollowupStatus | longtext | No |
| OwnerRole | longtext | No |
| OwnerUserId | int | No |

### deal_customer_mappings
Entity `DealCustomerMapping`. Primary key `Id`. Indexes: INDEX(CouponId) | INDEX(CustomerEmail) | INDEX(UserId).

| Column | Type | Null |
|---|---|---|
| Id | int identity | No |
| CreatedAt | datetime(6) | Yes |
| CreatedBy | varchar(255) | No |
| CouponId | int | No |
| CustomerEmail | varchar(255) | No |
| UserId | int | Yes |

### doc_media
Entity `DocMedia`. Primary key `Id`.

| Column | Type | Null |
|---|---|---|
| Id | int identity | No |
| CreatedAt | datetime(6) | No |
| Filename | varchar(255) | No |
| Mime | varchar(128) | Yes |
| RelPath | varchar(512) | No |
| SizeBytes | int | No |
| UploadedBy | longtext | Yes |

### doc_pages
Entity `DocPage`. Primary key `Id`. Indexes: INDEX(SectionId) | UNIQUE(Slug).

| Column | Type | Null |
|---|---|---|
| Id | int identity | No |
| ContentHtml | longtext | Yes |
| CreatedAt | datetime(6) | No |
| MetaDescription | longtext | Yes |
| MetaKeywords | longtext | Yes |
| MetaTitle | longtext | Yes |
| PublishedAt | datetime(6) | Yes |
| SectionId | int | No |
| Slug | varchar(191) | No |
| SortOrder | int | No |
| Status | varchar(20) | No |
| Title | varchar(255) | No |
| UpdatedAt | datetime(6) | Yes |

### doc_sections
Entity `DocSection`. Primary key `Id`. Indexes: UNIQUE(Slug).

| Column | Type | Null |
|---|---|---|
| Id | int identity | No |
| CollapsedDefault | tinyint(1) | No |
| CreatedAt | datetime(6) | No |
| Description | longtext | Yes |
| Slug | varchar(191) | No |
| SortOrder | int | No |
| Title | varchar(255) | No |
| UpdatedAt | datetime(6) | Yes |

### email_logs
Entity `EmailLog`. Primary key `Id`.

| Column | Type | Null |
|---|---|---|
| Id | int identity | No |
| EmailType | varchar(100) | No |
| SentDate | datetime(6) | No |
| Status | varchar(20) | No |
| Subject | varchar(500) | No |
| UserEmail | varchar(255) | No |

### email_templates
Entity `EmailTemplate`. Primary key `Id`. Indexes: UNIQUE(TemplateKey) | INDEX(Category,IsActive).

| Column | Type | Null |
|---|---|---|
| Id | int identity | No |
| AvailableVariables | varchar(500) | Yes |
| BodyHtml | longtext | No |
| Category | varchar(50) | No |
| CreatedAt | datetime(6) | Yes |
| Description | varchar(1000) | Yes |
| IsActive | tinyint(1) | No |
| IsSystem | tinyint(1) | No |
| Name | varchar(200) | No |
| Subject | varchar(500) | No |
| TemplateKey | varchar(80) | No |
| UpdatedAt | datetime(6) | Yes |
| UpdatedBy | varchar(150) | Yes |

### faqs
Entity `Faq`. Primary key `Id`. Indexes: INDEX(PageType,SortOrder).

| Column | Type | Null |
|---|---|---|
| Id | int identity | No |
| Answer | longtext | No |
| CreatedAt | datetime(6) | Yes |
| PageType | varchar(40) | No |
| Question | longtext | No |
| SortOrder | int | No |
| Status | varchar(20) | No |
| UpdatedAt | datetime(6) | Yes |

### uploaded_files
Entity `UploadedFile`. Primary key `Id`. Indexes: INDEX(UserId, Folder, Document, Status).

| Column | Type | Null |
|---|---|---|
| Id | int identity | No |
| ContentType | varchar(100) | No |
| Document | varchar(40) | No |
| Folder | varchar(30) | No |
| ObjectKey | varchar(500) | No |
| OriginalFileName | varchar(255) | No |
| ReplacedAt | datetime(6) | Yes |
| SizeBytes | bigint | No |
| Status | varchar(20) | No |
| StoredFileName | varchar(255) | No |
| UploadedAt | datetime(6) | No |
| UserId | int | No |

### franchise_verifications
Entity `FranchiseVerification`. Primary key `Id`. Indexes: UNIQUE(UserId).

| Column | Type | Null |
|---|---|---|
| Id | int identity | No |
| AdminRemarks | longtext | Yes |
| CreatedAt | datetime(6) | Yes |
| DocumentPath | varchar(500) | Yes |
| ReviewedAt | datetime(6) | Yes |
| ReviewedBy | longtext | Yes |
| Status | varchar(20) | No |
| SubmittedAt | datetime(6) | Yes |
| UpdatedAt | datetime(6) | Yes |
| UserId | int | No |

### invoices
Entity `Invoice`. Primary key `Id`. Indexes: INDEX(CardId) | UNIQUE(InvoiceNumber) | UNIQUE(OrderCode) | INDEX(UserEmail) | INDEX(UserId).

| Column | Type | Null |
|---|---|---|
| Id | int identity | No |
| AdminComment | varchar(1000) | Yes |
| BillingAddress | longtext | No |
| BillingCity | varchar(100) | No |
| BillingContact | longtext | No |
| BillingEmail | varchar(255) | No |
| BillingGstNumber | longtext | Yes |
| BillingName | longtext | No |
| BillingPincode | longtext | No |
| BillingState | longtext | No |
| CardId | int | Yes |
| CgstAmount | decimal(12,2) | No |
| CreatedAt | datetime(6) | Yes |
| DiscountAmount | decimal(12,2) | No |
| FinalAmount | decimal(12,2) | No |
| HsnSacCode | longtext | No |
| IgstAmount | decimal(12,2) | No |
| IgstPercentage | decimal(5,2) | No |
| InvoiceDate | datetime(6) | No |
| InvoiceNumber | varchar(20) | No |
| OrderCode | varchar(20) | No |
| OriginalAmount | decimal(12,2) | No |
| PaymentDate | datetime(6) | Yes |
| PaymentStatus | varchar(20) | No |
| PaymentType | longtext | No |
| PlanName | longtext | Yes |
| PlanValidity | longtext | Yes |
| PromoCode | longtext | Yes |
| PromoDiscount | decimal(12,2) | No |
| Quantity | int | No |
| RazorpayOrderId | longtext | No |
| RazorpayPaymentId | longtext | No |
| RazorpaySignature | longtext | No |
| ReferenceNumber | longtext | No |
| ServiceDescription | longtext | No |
| ServiceName | longtext | No |
| SgstAmount | decimal(12,2) | No |
| SubTotal | decimal(12,2) | No |
| TotalAmount | decimal(12,2) | No |
| TotalPrice | decimal(12,2) | No |
| UnitPrice | decimal(12,2) | No |
| UpdatedAt | datetime(6) | Yes |
| UserContact | longtext | No |
| UserEmail | varchar(255) | No |
| UserId | int | Yes |
| UserName | longtext | No |

### joining_deals
Entity `JoiningDeal`. Primary key `Id`. Indexes: UNIQUE(DealCode) | INDEX(FranchiseDealId) | INDEX(MwDealId).

| Column | Type | Null |
|---|---|---|
| Id | int identity | No |
| CommissionAmount | decimal(12,2) | No |
| CreatedAt | datetime(6) | Yes |
| DealCode | varchar(50) | No |
| DealName | varchar(255) | No |
| DealType | varchar(100) | No |
| Description | longtext | Yes |
| DiscountAmount | decimal(12,2) | No |
| Fees | decimal(12,2) | No |
| FranchiseDealId | int | Yes |
| GstAmount | decimal(12,2) | No |
| IsActive | tinyint(1) | No |
| MwDealId | int | Yes |
| TotalFees | decimal(12,2) | No |
| UpdatedAt | datetime(6) | Yes |
| UpgradeOrder | int | No |

### kit_folders
Entity `KitFolder`. Primary key `Id`. Indexes: INDEX(ParentId).

| Column | Type | Null |
|---|---|---|
| Id | int identity | No |
| Category | varchar(50) | No |
| CreatedAt | datetime(6) | Yes |
| DisplayOrder | int | No |
| ParentId | int | Yes |
| Status | varchar(20) | No |
| Title | varchar(255) | No |
| UpdatedAt | datetime(6) | Yes |

### kit_items
Entity `KitItem`. Primary key `Id`. Indexes: INDEX(FolderId).

| Column | Type | Null |
|---|---|---|
| Id | int identity | No |
| Category | varchar(50) | No |
| CreatedAt | datetime(6) | Yes |
| DisplayOrder | int | No |
| FilePath | varchar(500) | Yes |
| FolderId | int | Yes |
| Status | varchar(20) | No |
| Title | varchar(255) | Yes |
| Type | varchar(20) | No |
| UpdatedAt | datetime(6) | Yes |
| VideoUrl | longtext | Yes |

### notifications
Entity `Notification`. Primary key `Id`. Indexes: INDEX(UserId) | INDEX(Audience,IsRead).

| Column | Type | Null |
|---|---|---|
| Id | int identity | No |
| Audience | varchar(20) | No |
| CreatedAt | datetime(6) | No |
| IsRead | tinyint(1) | No |
| Message | longtext | No |
| Priority | varchar(20) | No |
| Title | varchar(255) | No |
| Type | varchar(50) | No |
| UpdatedAt | datetime(6) | Yes |
| UserEmail | longtext | Yes |
| UserId | int | Yes |
| UserType | longtext | Yes |

### orders
Entity `Order`. Primary key `Id`. Indexes: INDEX(CardId) | INDEX(InvoiceId) | INDEX(UserId).

| Column | Type | Null |
|---|---|---|
| Id | int identity | No |
| CardId | int | Yes |
| CreatedAt | datetime(6) | Yes |
| CustomerContact | longtext | Yes |
| CustomerEmail | varchar(255) | Yes |
| CustomerName | longtext | Yes |
| ExternalId | varchar(200) | No |
| InvoiceId | int | Yes |
| ProductId | longtext | Yes |
| UserEmail | varchar(200) | No |
| UserId | int | Yes |

### password_reset_tokens
Entity `PasswordResetToken`. Primary key `Id`. Indexes: INDEX(UserId).

| Column | Type | Null |
|---|---|---|
| Id | int identity | No |
| CreatedAt | datetime(6) | No |
| ExpiresAt | datetime(6) | No |
| IsDeleted | tinyint(1) | No |
| TokenHash | varchar(128) | No |
| UpdatedAt | datetime(6) | Yes |
| UsedAt | datetime(6) | Yes |
| UserId | int | No |

### plan_commissions
Entity `PlanCommission`. Primary key `Id`. Indexes: INDEX(PricingPlanId) | UNIQUE(RoleId,PricingPlanId).

| Column | Type | Null |
|---|---|---|
| Id | int identity | No |
| CreatedAt | datetime(6) | Yes |
| DiscountAmount | decimal(12,2) | No |
| DiscountType | varchar(30) | No |
| EarningAmount | decimal(12,2) | No |
| EarningType | varchar(30) | No |
| PricingPlanId | int | No |
| RenewalEarningPercent | decimal(5,2) | Yes |
| RenewalPrice | decimal(12,2) | Yes |
| RenewalPriceGst | decimal(12,2) | Yes |
| RoleId | int | No |
| Status | varchar(20) | No |
| UpdatedAt | datetime(6) | Yes |

### pricing_plans
Entity `PricingPlan`. Primary key `Id`. Indexes: UNIQUE(PlanName).

| Column | Type | Null |
|---|---|---|
| Id | int identity | No |
| CreatedAt | datetime(6) | Yes |
| OfferedPrice | decimal(12,2) | No |
| OfferedPriceGst | decimal(12,2) | No |
| OriginalPrice | decimal(12,2) | No |
| OriginalPriceGst | decimal(12,2) | No |
| PlanCategory | varchar(50) default `DB Profile` | No |
| PlanName | varchar(200) | No |
| Status | varchar(20) | No |
| UpdatedAt | datetime(6) | Yes |
| ValidityMonths | int | Yes |

### product_categories
Entity `ProductCategory`. Primary key `Id`.

| Column | Type | Null |
|---|---|---|
| Id | int identity | No |
| BusinessCategory | varchar(255) | Yes |
| BusinessCategorySlug | longtext | Yes |
| BusinessHeading | longtext | Yes |
| BusinessProfileType | varchar(100) | Yes |
| CreatedAt | datetime(6) | Yes |
| CreatedBy | longtext | Yes |
| DirectoryPriority | int | No |
| IsActive | tinyint(1) | No |
| Keywords | longtext | Yes |
| ProductCategoryName | varchar(255) | Yes |
| ProductCategorySlug | longtext | Yes |
| Tags | longtext | Yes |
| UpdatedAt | datetime(6) | Yes |

### referral_earnings
Entity `ReferralEarning`. Primary key `Id`. Indexes: UNIQUE(CommissionCode) | INDEX(CouponId) | INDEX(InvoiceId) | INDEX(ReferredEmail) | INDEX(ReferredUserId) | INDEX(ReferrerEmail) | INDEX(ReferrerUserId).

| Column | Type | Null |
|---|---|---|
| Id | int identity | No |
| AdminComment | varchar(1000) | Yes |
| Amount | decimal(12,2) | No |
| CommissionCode | varchar(30) | Yes |
| CouponId | int | Yes |
| InvoiceId | int | Yes |
| IsCollaboration | varchar(10) | No |
| PaymentDate | datetime(6) | Yes |
| ReferralDate | datetime(6) | No |
| ReferredEmail | varchar(255) | No |
| ReferredUserId | int | Yes |
| ReferrerEmail | varchar(255) | No |
| ReferrerUserId | int | Yes |
| Status | varchar(20) | No |

### referral_payments
Entity `ReferralPayment`. Primary key `Id`. Indexes: INDEX(ReferralEarningId).

| Column | Type | Null |
|---|---|---|
| Id | int identity | No |
| Amount | decimal(12,2) | No |
| PaymentDate | datetime(6) | No |
| PaymentMethod | longtext | Yes |
| PaymentNotes | longtext | Yes |
| ProcessedBy | longtext | Yes |
| ReferralEarningId | int | No |
| TransactionNumber | longtext | Yes |

### refresh_tokens
Entity `RefreshToken`. Primary key `Id`. Indexes: INDEX(Token) | INDEX(UserId).

| Column | Type | Null |
|---|---|---|
| Id | int identity | No |
| CreatedAt | datetime(6) | No |
| ExpiresAt | datetime(6) | No |
| IsDeleted | tinyint(1) | No |
| RevokedAt | datetime(6) | Yes |
| Token | varchar(500) | No |
| UpdatedAt | datetime(6) | Yes |
| UserId | int | No |

### registration_pending
Entity `RegistrationPending`. Primary key `Id`. Indexes: INDEX(ReferrerUserId) | INDEX(Email,Role).

| Column | Type | Null |
|---|---|---|
| Id | int identity | No |
| CreatedAt | datetime(6) | No |
| Email | varchar(255) | No |
| ExpiresAt | datetime(6) | No |
| FirstName | varchar(100) | No |
| IsConsumed | tinyint(1) | No |
| IsDeleted | tinyint(1) | No |
| LastName | varchar(100) | No |
| Otp | varchar(10) | No |
| PasswordHash | varchar(255) | No |
| Phone | varchar(25) | No |
| ReferrerEmail | varchar(255) | Yes |
| ReferrerUserId | int | Yes |
| Role | varchar(20) | No |
| State | varchar(100) | Yes |
| UpdatedAt | datetime(6) | Yes |

### roles
Entity `Role`. Primary key `RoleId`. Indexes: UNIQUE(RoleName) | INDEX(SalesType).

| Column | Type | Null |
|---|---|---|
| RoleId | int identity | No |
| CreatedAt | datetime(6) | Yes |
| Prefix | varchar(40) | Yes |
| RoleDescription | varchar(500) | Yes |
| RoleName | varchar(120) | No |
| SalesType | varchar(80) | Yes |
| UpdatedAt | datetime(6) | Yes |

### theme_catalog
Entity `ThemeCatalog`. Primary key `Id`. Indexes: UNIQUE(ThemeNumber).

| Column | Type | Null |
|---|---|---|
| Id | int identity | No |
| CreatedAt | datetime(6) | Yes |
| CssValue | varchar(120) | No |
| IsActive | tinyint(1) | No |
| PreviewImage | longtext | Yes |
| SortOrder | int | No |
| ThemeName | varchar(100) | No |
| ThemeNumber | int | No |
| UpdatedAt | datetime(6) | Yes |

### users
Entity `User`. Primary key `Id`. Indexes: UNIQUE(Email) | INDEX(RoleId) | UNIQUE(UserCode) | INDEX(RoleId,CreatedAt).

| Column | Type | Null |
|---|---|---|
| Id | int identity | No |
| CreatedAt | datetime(6) | No |
| Department | varchar(100) | Yes |
| Email | varchar(255) | No |
| FirstName | varchar(100) | No |
| Ip | varchar(100) | Yes |
| IsDeleted | tinyint(1) | No |
| LastName | varchar(100) | No |
| Password | varchar(255) | No |
| Phone | varchar(25) | Yes |
| ProfileImage | varchar(500) | Yes |
| RefundStatus | varchar(20) | Yes |
| RefundStatusDate | datetime(6) | Yes |
| RoleId | int | No |
| SelectService | varchar(200) | Yes |
| Status | varchar(20) | No |
| UpdatedAt | datetime(6) | Yes |
| UserCode | varchar(50) | No |
| UserToken | varchar(200) | Yes |

### user_bank_details
Entity `UserBankDetails`. Primary key `Id`. Indexes: UNIQUE(UserId).

| Column | Type | Null |
|---|---|---|
| Id | int identity | No |
| AccountHolderName | longtext | Yes |
| AccountNumber | longtext | Yes |
| AccountType | longtext | Yes |
| BankName | longtext | Yes |
| CreatedAt | datetime(6) | Yes |
| IfscCode | longtext | Yes |
| UpdatedAt | datetime(6) | Yes |
| UpiId | longtext | Yes |
| UpiName | longtext | Yes |
| UserId | int | No |

### user_custom_categories
Entity `UserCustomCategory`. Primary key `Id`. Indexes: INDEX(UserId).

| Column | Type | Null |
|---|---|---|
| Id | int identity | No |
| CategoryName | varchar(255) | No |
| CategoryType | varchar(50) | No |
| CreatedAt | datetime(6) | Yes |
| Description | longtext | Yes |
| IsActive | tinyint(1) | No |
| UpdatedAt | datetime(6) | Yes |
| UserId | int | No |

### user_joining_deals
Entity `UserJoiningDeal`. Primary key `Id`. Indexes: INDEX(InvoiceId) | INDEX(JoiningDealId) | INDEX(UserId).

| Column | Type | Null |
|---|---|---|
| Id | int identity | No |
| CreatedAt | datetime(6) | Yes |
| DealCode | varchar(50) | No |
| EmailSent | tinyint(1) | No |
| EmailSentDate | datetime(6) | Yes |
| ExpiryDate | datetime(6) | Yes |
| InvoiceId | int | Yes |
| JoiningDealId | int | No |
| MappedBy | longtext | Yes |
| MappedDate | datetime(6) | Yes |
| MappingStatus | varchar(20) | No |
| Notes | longtext | Yes |
| StartDate | datetime(6) | Yes |
| UpdatedAt | datetime(6) | Yes |
| UserId | int | No |

### user_addresses
Entity `UserAddress`. Primary key `UserId`. Owned by `User.Address`.

| Column | Type | Null |
|---|---|---|
| UserId | int | No |
| Address2 | varchar(500) | Yes |
| AddressLine | varchar(500) | Yes |
| City | varchar(100) | Yes |
| Country | varchar(100) | Yes |
| District | varchar(100) | Yes |
| Gst | varchar(50) | Yes |
| Pincode | varchar(20) | Yes |
| State | varchar(100) | Yes |

### user_payment_details
Entity `UserPaymentDetails`. Primary key `UserId`. Owned by `User.Payment`.

| Column | Type | Null |
|---|---|---|
| UserId | int | No |
| GooglePay | varchar(200) | Yes |
| Paytm | varchar(200) | Yes |
| PhonePay | varchar(200) | Yes |
| RzPay | varchar(300) | Yes |
| RzPay2 | varchar(300) | Yes |

### user_referral_settings
Entity `UserReferralSettings`. Primary key `UserId`. Owned by `User.Referral`. Indexes: INDEX(ReferralCode) | INDEX(ReferredBy) | INDEX(ReferrerUserId) | INDEX(SenderUserId).

| Column | Type | Null |
|---|---|---|
| UserId | int | No |
| CollaborationEnabled | tinyint(1) | No |
| Influencer | tinyint(1) | No |
| MwReferralId | int | Yes |
| ReferralCode | varchar(50) | Yes |
| ReferredBy | varchar(255) | Yes |
| ReferrerUserId | int | Yes |
| SaleskitEnabled | tinyint(1) | No |
| SenderUserId | int | Yes |

