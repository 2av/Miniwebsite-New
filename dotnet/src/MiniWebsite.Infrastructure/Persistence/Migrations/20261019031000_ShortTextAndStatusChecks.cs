using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniWebsite.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ShortTextAndStatusChecks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE cards
                SET Email = LEFT(Email, 255)
                WHERE Email IS NOT NULL AND CHAR_LENGTH(Email) > 255;

                UPDATE cards
                SET CardStatus = CASE
                    WHEN CardStatus IS NULL OR TRIM(CardStatus) = '' THEN NULL
                    WHEN LOWER(TRIM(CardStatus)) = 'active' THEN 'Active'
                    WHEN LOWER(TRIM(CardStatus)) = 'suspended' THEN 'Suspended'
                    WHEN LOWER(TRIM(CardStatus)) = 'inactive' THEN 'Inactive'
                    WHEN TRIM(CardStatus) = 'InActive' THEN 'InActive'
                    ELSE 'Active'
                END;

                UPDATE invoices
                SET BillingCity = LEFT(IFNULL(BillingCity, ''), 100),
                    BillingEmail = LEFT(IFNULL(BillingEmail, ''), 255),
                    PaymentStatus = CASE
                        WHEN LOWER(TRIM(PaymentStatus)) = 'created' THEN 'Created'
                        WHEN LOWER(TRIM(PaymentStatus)) = 'pending' THEN 'Pending'
                        WHEN LOWER(TRIM(PaymentStatus)) = 'success' THEN 'Success'
                        WHEN LOWER(TRIM(PaymentStatus)) = 'paid' THEN 'Paid'
                        WHEN LOWER(TRIM(PaymentStatus)) = 'failed' THEN 'Failed'
                        WHEN LOWER(TRIM(PaymentStatus)) = 'rejected' THEN 'Rejected'
                        WHEN LOWER(TRIM(PaymentStatus)) = 'refunded' THEN 'Refunded'
                        ELSE 'Pending'
                    END;

                UPDATE orders
                SET CustomerEmail = LEFT(CustomerEmail, 255)
                WHERE CustomerEmail IS NOT NULL AND CHAR_LENGTH(CustomerEmail) > 255;

                UPDATE customers
                SET AreaCity = LEFT(AreaCity, 100),
                    EmailId = LEFT(EmailId, 255),
                    Status = CASE
                        WHEN TRIM(Status) IN (
                            'Followup required', 'Phone Busy/Not Picked', 'Important', 'Joined',
                            'Deal Done', 'Profile Shared', 'Interested', 'Not Interested')
                        THEN TRIM(Status)
                        ELSE 'Followup required'
                    END;

                UPDATE coupons
                SET Status = CASE
                    WHEN LOWER(TRIM(Status)) = 'active' THEN 'Active'
                    WHEN LOWER(TRIM(Status)) IN ('inactive', 'in active') OR TRIM(Status) = 'InActive' THEN 'InActive'
                    WHEN LOWER(TRIM(Status)) = 'expired' THEN 'Expired'
                    ELSE 'InActive'
                END;

                UPDATE referral_earnings
                SET Status = CASE
                    WHEN LOWER(TRIM(Status)) = 'pending' THEN 'Pending'
                    WHEN LOWER(TRIM(Status)) = 'paid' THEN 'Paid'
                    WHEN LOWER(TRIM(Status)) = 'partial' THEN 'Partial'
                    WHEN LOWER(TRIM(Status)) = 'rejected' THEN 'Rejected'
                    ELSE 'Pending'
                END;
                """);

            migrationBuilder.AlterColumn<string>(name: "Email", table: "cards", type: "varchar(255)", maxLength: 255, nullable: true, oldClrType: typeof(string), oldType: "longtext", oldNullable: true).Annotation("MySql:CharSet", "utf8mb4").OldAnnotation("MySql:CharSet", "utf8mb4");
            migrationBuilder.AlterColumn<string>(name: "CardStatus", table: "cards", type: "varchar(20)", maxLength: 20, nullable: true, oldClrType: typeof(string), oldType: "longtext", oldNullable: true).Annotation("MySql:CharSet", "utf8mb4").OldAnnotation("MySql:CharSet", "utf8mb4");
            migrationBuilder.AlterColumn<string>(name: "BillingCity", table: "invoices", type: "varchar(100)", maxLength: 100, nullable: false, oldClrType: typeof(string), oldType: "longtext").Annotation("MySql:CharSet", "utf8mb4").OldAnnotation("MySql:CharSet", "utf8mb4");
            migrationBuilder.AlterColumn<string>(name: "BillingEmail", table: "invoices", type: "varchar(255)", maxLength: 255, nullable: false, oldClrType: typeof(string), oldType: "longtext").Annotation("MySql:CharSet", "utf8mb4").OldAnnotation("MySql:CharSet", "utf8mb4");
            migrationBuilder.AlterColumn<string>(name: "PaymentStatus", table: "invoices", type: "varchar(20)", maxLength: 20, nullable: false, oldClrType: typeof(string), oldType: "longtext").Annotation("MySql:CharSet", "utf8mb4").OldAnnotation("MySql:CharSet", "utf8mb4");
            migrationBuilder.AlterColumn<string>(name: "CustomerEmail", table: "orders", type: "varchar(255)", maxLength: 255, nullable: true, oldClrType: typeof(string), oldType: "longtext", oldNullable: true).Annotation("MySql:CharSet", "utf8mb4").OldAnnotation("MySql:CharSet", "utf8mb4");
            migrationBuilder.AlterColumn<string>(name: "AreaCity", table: "customers", type: "varchar(100)", maxLength: 100, nullable: true, oldClrType: typeof(string), oldType: "longtext", oldNullable: true).Annotation("MySql:CharSet", "utf8mb4").OldAnnotation("MySql:CharSet", "utf8mb4");
            migrationBuilder.AlterColumn<string>(name: "EmailId", table: "customers", type: "varchar(255)", maxLength: 255, nullable: true, oldClrType: typeof(string), oldType: "longtext", oldNullable: true).Annotation("MySql:CharSet", "utf8mb4").OldAnnotation("MySql:CharSet", "utf8mb4");
            migrationBuilder.AlterColumn<string>(name: "Status", table: "customers", type: "varchar(40)", maxLength: 40, nullable: false, oldClrType: typeof(string), oldType: "longtext").Annotation("MySql:CharSet", "utf8mb4").OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddCheckConstraint(name: "CK_cards_CardStatus", table: "cards", sql: "`CardStatus` IS NULL OR `CardStatus` IN ('Active', 'Suspended', 'Inactive', 'InActive')");
            migrationBuilder.AddCheckConstraint(name: "CK_invoices_PaymentStatus", table: "invoices", sql: "`PaymentStatus` IN ('Created', 'Pending', 'Success', 'Paid', 'Failed', 'Rejected', 'Refunded')");
            migrationBuilder.AddCheckConstraint(name: "CK_customers_Status", table: "customers", sql: "`Status` IN ('Followup required', 'Phone Busy/Not Picked', 'Important', 'Joined', 'Deal Done', 'Profile Shared', 'Interested', 'Not Interested')");
            migrationBuilder.AddCheckConstraint(name: "CK_coupons_Status", table: "coupons", sql: "`Status` IN ('Active', 'InActive', 'Expired')");
            migrationBuilder.AddCheckConstraint(name: "CK_referral_earnings_Status", table: "referral_earnings", sql: "`Status` IN ('Pending', 'Paid', 'Partial', 'Rejected')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(name: "CK_cards_CardStatus", table: "cards");
            migrationBuilder.DropCheckConstraint(name: "CK_invoices_PaymentStatus", table: "invoices");
            migrationBuilder.DropCheckConstraint(name: "CK_customers_Status", table: "customers");
            migrationBuilder.DropCheckConstraint(name: "CK_coupons_Status", table: "coupons");
            migrationBuilder.DropCheckConstraint(name: "CK_referral_earnings_Status", table: "referral_earnings");

            migrationBuilder.AlterColumn<string>(name: "Email", table: "cards", type: "longtext", nullable: true, oldClrType: typeof(string), oldType: "varchar(255)", oldMaxLength: 255, oldNullable: true).Annotation("MySql:CharSet", "utf8mb4").OldAnnotation("MySql:CharSet", "utf8mb4");
            migrationBuilder.AlterColumn<string>(name: "CardStatus", table: "cards", type: "longtext", nullable: true, oldClrType: typeof(string), oldType: "varchar(20)", oldMaxLength: 20, oldNullable: true).Annotation("MySql:CharSet", "utf8mb4").OldAnnotation("MySql:CharSet", "utf8mb4");
            migrationBuilder.AlterColumn<string>(name: "BillingCity", table: "invoices", type: "longtext", nullable: false, oldClrType: typeof(string), oldType: "varchar(100)", oldMaxLength: 100).Annotation("MySql:CharSet", "utf8mb4").OldAnnotation("MySql:CharSet", "utf8mb4");
            migrationBuilder.AlterColumn<string>(name: "BillingEmail", table: "invoices", type: "longtext", nullable: false, oldClrType: typeof(string), oldType: "varchar(255)", oldMaxLength: 255).Annotation("MySql:CharSet", "utf8mb4").OldAnnotation("MySql:CharSet", "utf8mb4");
            migrationBuilder.AlterColumn<string>(name: "PaymentStatus", table: "invoices", type: "longtext", nullable: false, oldClrType: typeof(string), oldType: "varchar(20)", oldMaxLength: 20).Annotation("MySql:CharSet", "utf8mb4").OldAnnotation("MySql:CharSet", "utf8mb4");
            migrationBuilder.AlterColumn<string>(name: "CustomerEmail", table: "orders", type: "longtext", nullable: true, oldClrType: typeof(string), oldType: "varchar(255)", oldMaxLength: 255, oldNullable: true).Annotation("MySql:CharSet", "utf8mb4").OldAnnotation("MySql:CharSet", "utf8mb4");
            migrationBuilder.AlterColumn<string>(name: "AreaCity", table: "customers", type: "longtext", nullable: true, oldClrType: typeof(string), oldType: "varchar(100)", oldMaxLength: 100, oldNullable: true).Annotation("MySql:CharSet", "utf8mb4").OldAnnotation("MySql:CharSet", "utf8mb4");
            migrationBuilder.AlterColumn<string>(name: "EmailId", table: "customers", type: "longtext", nullable: true, oldClrType: typeof(string), oldType: "varchar(255)", oldMaxLength: 255, oldNullable: true).Annotation("MySql:CharSet", "utf8mb4").OldAnnotation("MySql:CharSet", "utf8mb4");
            migrationBuilder.AlterColumn<string>(name: "Status", table: "customers", type: "longtext", nullable: false, oldClrType: typeof(string), oldType: "varchar(40)", oldMaxLength: 40).Annotation("MySql:CharSet", "utf8mb4").OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}
