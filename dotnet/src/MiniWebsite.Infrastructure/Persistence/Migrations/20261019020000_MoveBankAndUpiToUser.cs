using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniWebsite.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MoveBankAndUpiToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccountType",
                table: "user_bank_details",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "PhonePay",
                table: "user_payment_details",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.Sql(
                """
                INSERT INTO user_bank_details (UserId, BankName, AccountHolderName, AccountNumber, IfscCode, AccountType, CreatedAt, UpdatedAt)
                SELECT c.UserId, NULLIF(c.BankName, ''), NULLIF(c.AccountName, ''), NULLIF(c.AccountNo, ''), NULLIF(c.Ifsc, ''), NULLIF(c.AccountType, ''), UTC_TIMESTAMP(6), UTC_TIMESTAMP(6)
                FROM cards c
                INNER JOIN (
                    SELECT UserId, MAX(Id) AS Id
                    FROM cards
                    WHERE UserId IS NOT NULL
                    GROUP BY UserId
                ) pick ON pick.Id = c.Id
                LEFT JOIN user_bank_details b ON b.UserId = c.UserId
                WHERE b.UserId IS NULL
                  AND (
                    COALESCE(c.BankName, '') <> ''
                    OR COALESCE(c.AccountName, '') <> ''
                    OR COALESCE(c.AccountNo, '') <> ''
                    OR COALESCE(c.Ifsc, '') <> ''
                    OR COALESCE(c.AccountType, '') <> ''
                  );
                """);

            migrationBuilder.Sql(
                """
                UPDATE user_bank_details b
                INNER JOIN (
                    SELECT c.UserId, c.BankName, c.AccountName, c.AccountNo, c.Ifsc, c.AccountType
                    FROM cards c
                    INNER JOIN (
                        SELECT UserId, MAX(Id) AS Id
                        FROM cards
                        WHERE UserId IS NOT NULL
                        GROUP BY UserId
                    ) pick ON pick.Id = c.Id
                ) src ON src.UserId = b.UserId
                SET
                    b.BankName = IF(b.BankName IS NULL OR b.BankName = '', src.BankName, b.BankName),
                    b.AccountHolderName = IF(b.AccountHolderName IS NULL OR b.AccountHolderName = '', src.AccountName, b.AccountHolderName),
                    b.AccountNumber = IF(b.AccountNumber IS NULL OR b.AccountNumber = '', src.AccountNo, b.AccountNumber),
                    b.IfscCode = IF(b.IfscCode IS NULL OR b.IfscCode = '', src.Ifsc, b.IfscCode),
                    b.AccountType = IF(b.AccountType IS NULL OR b.AccountType = '', src.AccountType, b.AccountType);
                """);

            migrationBuilder.Sql(
                """
                UPDATE user_payment_details p
                INNER JOIN (
                    SELECT c.UserId, c.GooglePay, c.Paytm, c.PhonePay
                    FROM cards c
                    INNER JOIN (
                        SELECT UserId, MAX(Id) AS Id
                        FROM cards
                        WHERE UserId IS NOT NULL
                        GROUP BY UserId
                    ) pick ON pick.Id = c.Id
                ) src ON src.UserId = p.UserId
                SET
                    p.GooglePay = IF(p.GooglePay IS NULL OR p.GooglePay = '', LEFT(src.GooglePay, 200), p.GooglePay),
                    p.Paytm = IF(p.Paytm IS NULL OR p.Paytm = '', LEFT(src.Paytm, 200), p.Paytm),
                    p.PhonePay = IF(p.PhonePay IS NULL OR p.PhonePay = '', LEFT(src.PhonePay, 200), p.PhonePay);
                """);

            migrationBuilder.DropColumn(name: "AccountName", table: "cards");
            migrationBuilder.DropColumn(name: "AccountNo", table: "cards");
            migrationBuilder.DropColumn(name: "AccountType", table: "cards");
            migrationBuilder.DropColumn(name: "BankName", table: "cards");
            migrationBuilder.DropColumn(name: "GooglePay", table: "cards");
            migrationBuilder.DropColumn(name: "Ifsc", table: "cards");
            migrationBuilder.DropColumn(name: "Paytm", table: "cards");
            migrationBuilder.DropColumn(name: "PhonePay", table: "cards");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(name: "AccountName", table: "cards", type: "longtext", nullable: true).Annotation("MySql:CharSet", "utf8mb4");
            migrationBuilder.AddColumn<string>(name: "AccountNo", table: "cards", type: "longtext", nullable: true).Annotation("MySql:CharSet", "utf8mb4");
            migrationBuilder.AddColumn<string>(name: "AccountType", table: "cards", type: "longtext", nullable: true).Annotation("MySql:CharSet", "utf8mb4");
            migrationBuilder.AddColumn<string>(name: "BankName", table: "cards", type: "longtext", nullable: true).Annotation("MySql:CharSet", "utf8mb4");
            migrationBuilder.AddColumn<string>(name: "GooglePay", table: "cards", type: "longtext", nullable: true).Annotation("MySql:CharSet", "utf8mb4");
            migrationBuilder.AddColumn<string>(name: "Ifsc", table: "cards", type: "longtext", nullable: true).Annotation("MySql:CharSet", "utf8mb4");
            migrationBuilder.AddColumn<string>(name: "Paytm", table: "cards", type: "longtext", nullable: true).Annotation("MySql:CharSet", "utf8mb4");
            migrationBuilder.AddColumn<string>(name: "PhonePay", table: "cards", type: "longtext", nullable: true).Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.Sql(
                """
                UPDATE cards c
                INNER JOIN user_bank_details b ON b.UserId = c.UserId
                SET
                    c.BankName = b.BankName,
                    c.AccountName = b.AccountHolderName,
                    c.AccountNo = b.AccountNumber,
                    c.Ifsc = b.IfscCode,
                    c.AccountType = b.AccountType;
                """);

            migrationBuilder.Sql(
                """
                UPDATE cards c
                INNER JOIN user_payment_details p ON p.UserId = c.UserId
                SET
                    c.GooglePay = p.GooglePay,
                    c.Paytm = p.Paytm,
                    c.PhonePay = p.PhonePay;
                """);

            migrationBuilder.DropColumn(name: "AccountType", table: "user_bank_details");
            migrationBuilder.DropColumn(name: "PhonePay", table: "user_payment_details");
        }
    }
}
