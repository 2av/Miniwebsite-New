using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniWebsite.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameDealsToCoupons : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_deal_customer_mappings_deals_DealId",
                table: "deal_customer_mappings");

            migrationBuilder.DropForeignKey(
                name: "FK_pricing_plans_deals_DealId",
                table: "pricing_plans");

            migrationBuilder.DropForeignKey(
                name: "FK_referral_earnings_deals_DealId",
                table: "referral_earnings");

            migrationBuilder.RenameTable(
                name: "deals",
                newName: "coupons");

            migrationBuilder.RenameIndex(
                name: "IX_deals_CouponCode",
                table: "coupons",
                newName: "IX_coupons_CouponCode");

            migrationBuilder.RenameColumn(
                name: "DealName",
                table: "coupons",
                newName: "CouponName");

            migrationBuilder.RenameColumn(
                name: "DealStatus",
                table: "coupons",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "MaxUsage",
                table: "coupons",
                newName: "UsageLimitPerCoupon");

            migrationBuilder.AddColumn<int>(
                name: "RoleId",
                table: "coupons",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PromoDiscountType",
                table: "coupons",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Amount")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "PromoDiscountValue",
                table: "coupons",
                type: "decimal(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "UsageLimitPerUser",
                table: "coupons",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Plan Type text -> role: MiniWebsite = ShareBizz User (SBU), Franchise = Franchise Partner (FRP).
            migrationBuilder.Sql("""
                UPDATE coupons
                SET RoleId = (SELECT r.RoleId FROM roles r WHERE r.Prefix = 'SBU' ORDER BY r.RoleId LIMIT 1)
                WHERE PlanType = 'MiniWebsite';
                """);
            migrationBuilder.Sql("""
                UPDATE coupons
                SET RoleId = (SELECT r.RoleId FROM roles r WHERE r.Prefix = 'FRP' ORDER BY r.RoleId LIMIT 1)
                WHERE PlanType IN ('Franchise', 'Franchisee');
                """);

            // Referee discount (amount or %) -> one promo discount value plus its type; amount wins when both are set.
            migrationBuilder.Sql("""
                UPDATE coupons
                SET PromoDiscountType = CASE WHEN DiscountAmount <= 0 AND DiscountPercentage > 0 THEN 'Percentage' ELSE 'Amount' END,
                    PromoDiscountValue = CASE WHEN DiscountAmount <= 0 AND DiscountPercentage > 0 THEN DiscountPercentage ELSE DiscountAmount END;
                """);

            migrationBuilder.Sql("""
                UPDATE coupons
                SET CouponType = CASE WHEN CouponType IN ('Festive Discount', 'Festive') THEN 'Festive Discount' ELSE 'Promotional Discount' END;
                """);

            migrationBuilder.Sql("""
                UPDATE coupons
                SET Status = CASE WHEN Status = 'Active' THEN 'Active' ELSE 'InActive' END;
                """);

            migrationBuilder.DropColumn(
                name: "PlanType",
                table: "coupons");

            migrationBuilder.DropColumn(
                name: "PlanName",
                table: "coupons");

            migrationBuilder.DropColumn(
                name: "DealState",
                table: "coupons");

            migrationBuilder.DropColumn(
                name: "DiscountAmount",
                table: "coupons");

            migrationBuilder.DropColumn(
                name: "DiscountPercentage",
                table: "coupons");

            migrationBuilder.CreateIndex(
                name: "IX_coupons_RoleId",
                table: "coupons",
                column: "RoleId");

            migrationBuilder.AddForeignKey(
                name: "FK_coupons_roles_RoleId",
                table: "coupons",
                column: "RoleId",
                principalTable: "roles",
                principalColumn: "RoleId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_deal_customer_mappings_coupons_DealId",
                table: "deal_customer_mappings",
                column: "DealId",
                principalTable: "coupons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_pricing_plans_coupons_DealId",
                table: "pricing_plans",
                column: "DealId",
                principalTable: "coupons",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_referral_earnings_coupons_DealId",
                table: "referral_earnings",
                column: "DealId",
                principalTable: "coupons",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_deal_customer_mappings_coupons_DealId",
                table: "deal_customer_mappings");

            migrationBuilder.DropForeignKey(
                name: "FK_pricing_plans_coupons_DealId",
                table: "pricing_plans");

            migrationBuilder.DropForeignKey(
                name: "FK_referral_earnings_coupons_DealId",
                table: "referral_earnings");

            migrationBuilder.DropForeignKey(
                name: "FK_coupons_roles_RoleId",
                table: "coupons");

            migrationBuilder.DropIndex(
                name: "IX_coupons_RoleId",
                table: "coupons");

            migrationBuilder.AddColumn<string>(
                name: "PlanType",
                table: "coupons",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "PlanName",
                table: "coupons",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "DealState",
                table: "coupons",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmount",
                table: "coupons",
                type: "decimal(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountPercentage",
                table: "coupons",
                type: "decimal(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.Sql("""
                UPDATE coupons c
                LEFT JOIN roles r ON r.RoleId = c.RoleId
                SET c.PlanType = CASE WHEN r.Prefix = 'FRP' THEN 'Franchise' ELSE 'MiniWebsite' END,
                    c.PlanName = 'Basic',
                    c.DealState = '',
                    c.DiscountAmount = CASE WHEN c.PromoDiscountType = 'Percentage' THEN 0 ELSE c.PromoDiscountValue END,
                    c.DiscountPercentage = CASE WHEN c.PromoDiscountType = 'Percentage' THEN c.PromoDiscountValue ELSE 0 END,
                    c.Status = CASE WHEN c.Status = 'Active' THEN 'Active' ELSE 'Inactive' END;
                """);

            migrationBuilder.DropColumn(
                name: "RoleId",
                table: "coupons");

            migrationBuilder.DropColumn(
                name: "PromoDiscountType",
                table: "coupons");

            migrationBuilder.DropColumn(
                name: "PromoDiscountValue",
                table: "coupons");

            migrationBuilder.DropColumn(
                name: "UsageLimitPerUser",
                table: "coupons");

            migrationBuilder.RenameColumn(
                name: "UsageLimitPerCoupon",
                table: "coupons",
                newName: "MaxUsage");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "coupons",
                newName: "DealStatus");

            migrationBuilder.RenameColumn(
                name: "CouponName",
                table: "coupons",
                newName: "DealName");

            migrationBuilder.RenameIndex(
                name: "IX_coupons_CouponCode",
                table: "coupons",
                newName: "IX_deals_CouponCode");

            migrationBuilder.RenameTable(
                name: "coupons",
                newName: "deals");

            migrationBuilder.AddForeignKey(
                name: "FK_deal_customer_mappings_deals_DealId",
                table: "deal_customer_mappings",
                column: "DealId",
                principalTable: "deals",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_pricing_plans_deals_DealId",
                table: "pricing_plans",
                column: "DealId",
                principalTable: "deals",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_referral_earnings_deals_DealId",
                table: "referral_earnings",
                column: "DealId",
                principalTable: "deals",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
