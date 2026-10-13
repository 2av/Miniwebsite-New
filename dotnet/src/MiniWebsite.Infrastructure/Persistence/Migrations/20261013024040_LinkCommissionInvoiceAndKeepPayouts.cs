using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniWebsite.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class LinkCommissionInvoiceAndKeepPayouts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_referral_payments_referral_earnings_ReferralEarningId",
                table: "referral_payments");

            migrationBuilder.AddColumn<int>(
                name: "InvoiceId",
                table: "referral_earnings",
                type: "int",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE referral_earnings r
                SET r.InvoiceId = (
                    SELECT i.Id FROM invoices i
                    WHERE i.UserEmail = TRIM(r.ReferredEmail)
                    ORDER BY ABS(TIMESTAMPDIFF(MICROSECOND, r.ReferralDate, i.InvoiceDate)), i.Id
                    LIMIT 1)
                WHERE r.InvoiceId IS NULL;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_referral_earnings_InvoiceId",
                table: "referral_earnings",
                column: "InvoiceId");

            migrationBuilder.AddForeignKey(
                name: "FK_referral_earnings_invoices_InvoiceId",
                table: "referral_earnings",
                column: "InvoiceId",
                principalTable: "invoices",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_referral_payments_referral_earnings_ReferralEarningId",
                table: "referral_payments",
                column: "ReferralEarningId",
                principalTable: "referral_earnings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_referral_earnings_invoices_InvoiceId",
                table: "referral_earnings");

            migrationBuilder.DropForeignKey(
                name: "FK_referral_payments_referral_earnings_ReferralEarningId",
                table: "referral_payments");

            migrationBuilder.DropIndex(
                name: "IX_referral_earnings_InvoiceId",
                table: "referral_earnings");

            migrationBuilder.DropColumn(
                name: "InvoiceId",
                table: "referral_earnings");

            migrationBuilder.AddForeignKey(
                name: "FK_referral_payments_referral_earnings_ReferralEarningId",
                table: "referral_payments",
                column: "ReferralEarningId",
                principalTable: "referral_earnings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
