using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniWebsite.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261009053000_AddRoles")]
    public partial class AddRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // IF NOT EXISTS keeps this safe on databases where `roles` was created by hand.
            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS `roles` (
    `RoleId` int NOT NULL AUTO_INCREMENT,
    `RoleName` varchar(120) CHARACTER SET utf8mb4 NOT NULL,
    `RoleDescription` varchar(500) CHARACTER SET utf8mb4 NULL,
    `Prefix` varchar(40) CHARACTER SET utf8mb4 NULL,
    `SalesType` varchar(80) CHARACTER SET utf8mb4 NULL,
    `CreatedAt` datetime(6) NULL,
    `UpdatedAt` datetime(6) NULL,
    CONSTRAINT `PK_roles` PRIMARY KEY (`RoleId`),
    UNIQUE KEY `IX_roles_RoleName` (`RoleName`),
    KEY `IX_roles_SalesType` (`SalesType`)
) CHARACTER SET=utf8mb4;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS `roles`;");
        }
    }
}
