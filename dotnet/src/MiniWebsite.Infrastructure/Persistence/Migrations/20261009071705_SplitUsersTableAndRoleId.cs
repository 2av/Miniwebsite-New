using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniWebsite.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Replaces users.Role with the users.RoleId → roles mapping and moves address, payment and
    /// referral columns into one-row-per-user tables. Data is copied before any column is dropped.
    /// </summary>
    public partial class SplitUsersTableAndRoleId : Migration
    {
        private static readonly (string LegacyRole, string Prefix)[] RoleBackfill =
        [
            ("ADMIN", "ADM"),
            ("CUSTOMER", "SBU"),
            ("FRANCHISEE", "FRP"),
            ("TEAM", "DMP"),
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Snapshot of the pre-split table; drop manually once the split is verified.
            migrationBuilder.Sql("CREATE TABLE IF NOT EXISTS `users_backup_20261009` AS SELECT * FROM `users`;");

            // Admins need a role row before RoleId can become required.
            migrationBuilder.Sql(@"
INSERT INTO `roles` (`RoleName`, `RoleDescription`, `Prefix`, `SalesType`, `CreatedAt`, `UpdatedAt`)
SELECT 'Administrator', 'Admin panel user', 'ADM', 'NA', UTC_TIMESTAMP(), UTC_TIMESTAMP()
WHERE NOT EXISTS (SELECT 1 FROM `roles` WHERE `Prefix` = 'ADM');");

            migrationBuilder.CreateTable(
                name: "user_addresses",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    AddressLine = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    City = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    District = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    State = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Pincode = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Gst = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_addresses", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_user_addresses_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "user_payment_details",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    GooglePay = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Paytm = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RzPay = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RzPay2 = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_payment_details", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_user_payment_details_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "user_referral_settings",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ReferralCode = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReferredBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MwReferralId = table.Column<int>(type: "int", nullable: true),
                    SenderUserId = table.Column<int>(type: "int", nullable: true),
                    Influencer = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CollaborationEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    SaleskitEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_referral_settings", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_user_referral_settings_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_user_referral_settings_ReferralCode",
                table: "user_referral_settings",
                column: "ReferralCode");

            // Every user (including soft-deleted) gets exactly one row in each table.
            migrationBuilder.Sql(@"
INSERT INTO `user_addresses` (`UserId`, `AddressLine`, `City`, `District`, `State`, `Pincode`, `Gst`)
SELECT `Id`, `Address`, `City`, `District`, `State`, `Pincode`, `Gst` FROM `users`;");

            migrationBuilder.Sql(@"
INSERT INTO `user_payment_details` (`UserId`, `GooglePay`, `Paytm`, `RzPay`, `RzPay2`)
SELECT `Id`, `GooglePay`, `Paytm`, `RzPay`, `RzPay2` FROM `users`;");

            migrationBuilder.Sql(@"
INSERT INTO `user_referral_settings`
    (`UserId`, `ReferralCode`, `ReferredBy`, `MwReferralId`, `SenderUserId`, `Influencer`, `CollaborationEnabled`, `SaleskitEnabled`)
SELECT `Id`, `ReferralCode`, `ReferredBy`, `MwReferralId`, `SenderUserId`, `Influencer`, `CollaborationEnabled`, `SaleskitEnabled`
FROM `users`;");

            foreach (var (legacyRole, prefix) in RoleBackfill)
            {
                migrationBuilder.Sql($@"
UPDATE `users` u
JOIN (SELECT MIN(`RoleId`) AS `RoleId` FROM `roles` WHERE `Prefix` = '{prefix}') r ON r.`RoleId` IS NOT NULL
SET u.`RoleId` = r.`RoleId`
WHERE u.`RoleId` IS NULL AND UPPER(TRIM(u.`Role`)) = '{legacyRole}';");
            }

            // The old converter treated unknown role values as CUSTOMER.
            migrationBuilder.Sql(@"
UPDATE `users` u
JOIN (SELECT MIN(`RoleId`) AS `RoleId` FROM `roles` WHERE `Prefix` = 'SBU') r ON r.`RoleId` IS NOT NULL
SET u.`RoleId` = r.`RoleId`
WHERE u.`RoleId` IS NULL;");

            migrationBuilder.AlterColumn<int>(
                name: "RoleId",
                table: "users",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.DropIndex(
                name: "IX_users_ReferralCode",
                table: "users");

            foreach (var column in MovedColumns)
                migrationBuilder.DropColumn(name: column, table: "users");

            migrationBuilder.DropColumn(name: "Role", table: "users");
        }

        private static readonly string[] MovedColumns =
        [
            "Address", "City", "District", "State", "Pincode", "Gst",
            "GooglePay", "Paytm", "RzPay", "RzPay2",
            "ReferralCode", "ReferredBy", "MwReferralId", "SenderUserId",
            "Influencer", "CollaborationEnabled", "SaleskitEnabled",
        ];

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(name: "Role", table: "users", type: "varchar(20)", maxLength: 20, nullable: false, defaultValue: "CUSTOMER")
                .Annotation("MySql:CharSet", "utf8mb4");
            migrationBuilder.AddColumn<string>(name: "Address", table: "users", type: "varchar(500)", maxLength: 500, nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
            migrationBuilder.AddColumn<string>(name: "City", table: "users", type: "varchar(100)", maxLength: 100, nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
            migrationBuilder.AddColumn<string>(name: "District", table: "users", type: "varchar(100)", maxLength: 100, nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
            migrationBuilder.AddColumn<string>(name: "State", table: "users", type: "varchar(100)", maxLength: 100, nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
            migrationBuilder.AddColumn<string>(name: "Pincode", table: "users", type: "varchar(20)", maxLength: 20, nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
            migrationBuilder.AddColumn<string>(name: "Gst", table: "users", type: "varchar(50)", maxLength: 50, nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
            migrationBuilder.AddColumn<string>(name: "GooglePay", table: "users", type: "varchar(200)", maxLength: 200, nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
            migrationBuilder.AddColumn<string>(name: "Paytm", table: "users", type: "varchar(200)", maxLength: 200, nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
            migrationBuilder.AddColumn<string>(name: "RzPay", table: "users", type: "varchar(300)", maxLength: 300, nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
            migrationBuilder.AddColumn<string>(name: "RzPay2", table: "users", type: "varchar(300)", maxLength: 300, nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
            migrationBuilder.AddColumn<string>(name: "ReferralCode", table: "users", type: "varchar(50)", maxLength: 50, nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
            migrationBuilder.AddColumn<string>(name: "ReferredBy", table: "users", type: "longtext", nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
            migrationBuilder.AddColumn<int>(name: "MwReferralId", table: "users", type: "int", nullable: true);
            migrationBuilder.AddColumn<int>(name: "SenderUserId", table: "users", type: "int", nullable: true);
            migrationBuilder.AddColumn<bool>(name: "Influencer", table: "users", type: "tinyint(1)", nullable: false, defaultValue: false);
            migrationBuilder.AddColumn<bool>(name: "CollaborationEnabled", table: "users", type: "tinyint(1)", nullable: false, defaultValue: false);
            migrationBuilder.AddColumn<bool>(name: "SaleskitEnabled", table: "users", type: "tinyint(1)", nullable: false, defaultValue: false);

            migrationBuilder.Sql(@"
UPDATE `users` u JOIN `user_addresses` a ON a.`UserId` = u.`Id`
SET u.`Address` = a.`AddressLine`, u.`City` = a.`City`, u.`District` = a.`District`,
    u.`State` = a.`State`, u.`Pincode` = a.`Pincode`, u.`Gst` = a.`Gst`;");

            migrationBuilder.Sql(@"
UPDATE `users` u JOIN `user_payment_details` p ON p.`UserId` = u.`Id`
SET u.`GooglePay` = p.`GooglePay`, u.`Paytm` = p.`Paytm`, u.`RzPay` = p.`RzPay`, u.`RzPay2` = p.`RzPay2`;");

            migrationBuilder.Sql(@"
UPDATE `users` u JOIN `user_referral_settings` r ON r.`UserId` = u.`Id`
SET u.`ReferralCode` = r.`ReferralCode`, u.`ReferredBy` = r.`ReferredBy`, u.`MwReferralId` = r.`MwReferralId`,
    u.`SenderUserId` = r.`SenderUserId`, u.`Influencer` = r.`Influencer`,
    u.`CollaborationEnabled` = r.`CollaborationEnabled`, u.`SaleskitEnabled` = r.`SaleskitEnabled`;");

            migrationBuilder.Sql(@"
UPDATE `users` u JOIN `roles` ro ON ro.`RoleId` = u.`RoleId`
SET u.`Role` = CASE
    WHEN ro.`Prefix` = 'FRP' THEN 'FRANCHISEE'
    WHEN ro.`Prefix` IN ('DMP', 'PSP', 'SMP', 'FST', 'FSP') THEN 'TEAM'
    WHEN ro.`Prefix` = 'ADM' THEN 'ADMIN'
    ELSE 'CUSTOMER' END;");

            migrationBuilder.CreateIndex(
                name: "IX_users_ReferralCode",
                table: "users",
                column: "ReferralCode");

            migrationBuilder.AlterColumn<int>(
                name: "RoleId",
                table: "users",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.DropTable(name: "user_addresses");
            migrationBuilder.DropTable(name: "user_payment_details");
            migrationBuilder.DropTable(name: "user_referral_settings");
        }
    }
}
