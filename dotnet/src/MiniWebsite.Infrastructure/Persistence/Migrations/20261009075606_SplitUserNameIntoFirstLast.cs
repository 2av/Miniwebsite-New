using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniWebsite.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SplitUserNameIntoFirstLast : Migration
    {
        private static readonly string[] Tables = ["users", "registration_pending"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS `users_name_backup_20261009` AS
SELECT `Id`, `Name` FROM `users`;");

            foreach (var table in Tables)
            {
                migrationBuilder.AddColumn<string>(
                    name: "FirstName",
                    table: table,
                    type: "varchar(100)",
                    maxLength: 100,
                    nullable: false,
                    defaultValue: "")
                    .Annotation("MySql:CharSet", "utf8mb4");

                migrationBuilder.AddColumn<string>(
                    name: "LastName",
                    table: table,
                    type: "varchar(100)",
                    maxLength: 100,
                    nullable: false,
                    defaultValue: "")
                    .Annotation("MySql:CharSet", "utf8mb4");

                // First word -> FirstName, the rest -> LastName.
                migrationBuilder.Sql($@"
UPDATE `{table}`
SET `FirstName` = LEFT(SUBSTRING_INDEX(TRIM(COALESCE(`Name`, '')), ' ', 1), 100),
    `LastName` = LEFT(TRIM(SUBSTRING(
        TRIM(COALESCE(`Name`, '')),
        CHAR_LENGTH(SUBSTRING_INDEX(TRIM(COALESCE(`Name`, '')), ' ', 1)) + 1)), 100);");

                migrationBuilder.DropColumn(
                    name: "Name",
                    table: table);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var table in Tables)
            {
                migrationBuilder.AddColumn<string>(
                    name: "Name",
                    table: table,
                    type: "varchar(150)",
                    maxLength: 150,
                    nullable: false,
                    defaultValue: "")
                    .Annotation("MySql:CharSet", "utf8mb4");

                migrationBuilder.Sql($@"
UPDATE `{table}`
SET `Name` = LEFT(TRIM(CONCAT(`FirstName`, ' ', `LastName`)), 150);");

                migrationBuilder.DropColumn(
                    name: "FirstName",
                    table: table);

                migrationBuilder.DropColumn(
                    name: "LastName",
                    table: table);
            }
        }
    }
}
