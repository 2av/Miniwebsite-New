using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniWebsite.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReferralCommissionCodeAndComment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdminComment",
                table: "referral_earnings",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "CommissionCode",
                table: "referral_earnings",
                type: "varchar(30)",
                maxLength: 30,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_referral_earnings_CommissionCode",
                table: "referral_earnings",
                column: "CommissionCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_referral_earnings_CommissionCode",
                table: "referral_earnings");

            migrationBuilder.DropColumn(
                name: "AdminComment",
                table: "referral_earnings");

            migrationBuilder.DropColumn(
                name: "CommissionCode",
                table: "referral_earnings");
        }
    }
}
