using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniWebsite.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261009070000_AddUserRoleId")]
    public partial class AddUserRoleId : Migration
    {
        private static readonly (string LegacyRole, string Prefix)[] Backfill =
        [
            ("CUSTOMER", "SBU"),
            ("FRANCHISEE", "FRP"),
            ("TEAM", "DMP"),
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RoleId",
                table: "users",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_RoleId",
                table: "users",
                column: "RoleId");

            migrationBuilder.AddForeignKey(
                name: "FK_users_roles_RoleId",
                table: "users",
                column: "RoleId",
                principalTable: "roles",
                principalColumn: "RoleId",
                onDelete: ReferentialAction.Restrict);

            foreach (var (legacyRole, prefix) in Backfill)
            {
                migrationBuilder.Sql($@"
UPDATE `users` u
JOIN (SELECT MIN(`RoleId`) AS `RoleId` FROM `roles` WHERE `Prefix` = '{prefix}') r ON r.`RoleId` IS NOT NULL
SET u.`RoleId` = r.`RoleId`
WHERE u.`RoleId` IS NULL AND u.`Role` = '{legacyRole}';");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_users_roles_RoleId",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_users_RoleId",
                table: "users");

            migrationBuilder.DropColumn(
                name: "RoleId",
                table: "users");
        }
    }
}
