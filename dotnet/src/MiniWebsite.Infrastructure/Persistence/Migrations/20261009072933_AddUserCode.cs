using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniWebsite.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Adds users.UserCode (role prefix + 8 random uppercase letters/digits) and backfills existing users.
    /// New users get their code from ApplicationDbContext.SaveChangesAsync.
    /// </summary>
    public partial class AddUserCode : Migration
    {
        private const string RandomChar =
            "SUBSTRING('ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789', FLOOR(1 + RAND() * 36), 1)";

        /// <summary>Passes that regenerate codes that are missing, not a letter/digit mix, or duplicated.</summary>
        private const int BackfillPasses = 10;

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserCode",
                table: "users",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            var randomSuffix = string.Join(", ", Enumerable.Repeat(RandomChar, 8));
            for (var pass = 0; pass < BackfillPasses; pass++)
            {
                migrationBuilder.Sql($@"
UPDATE `users` u
LEFT JOIN `roles` r ON r.`RoleId` = u.`RoleId`
SET u.`UserCode` = CONCAT(UPPER(COALESCE(NULLIF(TRIM(r.`Prefix`), ''), 'USR')), {randomSuffix})
WHERE u.`UserCode` IS NULL
   OR RIGHT(u.`UserCode`, 8) REGEXP '^[A-Z]+$'
   OR RIGHT(u.`UserCode`, 8) REGEXP '^[0-9]+$'
   OR u.`UserCode` IN (
       SELECT d.`UserCode` FROM (
           SELECT `UserCode` FROM `users` WHERE `UserCode` IS NOT NULL GROUP BY `UserCode` HAVING COUNT(*) > 1
       ) d);");
            }

            migrationBuilder.AlterColumn<string>(
                name: "UserCode",
                table: "users",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldMaxLength: 50,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_users_UserCode",
                table: "users",
                column: "UserCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_users_UserCode",
                table: "users");

            migrationBuilder.DropColumn(
                name: "UserCode",
                table: "users");
        }
    }
}
