using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniWebsite.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFranchiseeAndReferrerUserIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ReferrerUserId",
                table: "user_referral_settings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReferrerUserId",
                table: "registration_pending",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FranchiseeUserId",
                table: "cards",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_referral_settings_ReferrerUserId",
                table: "user_referral_settings",
                column: "ReferrerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_registration_pending_ReferrerUserId",
                table: "registration_pending",
                column: "ReferrerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_cards_FranchiseeUserId",
                table: "cards",
                column: "FranchiseeUserId");

            migrationBuilder.Sql("""
                UPDATE cards c JOIN users u ON u.Email = TRIM(c.FranchiseeEmail)
                SET c.FranchiseeUserId = u.Id
                WHERE c.FranchiseeUserId IS NULL AND TRIM(c.FranchiseeEmail) <> '';
                """);
            migrationBuilder.Sql("""
                UPDATE user_referral_settings s JOIN users u ON u.Email = TRIM(s.ReferredBy)
                SET s.ReferrerUserId = u.Id
                WHERE s.ReferrerUserId IS NULL AND TRIM(s.ReferredBy) <> '';
                """);
            migrationBuilder.Sql("""
                UPDATE registration_pending p JOIN users u ON u.Email = TRIM(p.ReferrerEmail)
                SET p.ReferrerUserId = u.Id
                WHERE p.ReferrerUserId IS NULL AND TRIM(p.ReferrerEmail) <> '';
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_cards_users_FranchiseeUserId",
                table: "cards",
                column: "FranchiseeUserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_registration_pending_users_ReferrerUserId",
                table: "registration_pending",
                column: "ReferrerUserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_user_referral_settings_users_ReferrerUserId",
                table: "user_referral_settings",
                column: "ReferrerUserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_cards_users_FranchiseeUserId",
                table: "cards");

            migrationBuilder.DropForeignKey(
                name: "FK_registration_pending_users_ReferrerUserId",
                table: "registration_pending");

            migrationBuilder.DropForeignKey(
                name: "FK_user_referral_settings_users_ReferrerUserId",
                table: "user_referral_settings");

            migrationBuilder.DropIndex(
                name: "IX_user_referral_settings_ReferrerUserId",
                table: "user_referral_settings");

            migrationBuilder.DropIndex(
                name: "IX_registration_pending_ReferrerUserId",
                table: "registration_pending");

            migrationBuilder.DropIndex(
                name: "IX_cards_FranchiseeUserId",
                table: "cards");

            migrationBuilder.DropColumn(
                name: "ReferrerUserId",
                table: "user_referral_settings");

            migrationBuilder.DropColumn(
                name: "ReferrerUserId",
                table: "registration_pending");

            migrationBuilder.DropColumn(
                name: "FranchiseeUserId",
                table: "cards");
        }
    }
}
