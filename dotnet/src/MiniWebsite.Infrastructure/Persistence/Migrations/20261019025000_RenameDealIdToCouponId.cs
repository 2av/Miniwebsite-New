using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniWebsite.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameDealIdToCouponId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_deal_customer_mappings_coupons_DealId",
                table: "deal_customer_mappings");
            migrationBuilder.DropForeignKey(
                name: "FK_referral_earnings_coupons_DealId",
                table: "referral_earnings");

            migrationBuilder.RenameColumn(
                name: "DealId",
                table: "deal_customer_mappings",
                newName: "CouponId");
            migrationBuilder.RenameColumn(
                name: "DealId",
                table: "referral_earnings",
                newName: "CouponId");

            migrationBuilder.RenameIndex(
                name: "IX_deal_customer_mappings_DealId",
                table: "deal_customer_mappings",
                newName: "IX_deal_customer_mappings_CouponId");
            migrationBuilder.RenameIndex(
                name: "IX_referral_earnings_DealId",
                table: "referral_earnings",
                newName: "IX_referral_earnings_CouponId");

            migrationBuilder.AddForeignKey(
                name: "FK_deal_customer_mappings_coupons_CouponId",
                table: "deal_customer_mappings",
                column: "CouponId",
                principalTable: "coupons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
            migrationBuilder.AddForeignKey(
                name: "FK_referral_earnings_coupons_CouponId",
                table: "referral_earnings",
                column: "CouponId",
                principalTable: "coupons",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_deal_customer_mappings_coupons_CouponId",
                table: "deal_customer_mappings");
            migrationBuilder.DropForeignKey(
                name: "FK_referral_earnings_coupons_CouponId",
                table: "referral_earnings");

            migrationBuilder.RenameColumn(
                name: "CouponId",
                table: "deal_customer_mappings",
                newName: "DealId");
            migrationBuilder.RenameColumn(
                name: "CouponId",
                table: "referral_earnings",
                newName: "DealId");

            migrationBuilder.RenameIndex(
                name: "IX_deal_customer_mappings_CouponId",
                table: "deal_customer_mappings",
                newName: "IX_deal_customer_mappings_DealId");
            migrationBuilder.RenameIndex(
                name: "IX_referral_earnings_CouponId",
                table: "referral_earnings",
                newName: "IX_referral_earnings_DealId");

            migrationBuilder.AddForeignKey(
                name: "FK_deal_customer_mappings_coupons_DealId",
                table: "deal_customer_mappings",
                column: "DealId",
                principalTable: "coupons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
            migrationBuilder.AddForeignKey(
                name: "FK_referral_earnings_coupons_DealId",
                table: "referral_earnings",
                column: "DealId",
                principalTable: "coupons",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
