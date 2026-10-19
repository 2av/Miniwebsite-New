using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniWebsite.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MoveCardPaymentToInvoice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE invoices i
                INNER JOIN (
                    SELECT picked.InvoiceId, c.PaymentStatus, c.PaymentDate, c.PaymentAmount, c.OrderId
                    FROM cards c
                    INNER JOIN (
                        SELECT c2.Id AS CardId,
                               (
                                   SELECT i2.Id
                                   FROM invoices i2
                                   WHERE i2.CardId = c2.Id
                                   ORDER BY CASE
                                       WHEN c2.OrderId IS NOT NULL AND c2.OrderId <> '' AND i2.RazorpayOrderId = c2.OrderId THEN 0
                                       ELSE 1
                                   END, i2.Id DESC
                                   LIMIT 1
                               ) AS InvoiceId
                        FROM cards c2
                        WHERE c2.PaymentStatus IS NOT NULL
                          AND c2.PaymentStatus <> ''
                          AND c2.PaymentStatus <> 'Created'
                    ) picked ON picked.CardId = c.Id
                    WHERE picked.InvoiceId IS NOT NULL
                ) src ON src.InvoiceId = i.Id
                SET
                    i.PaymentStatus = IF(
                        i.PaymentStatus IS NULL OR i.PaymentStatus = '' OR i.PaymentStatus IN ('Pending', 'PENDING', 'Created'),
                        src.PaymentStatus,
                        i.PaymentStatus),
                    i.PaymentDate = IF(i.PaymentDate IS NULL, src.PaymentDate, i.PaymentDate),
                    i.TotalAmount = IF(
                        i.TotalAmount = 0 AND src.PaymentAmount REGEXP '^[0-9]+(\\.[0-9]+)?$',
                        CAST(src.PaymentAmount AS DECIMAL(12, 2)),
                        i.TotalAmount),
                    i.RazorpayOrderId = IF(
                        i.RazorpayOrderId IS NULL OR i.RazorpayOrderId = '',
                        IFNULL(src.OrderId, ''),
                        i.RazorpayOrderId);

                INSERT INTO invoices (
                    InvoiceNumber, OrderCode, InvoiceDate, CardId, UserId,
                    UserEmail, UserName, UserContact,
                    BillingName, BillingEmail, BillingContact, BillingAddress, BillingState, BillingCity, BillingPincode,
                    OriginalAmount, DiscountAmount, FinalAmount, PromoDiscount,
                    RazorpayOrderId, RazorpayPaymentId, RazorpaySignature,
                    PaymentStatus, PaymentDate,
                    ServiceName, ServiceDescription, HsnSacCode, Quantity,
                    UnitPrice, TotalPrice, SubTotal, IgstPercentage, IgstAmount, CgstAmount, SgstAmount,
                    TotalAmount, PaymentType, ReferenceNumber, AdminComment, CreatedAt, UpdatedAt)
                SELECT
                    CONCAT('LEG', LPAD(c.Id, 8, '0')),
                    CONCAT('LGO', LPAD(c.Id, 8, '0')),
                    COALESCE(c.PaymentDate, c.CreatedAt, UTC_TIMESTAMP(6)),
                    c.Id,
                    c.UserId,
                    LEFT(IFNULL(c.UserEmail, ''), 255),
                    '',
                    IFNULL(c.Contact, ''),
                    '', '', '', '', '', '', '',
                    IF(c.PaymentAmount REGEXP '^[0-9]+(\\.[0-9]+)?$', CAST(c.PaymentAmount AS DECIMAL(12, 2)), 0),
                    0,
                    IF(c.PaymentAmount REGEXP '^[0-9]+(\\.[0-9]+)?$', CAST(c.PaymentAmount AS DECIMAL(12, 2)), 0),
                    0,
                    IFNULL(c.OrderId, ''),
                    '',
                    '',
                    c.PaymentStatus,
                    c.PaymentDate,
                    'Profile payment',
                    'Moved from the card',
                    '998313',
                    1,
                    IF(c.PaymentAmount REGEXP '^[0-9]+(\\.[0-9]+)?$', CAST(c.PaymentAmount AS DECIMAL(12, 2)), 0),
                    IF(c.PaymentAmount REGEXP '^[0-9]+(\\.[0-9]+)?$', CAST(c.PaymentAmount AS DECIMAL(12, 2)), 0),
                    IF(c.PaymentAmount REGEXP '^[0-9]+(\\.[0-9]+)?$', CAST(c.PaymentAmount AS DECIMAL(12, 2)), 0),
                    18,
                    0, 0, 0,
                    IF(c.PaymentAmount REGEXP '^[0-9]+(\\.[0-9]+)?$', CAST(c.PaymentAmount AS DECIMAL(12, 2)), 0),
                    'Regular',
                    IFNULL(c.OrderId, ''),
                    'Moved from the card',
                    COALESCE(c.PaymentDate, c.CreatedAt, UTC_TIMESTAMP(6)),
                    UTC_TIMESTAMP(6)
                FROM cards c
                WHERE c.PaymentStatus IS NOT NULL
                  AND c.PaymentStatus <> ''
                  AND c.PaymentStatus <> 'Created'
                  AND NOT EXISTS (SELECT 1 FROM invoices i WHERE i.CardId = c.Id);
                """);

            migrationBuilder.DropColumn(name: "OrderId", table: "cards");
            migrationBuilder.DropColumn(name: "PaymentAmount", table: "cards");
            migrationBuilder.DropColumn(name: "PaymentDate", table: "cards");
            migrationBuilder.DropColumn(name: "PaymentStatus", table: "cards");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(name: "OrderId", table: "cards", type: "longtext", nullable: true).Annotation("MySql:CharSet", "utf8mb4");
            migrationBuilder.AddColumn<string>(name: "PaymentAmount", table: "cards", type: "varchar(50)", maxLength: 50, nullable: true).Annotation("MySql:CharSet", "utf8mb4");
            migrationBuilder.AddColumn<DateTime>(name: "PaymentDate", table: "cards", type: "datetime(6)", nullable: true);
            migrationBuilder.AddColumn<string>(name: "PaymentStatus", table: "cards", type: "longtext", nullable: true).Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.Sql(
                """
                UPDATE cards c
                LEFT JOIN (
                    SELECT i.CardId, i.PaymentStatus, i.PaymentDate, i.TotalAmount, i.RazorpayOrderId
                    FROM invoices i
                    INNER JOIN (
                        SELECT CardId, MAX(Id) AS Id
                        FROM invoices
                        WHERE CardId IS NOT NULL
                        GROUP BY CardId
                    ) pick ON pick.Id = i.Id
                ) src ON src.CardId = c.Id
                SET
                    c.PaymentStatus = IFNULL(src.PaymentStatus, 'Created'),
                    c.PaymentDate = src.PaymentDate,
                    c.PaymentAmount = IF(src.TotalAmount IS NULL OR src.TotalAmount = 0, NULL, CAST(src.TotalAmount AS CHAR)),
                    c.OrderId = NULLIF(src.RazorpayOrderId, '');

                DELETE FROM invoices
                WHERE InvoiceNumber LIKE 'LEG%'
                  AND AdminComment = 'Moved from the card';
                """);
        }
    }
}
