using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniWebsite.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Adds invoices.OrderCode (ORD + 8 random uppercase letters/digits, backfilled for existing rows)
    /// and invoices.AdminComment. New invoices get their code from ApplicationDbContext.SaveChangesAsync.
    /// </summary>
    public partial class AddInvoiceOrderCodeAndComment : Migration
    {
        private const string RandomChar =
            "SUBSTRING('ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789', FLOOR(1 + RAND() * 36), 1)";

        /// <summary>Passes that regenerate codes that are missing, not a letter/digit mix, or duplicated.</summary>
        private const int BackfillPasses = 10;

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdminComment",
                table: "invoices",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "OrderCode",
                table: "invoices",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            var randomSuffix = string.Join(", ", Enumerable.Repeat(RandomChar, 8));
            for (var pass = 0; pass < BackfillPasses; pass++)
            {
                migrationBuilder.Sql($@"
UPDATE `invoices`
SET `OrderCode` = CONCAT('ORD', {randomSuffix})
WHERE `OrderCode` = ''
   OR RIGHT(`OrderCode`, 8) REGEXP '^[A-Z]+$'
   OR RIGHT(`OrderCode`, 8) REGEXP '^[0-9]+$'
   OR `OrderCode` IN (
       SELECT d.`OrderCode` FROM (
           SELECT `OrderCode` FROM `invoices` GROUP BY `OrderCode` HAVING COUNT(*) > 1
       ) d);");
            }

            migrationBuilder.CreateIndex(
                name: "IX_invoices_OrderCode",
                table: "invoices",
                column: "OrderCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_invoices_OrderCode",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "AdminComment",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "OrderCode",
                table: "invoices");
        }
    }
}
