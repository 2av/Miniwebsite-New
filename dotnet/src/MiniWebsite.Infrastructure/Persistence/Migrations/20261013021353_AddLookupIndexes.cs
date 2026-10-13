using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniWebsite.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLookupIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ReferredBy",
                table: "user_referral_settings",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "UserEmail",
                table: "invoices",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_users_RoleId_CreatedAt",
                table: "users",
                columns: new[] { "RoleId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_user_referral_settings_ReferredBy",
                table: "user_referral_settings",
                column: "ReferredBy");

            migrationBuilder.CreateIndex(
                name: "IX_user_bank_details_UserEmail",
                table: "user_bank_details",
                column: "UserEmail");

            migrationBuilder.CreateIndex(
                name: "IX_referral_earnings_ReferredEmail",
                table: "referral_earnings",
                column: "ReferredEmail");

            migrationBuilder.CreateIndex(
                name: "IX_referral_earnings_ReferrerEmail",
                table: "referral_earnings",
                column: "ReferrerEmail");

            migrationBuilder.CreateIndex(
                name: "IX_invoices_UserEmail",
                table: "invoices",
                column: "UserEmail");

            migrationBuilder.CreateIndex(
                name: "IX_franchise_verifications_UserEmail",
                table: "franchise_verifications",
                column: "UserEmail");

            migrationBuilder.CreateIndex(
                name: "IX_deal_customer_mappings_CustomerEmail",
                table: "deal_customer_mappings",
                column: "CustomerEmail");

            migrationBuilder.CreateIndex(
                name: "IX_cards_FranchiseeEmail",
                table: "cards",
                column: "FranchiseeEmail");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_users_RoleId_CreatedAt",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_user_referral_settings_ReferredBy",
                table: "user_referral_settings");

            migrationBuilder.DropIndex(
                name: "IX_user_bank_details_UserEmail",
                table: "user_bank_details");

            migrationBuilder.DropIndex(
                name: "IX_referral_earnings_ReferredEmail",
                table: "referral_earnings");

            migrationBuilder.DropIndex(
                name: "IX_referral_earnings_ReferrerEmail",
                table: "referral_earnings");

            migrationBuilder.DropIndex(
                name: "IX_invoices_UserEmail",
                table: "invoices");

            migrationBuilder.DropIndex(
                name: "IX_franchise_verifications_UserEmail",
                table: "franchise_verifications");

            migrationBuilder.DropIndex(
                name: "IX_deal_customer_mappings_CustomerEmail",
                table: "deal_customer_mappings");

            migrationBuilder.DropIndex(
                name: "IX_cards_FranchiseeEmail",
                table: "cards");

            migrationBuilder.AlterColumn<string>(
                name: "ReferredBy",
                table: "user_referral_settings",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldMaxLength: 255,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "UserEmail",
                table: "invoices",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldMaxLength: 255)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}
