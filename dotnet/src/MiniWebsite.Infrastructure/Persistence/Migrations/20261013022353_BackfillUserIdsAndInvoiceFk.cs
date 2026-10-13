using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniWebsite.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BackfillUserIdsAndInvoiceFk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE cards c JOIN users u ON u.Email = TRIM(c.UserEmail)
                SET c.UserId = u.Id
                WHERE c.UserId IS NULL;
                """);
            migrationBuilder.Sql("""
                UPDATE invoices i JOIN users u ON u.Email = TRIM(i.UserEmail)
                SET i.UserId = u.Id
                WHERE i.UserId IS NULL;
                """);
            migrationBuilder.Sql("""
                UPDATE referral_earnings r JOIN users u ON u.Email = TRIM(r.ReferrerEmail)
                SET r.ReferrerUserId = u.Id
                WHERE r.ReferrerUserId IS NULL;
                """);
            migrationBuilder.Sql("""
                UPDATE referral_earnings r JOIN users u ON u.Email = TRIM(r.ReferredEmail)
                SET r.ReferredUserId = u.Id
                WHERE r.ReferredUserId IS NULL;
                """);
            migrationBuilder.Sql("""
                UPDATE user_joining_deals d LEFT JOIN invoices i ON i.Id = d.InvoiceId
                SET d.InvoiceId = NULL
                WHERE d.InvoiceId IS NOT NULL AND i.Id IS NULL;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_user_joining_deals_InvoiceId",
                table: "user_joining_deals",
                column: "InvoiceId");

            migrationBuilder.AddForeignKey(
                name: "FK_user_joining_deals_invoices_InvoiceId",
                table: "user_joining_deals",
                column: "InvoiceId",
                principalTable: "invoices",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_user_joining_deals_invoices_InvoiceId",
                table: "user_joining_deals");

            migrationBuilder.DropIndex(
                name: "IX_user_joining_deals_InvoiceId",
                table: "user_joining_deals");
        }
    }
}
