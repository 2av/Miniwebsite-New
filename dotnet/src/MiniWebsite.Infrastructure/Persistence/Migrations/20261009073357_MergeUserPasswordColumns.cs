using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniWebsite.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Keeps a single users.Password column holding the hash. Rows whose Password is not already a
    /// hash take PasswordHash; any plain text left over is hashed by DataSeeder on startup.
    /// </summary>
    public partial class MergeUserPasswordColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
UPDATE `users`
SET `Password` = `PasswordHash`
WHERE `PasswordHash` IS NOT NULL AND TRIM(`PasswordHash`) <> ''
  AND NOT (`Password` LIKE '$2a$%' OR `Password` LIKE '$2b$%' OR `Password` LIKE '$2y$%' OR `Password` LIKE 'AQAAAA%');");

            migrationBuilder.DropColumn(
                name: "PasswordHash",
                table: "users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "users",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.Sql("UPDATE `users` SET `PasswordHash` = `Password`;");
        }
    }
}
