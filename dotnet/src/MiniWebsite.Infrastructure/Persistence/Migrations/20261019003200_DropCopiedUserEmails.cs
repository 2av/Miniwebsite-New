using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniWebsite.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DropCopiedUserEmails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_franchise_verifications_UserEmail",
                table: "franchise_verifications");

            migrationBuilder.DropIndex(
                name: "IX_user_bank_details_UserEmail",
                table: "user_bank_details");

            migrationBuilder.DropColumn(
                name: "UserEmail",
                table: "franchise_verifications");

            migrationBuilder.DropColumn(
                name: "UserEmail",
                table: "user_bank_details");

            migrationBuilder.DropColumn(
                name: "UserEmail",
                table: "user_custom_categories");

            migrationBuilder.DropColumn(
                name: "UserEmail",
                table: "user_joining_deals");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserEmail",
                table: "franchise_verifications",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "UserEmail",
                table: "user_bank_details",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "UserEmail",
                table: "user_custom_categories",
                type: "longtext",
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "UserEmail",
                table: "user_joining_deals",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_franchise_verifications_UserEmail",
                table: "franchise_verifications",
                column: "UserEmail");

            migrationBuilder.CreateIndex(
                name: "IX_user_bank_details_UserEmail",
                table: "user_bank_details",
                column: "UserEmail");
        }
    }
}
