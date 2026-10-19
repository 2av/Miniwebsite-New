using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniWebsite.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class KeepInvoicesAsTheSale : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE invoices i
                INNER JOIN franchise_payments p
                    ON p.RazorpayOrderId IS NOT NULL AND p.RazorpayOrderId <> '' AND i.RazorpayOrderId = p.RazorpayOrderId
                SET
                    i.PaymentStatus = IF(i.PaymentStatus IS NULL OR i.PaymentStatus IN ('', 'Pending', 'PENDING', 'Created'), p.PaymentStatus, i.PaymentStatus),
                    i.PaymentDate = IF(i.PaymentDate IS NULL, p.PaymentDate, i.PaymentDate),
                    i.TotalAmount = IF(i.TotalAmount = 0, p.Amount, i.TotalAmount),
                    i.RazorpayPaymentId = IF(i.RazorpayPaymentId IS NULL OR i.RazorpayPaymentId = '', IFNULL(p.RazorpayPaymentId, ''), i.RazorpayPaymentId),
                    i.RazorpaySignature = IF(i.RazorpaySignature IS NULL OR i.RazorpaySignature = '', IFNULL(p.RazorpaySignature, ''), i.RazorpaySignature);

                UPDATE invoices i
                INNER JOIN franchise_payments p
                    ON p.RazorpayPaymentId IS NOT NULL AND p.RazorpayPaymentId <> '' AND i.RazorpayPaymentId = p.RazorpayPaymentId
                SET
                    i.PaymentStatus = IF(i.PaymentStatus IS NULL OR i.PaymentStatus IN ('', 'Pending', 'PENDING', 'Created'), p.PaymentStatus, i.PaymentStatus),
                    i.PaymentDate = IF(i.PaymentDate IS NULL, p.PaymentDate, i.PaymentDate),
                    i.TotalAmount = IF(i.TotalAmount = 0, p.Amount, i.TotalAmount);

                UPDATE invoices i
                INNER JOIN franchise_payments p
                    ON p.ReferenceNumber <> '' AND i.ReferenceNumber = p.ReferenceNumber
                SET
                    i.PaymentStatus = IF(i.PaymentStatus IS NULL OR i.PaymentStatus IN ('', 'Pending', 'PENDING', 'Created'), p.PaymentStatus, i.PaymentStatus),
                    i.PaymentDate = IF(i.PaymentDate IS NULL, p.PaymentDate, i.PaymentDate),
                    i.TotalAmount = IF(i.TotalAmount = 0, p.Amount, i.TotalAmount);

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
                    CONCAT('FRP', LPAD(p.Id, 8, '0')),
                    CONCAT('FRO', LPAD(p.Id, 8, '0')),
                    p.PaymentDate,
                    NULL,
                    p.FranchiseUserId,
                    LEFT(IFNULL(p.FranchiseEmail, ''), 255),
                    '', '',
                    '', '', '', '', '', '', '',
                    p.Amount, 0, p.Amount, 0,
                    IFNULL(p.RazorpayOrderId, ''),
                    IFNULL(p.RazorpayPaymentId, ''),
                    IFNULL(p.RazorpaySignature, ''),
                    p.PaymentStatus,
                    p.PaymentDate,
                    'Franchisee Registration Fees',
                    'Moved from franchise payment',
                    '998313',
                    1,
                    p.Amount, p.Amount, p.Amount, 18, 0, 0, 0,
                    p.Amount,
                    'franchisee',
                    p.ReferenceNumber,
                    'Moved from franchise payment',
                    COALESCE(p.CreatedAt, p.PaymentDate),
                    UTC_TIMESTAMP(6)
                FROM franchise_payments p
                WHERE p.PaymentStatus IS NOT NULL
                  AND p.PaymentStatus <> ''
                  AND p.PaymentStatus NOT IN ('Pending', 'PENDING', 'Created')
                  AND NOT EXISTS (
                      SELECT 1 FROM invoices i
                      WHERE (p.RazorpayOrderId IS NOT NULL AND p.RazorpayOrderId <> '' AND i.RazorpayOrderId = p.RazorpayOrderId)
                         OR (p.RazorpayPaymentId IS NOT NULL AND p.RazorpayPaymentId <> '' AND i.RazorpayPaymentId = p.RazorpayPaymentId)
                         OR (p.ReferenceNumber <> '' AND i.ReferenceNumber = p.ReferenceNumber)
                  );

                UPDATE invoices i
                INNER JOIN user_joining_deals d ON d.InvoiceId = i.Id
                SET
                    i.PaymentStatus = IF(i.PaymentStatus IS NULL OR i.PaymentStatus IN ('', 'Pending', 'PENDING', 'Created'), d.PaymentStatus, i.PaymentStatus),
                    i.PaymentDate = IF(i.PaymentDate IS NULL, d.PaymentDate, i.PaymentDate),
                    i.TotalAmount = IF(i.TotalAmount = 0, d.AmountPaid, i.TotalAmount),
                    i.RazorpayPaymentId = IF(
                        (i.RazorpayPaymentId IS NULL OR i.RazorpayPaymentId = '') AND d.TransactionId IS NOT NULL AND d.TransactionId <> '',
                        d.TransactionId,
                        i.RazorpayPaymentId)
                WHERE d.PaymentStatus IS NOT NULL
                  AND d.PaymentStatus <> ''
                  AND d.PaymentStatus NOT IN ('Pending', 'PENDING');

                UPDATE user_joining_deals d
                INNER JOIN (
                    SELECT d2.Id AS DealRowId, MIN(i.Id) AS InvoiceId
                    FROM user_joining_deals d2
                    INNER JOIN invoices i
                        ON d2.TransactionId IS NOT NULL AND d2.TransactionId <> ''
                       AND (i.RazorpayPaymentId = d2.TransactionId OR i.RazorpayOrderId = d2.TransactionId OR i.ReferenceNumber = d2.TransactionId)
                    WHERE d2.InvoiceId IS NULL
                    GROUP BY d2.Id
                ) picked ON picked.DealRowId = d.Id
                SET d.InvoiceId = picked.InvoiceId;

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
                    CONCAT('UJD', LPAD(d.Id, 8, '0')),
                    CONCAT('UJO', LPAD(d.Id, 8, '0')),
                    COALESCE(d.PaymentDate, d.CreatedAt, UTC_TIMESTAMP(6)),
                    NULL,
                    d.UserId,
                    LEFT(IFNULL(u.Email, ''), 255),
                    '', '',
                    '', '', '', '', '', '', '',
                    d.AmountPaid, 0, d.AmountPaid, 0,
                    '',
                    IFNULL(d.TransactionId, ''),
                    '',
                    d.PaymentStatus,
                    d.PaymentDate,
                    'Joining deal',
                    'Moved from a joining deal',
                    '998313',
                    1,
                    d.AmountPaid, d.AmountPaid, d.AmountPaid, 18, 0, 0, 0,
                    d.AmountPaid,
                    'Regular',
                    IFNULL(d.TransactionId, ''),
                    'Moved from a joining deal',
                    COALESCE(d.CreatedAt, d.PaymentDate, UTC_TIMESTAMP(6)),
                    UTC_TIMESTAMP(6)
                FROM user_joining_deals d
                LEFT JOIN users u ON u.Id = d.UserId
                WHERE d.InvoiceId IS NULL
                  AND d.PaymentStatus IS NOT NULL
                  AND d.PaymentStatus <> ''
                  AND d.PaymentStatus NOT IN ('Pending', 'PENDING');

                UPDATE user_joining_deals d
                INNER JOIN invoices i ON i.InvoiceNumber = CONCAT('UJD', LPAD(d.Id, 8, '0'))
                SET d.InvoiceId = i.Id
                WHERE d.InvoiceId IS NULL;
                """);

            migrationBuilder.DropTable(name: "franchise_payments");

            migrationBuilder.DropColumn(name: "AmountPaid", table: "user_joining_deals");
            migrationBuilder.DropColumn(name: "PaymentDate", table: "user_joining_deals");
            migrationBuilder.DropColumn(name: "PaymentStatus", table: "user_joining_deals");
            migrationBuilder.DropColumn(name: "TransactionId", table: "user_joining_deals");

            migrationBuilder.AddColumn<int>(
                name: "InvoiceId",
                table: "orders",
                type: "int",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE orders o
                INNER JOIN (
                    SELECT o2.Id AS OrderRowId, MIN(i.Id) AS InvoiceId
                    FROM orders o2
                    INNER JOIN invoices i
                        ON o2.ExternalId <> ''
                       AND (i.RazorpayOrderId = o2.ExternalId OR i.OrderCode = o2.ExternalId OR i.ReferenceNumber = o2.ExternalId)
                    GROUP BY o2.Id
                ) picked ON picked.OrderRowId = o.Id
                SET o.InvoiceId = picked.InvoiceId
                WHERE o.InvoiceId IS NULL;

                UPDATE orders o
                INNER JOIN (
                    SELECT o2.Id AS OrderRowId, MIN(i.Id) AS InvoiceId
                    FROM orders o2
                    INNER JOIN invoices i ON i.CardId = o2.CardId
                    WHERE o2.InvoiceId IS NULL AND o2.CardId IS NOT NULL
                    GROUP BY o2.Id
                    HAVING COUNT(DISTINCT i.Id) = 1
                ) picked ON picked.OrderRowId = o.Id
                SET o.InvoiceId = picked.InvoiceId;

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
                    CONCAT('ORL', LPAD(o.Id, 8, '0')),
                    CONCAT('ORO', LPAD(o.Id, 8, '0')),
                    COALESCE(o.CreatedAt, UTC_TIMESTAMP(6)),
                    o.CardId,
                    o.UserId,
                    LEFT(IFNULL(o.UserEmail, ''), 255),
                    IFNULL(o.CustomerName, ''),
                    IFNULL(o.CustomerContact, ''),
                    IFNULL(o.CustomerName, ''),
                    IFNULL(o.CustomerEmail, ''),
                    IFNULL(o.CustomerContact, ''),
                    '', '', '', '',
                    o.PaymentAmount, 0, o.PaymentAmount, 0,
                    IFNULL(o.ExternalId, ''),
                    '',
                    '',
                    o.OrderStatus,
                    o.CreatedAt,
                    'Order',
                    'Moved from an order',
                    '998313',
                    1,
                    o.PaymentAmount, o.PaymentAmount, o.PaymentAmount, 18, 0, 0, 0,
                    o.PaymentAmount,
                    IFNULL(o.PaymentType, 'Regular'),
                    IFNULL(o.ExternalId, ''),
                    'Moved from an order',
                    COALESCE(o.CreatedAt, UTC_TIMESTAMP(6)),
                    UTC_TIMESTAMP(6)
                FROM orders o
                WHERE o.InvoiceId IS NULL
                  AND o.OrderStatus IS NOT NULL
                  AND o.OrderStatus <> ''
                  AND o.OrderStatus NOT IN ('Pending', 'PENDING', 'Created')
                  AND NOT EXISTS (
                      SELECT 1 FROM invoices i
                      WHERE (o.CardId IS NOT NULL AND i.CardId = o.CardId)
                         OR (o.UserId IS NOT NULL AND i.UserId = o.UserId)
                  );

                UPDATE orders o
                INNER JOIN invoices i ON i.InvoiceNumber = CONCAT('ORL', LPAD(o.Id, 8, '0'))
                SET o.InvoiceId = i.Id
                WHERE o.InvoiceId IS NULL;
                """);

            migrationBuilder.CreateIndex(name: "IX_orders_InvoiceId", table: "orders", column: "InvoiceId");
            migrationBuilder.AddForeignKey(
                name: "FK_orders_invoices_InvoiceId",
                table: "orders",
                column: "InvoiceId",
                principalTable: "invoices",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.DropColumn(name: "OrderStatus", table: "orders");
            migrationBuilder.DropColumn(name: "PaymentAmount", table: "orders");
            migrationBuilder.DropColumn(name: "PaymentType", table: "orders");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(name: "OrderStatus", table: "orders", type: "varchar(50)", maxLength: 50, nullable: false, defaultValue: "").Annotation("MySql:CharSet", "utf8mb4");
            migrationBuilder.AddColumn<decimal>(name: "PaymentAmount", table: "orders", type: "decimal(12,2)", precision: 12, scale: 2, nullable: false, defaultValue: 0m);
            migrationBuilder.AddColumn<string>(name: "PaymentType", table: "orders", type: "longtext", nullable: true).Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(name: "AmountPaid", table: "user_joining_deals", type: "decimal(12,2)", precision: 12, scale: 2, nullable: false, defaultValue: 0m);
            migrationBuilder.AddColumn<DateTime>(name: "PaymentDate", table: "user_joining_deals", type: "datetime(6)", nullable: true);
            migrationBuilder.AddColumn<string>(name: "PaymentStatus", table: "user_joining_deals", type: "varchar(20)", maxLength: 20, nullable: false, defaultValue: "PENDING").Annotation("MySql:CharSet", "utf8mb4");
            migrationBuilder.AddColumn<string>(name: "TransactionId", table: "user_joining_deals", type: "longtext", nullable: true).Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.Sql(
                """
                UPDATE orders o
                INNER JOIN invoices i ON i.Id = o.InvoiceId
                SET o.OrderStatus = LEFT(IFNULL(i.PaymentStatus, ''), 50),
                    o.PaymentAmount = i.TotalAmount,
                    o.PaymentType = NULLIF(i.PaymentType, '');

                UPDATE orders o
                INNER JOIN invoices i ON i.Id = o.InvoiceId
                    AND i.InvoiceNumber LIKE 'ORL%'
                    AND i.AdminComment = 'Moved from an order'
                SET o.InvoiceId = NULL;

                DELETE FROM invoices
                WHERE InvoiceNumber LIKE 'ORL%' AND AdminComment = 'Moved from an order';

                UPDATE user_joining_deals d
                INNER JOIN invoices i ON i.Id = d.InvoiceId
                SET d.PaymentStatus = LEFT(IFNULL(NULLIF(i.PaymentStatus, ''), 'PENDING'), 20),
                    d.PaymentDate = i.PaymentDate,
                    d.AmountPaid = i.TotalAmount,
                    d.TransactionId = NULLIF(i.RazorpayPaymentId, '');

                UPDATE user_joining_deals d
                INNER JOIN invoices i ON i.Id = d.InvoiceId
                    AND i.InvoiceNumber LIKE 'UJD%'
                    AND i.AdminComment = 'Moved from a joining deal'
                SET d.InvoiceId = NULL;

                DELETE FROM invoices
                WHERE InvoiceNumber LIKE 'UJD%' AND AdminComment = 'Moved from a joining deal';
                """);

            migrationBuilder.DropForeignKey(name: "FK_orders_invoices_InvoiceId", table: "orders");
            migrationBuilder.DropIndex(name: "IX_orders_InvoiceId", table: "orders");
            migrationBuilder.DropColumn(name: "InvoiceId", table: "orders");

            migrationBuilder.CreateTable(
                name: "franchise_payments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    AdminNotes = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Amount = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Currency = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FranchiseEmail = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FranchiseUserId = table.Column<int>(type: "int", nullable: true),
                    IpAddress = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PaymentDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    PaymentMethod = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PaymentStatus = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RazorpayOrderId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RazorpayPaymentId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RazorpaySignature = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReferenceNumber = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RefundAmount = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    RefundDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UserAgent = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_franchise_payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_franchise_payments_users_FranchiseUserId",
                        column: x => x.FranchiseUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(name: "IX_franchise_payments_FranchiseUserId", table: "franchise_payments", column: "FranchiseUserId");

            migrationBuilder.Sql(
                """
                INSERT INTO franchise_payments (
                    FranchiseUserId, FranchiseEmail, ReferenceNumber,
                    RazorpayOrderId, RazorpayPaymentId, RazorpaySignature,
                    Amount, Currency, PaymentStatus, PaymentMethod, PaymentDate,
                    AdminNotes, RefundAmount, CreatedAt, UpdatedAt)
                SELECT
                    i.UserId,
                    LEFT(IFNULL(i.UserEmail, ''), 255),
                    LEFT(IFNULL(i.ReferenceNumber, ''), 100),
                    NULLIF(i.RazorpayOrderId, ''),
                    NULLIF(i.RazorpayPaymentId, ''),
                    NULLIF(i.RazorpaySignature, ''),
                    i.TotalAmount,
                    'INR',
                    LEFT(IFNULL(i.PaymentStatus, 'Pending'), 20),
                    'Razorpay',
                    COALESCE(i.PaymentDate, i.InvoiceDate),
                    'Moved from franchise payment',
                    0,
                    i.CreatedAt,
                    i.UpdatedAt
                FROM invoices i
                WHERE i.InvoiceNumber LIKE 'FRP%' AND i.AdminComment = 'Moved from franchise payment';

                DELETE FROM invoices
                WHERE InvoiceNumber LIKE 'FRP%' AND AdminComment = 'Moved from franchise payment';
                """);
        }
    }
}
