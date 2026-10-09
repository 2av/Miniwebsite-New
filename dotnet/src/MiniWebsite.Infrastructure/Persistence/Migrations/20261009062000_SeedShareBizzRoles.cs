using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniWebsite.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261009062000_SeedShareBizzRoles")]
    public partial class SeedShareBizzRoles : Migration
    {
        private static readonly (string Prefix, string Name, string Description, string SalesType)[] Roles =
        [
            ("SBU", "ShareBizz User", "ShareBizz User (Customer)", "NA"),
            ("SBPU", "ShareBizz Profile User", "ShareBizz Profile User", "NA"),
            ("FRP", "Franchise Partner", "Franchise Partner", "Do Offline Sales"),
            ("DMP", "Digital Marketing Partner", "Digital Marketing Partner", "Do Online Sales"),
            ("PSP", "Print Shop Partner", "Print Shop Partner", "Do Offline Sales"),
            ("SMP", "Social Media Partner", "Social Media Partner", "Do Online Sales"),
            ("FST", "Field Sales Team", "Field Sales Team", "Do Offline Sales"),
            ("FSP", "Field Sales Partner", "Field Sales Partner", "Do Offline Sales"),
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Placeholder catalog from AddRoles' first run; only exact untouched rows are removed.
            migrationBuilder.Sql(@"
DELETE FROM `roles` WHERE
    (`RoleName` = 'Customer' AND `Prefix` = 'MW' AND `SalesType` = 'Direct') OR
    (`RoleName` = 'Franchise Partner' AND `Prefix` = 'FR' AND `SalesType` = 'Franchise') OR
    (`RoleName` = 'Freelance Sales Partner' AND `Prefix` = 'FSP' AND `SalesType` = 'Freelance') OR
    (`RoleName` = 'Influencer Partner' AND `Prefix` = 'INF' AND `SalesType` = 'Influencer') OR
    (`RoleName` = 'Digital Marketing Partner' AND `Prefix` = 'TEAM' AND `SalesType` = 'Digital Marketing') OR
    (`RoleName` = 'Admin' AND `Prefix` = 'ADMIN' AND `SalesType` = 'Internal');");

            foreach (var (prefix, name, description, salesType) in Roles)
            {
                migrationBuilder.Sql($@"
INSERT INTO `roles` (`RoleName`, `RoleDescription`, `Prefix`, `SalesType`, `CreatedAt`, `UpdatedAt`)
SELECT '{name}', '{description}', '{prefix}', '{salesType}', UTC_TIMESTAMP(6), UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `roles` WHERE `RoleName` = '{name}');");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var (prefix, name, _, _) in Roles)
            {
                migrationBuilder.Sql($"DELETE FROM `roles` WHERE `RoleName` = '{name}' AND `Prefix` = '{prefix}';");
            }
        }
    }
}
