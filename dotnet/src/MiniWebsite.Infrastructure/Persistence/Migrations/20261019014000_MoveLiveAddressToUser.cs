using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniWebsite.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MoveLiveAddressToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Address2",
                table: "user_addresses",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "user_addresses",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.Sql(
                """
                UPDATE user_addresses ua
                INNER JOIN (
                    SELECT c.UserId, c.Address, c.Address2, c.City, c.State, c.Pincode, c.Country
                    FROM cards c
                    INNER JOIN (
                        SELECT UserId, MAX(Id) AS Id
                        FROM cards
                        WHERE UserId IS NOT NULL
                        GROUP BY UserId
                    ) pick ON pick.Id = c.Id
                ) src ON src.UserId = ua.UserId
                SET
                    ua.AddressLine = IF(ua.AddressLine IS NULL OR ua.AddressLine = '', LEFT(src.Address, 500), ua.AddressLine),
                    ua.Address2 = IF(ua.Address2 IS NULL OR ua.Address2 = '', LEFT(src.Address2, 500), ua.Address2),
                    ua.City = IF(ua.City IS NULL OR ua.City = '', LEFT(src.City, 100), ua.City),
                    ua.State = IF(ua.State IS NULL OR ua.State = '', LEFT(src.State, 100), ua.State),
                    ua.Pincode = IF(ua.Pincode IS NULL OR ua.Pincode = '', LEFT(src.Pincode, 20), ua.Pincode),
                    ua.Country = IF(ua.Country IS NULL OR ua.Country = '', LEFT(src.Country, 100), ua.Country);
                """);

            migrationBuilder.Sql(
                """
                UPDATE user_addresses ua
                INNER JOIN (
                    SELECT o.UserId, o.Address, o.City, o.State, o.Pincode
                    FROM orders o
                    INNER JOIN (
                        SELECT UserId, MAX(Id) AS Id
                        FROM orders
                        WHERE UserId IS NOT NULL
                        GROUP BY UserId
                    ) pick ON pick.Id = o.Id
                ) src ON src.UserId = ua.UserId
                SET
                    ua.AddressLine = IF(ua.AddressLine IS NULL OR ua.AddressLine = '', LEFT(src.Address, 500), ua.AddressLine),
                    ua.City = IF(ua.City IS NULL OR ua.City = '', LEFT(src.City, 100), ua.City),
                    ua.State = IF(ua.State IS NULL OR ua.State = '', LEFT(src.State, 100), ua.State),
                    ua.Pincode = IF(ua.Pincode IS NULL OR ua.Pincode = '', LEFT(src.Pincode, 20), ua.Pincode);
                """);

            migrationBuilder.DropColumn(name: "Address", table: "cards");
            migrationBuilder.DropColumn(name: "Address2", table: "cards");
            migrationBuilder.DropColumn(name: "City", table: "cards");
            migrationBuilder.DropColumn(name: "Country", table: "cards");
            migrationBuilder.DropColumn(name: "Pincode", table: "cards");
            migrationBuilder.DropColumn(name: "State", table: "cards");

            migrationBuilder.DropColumn(name: "Address", table: "orders");
            migrationBuilder.DropColumn(name: "City", table: "orders");
            migrationBuilder.DropColumn(name: "Pincode", table: "orders");
            migrationBuilder.DropColumn(name: "State", table: "orders");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "cards",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Address2",
                table: "cards",
                type: "longtext",
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "cards",
                type: "longtext",
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "cards",
                type: "longtext",
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Pincode",
                table: "cards",
                type: "longtext",
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "State",
                table: "cards",
                type: "longtext",
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "orders",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "orders",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Pincode",
                table: "orders",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "State",
                table: "orders",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.Sql(
                """
                UPDATE cards c
                INNER JOIN user_addresses ua ON ua.UserId = c.UserId
                SET
                    c.Address = ua.AddressLine,
                    c.Address2 = IFNULL(ua.Address2, ''),
                    c.City = IFNULL(ua.City, ''),
                    c.State = IFNULL(ua.State, ''),
                    c.Pincode = IFNULL(ua.Pincode, ''),
                    c.Country = IFNULL(ua.Country, '');
                """);

            migrationBuilder.Sql(
                """
                UPDATE orders o
                INNER JOIN user_addresses ua ON ua.UserId = o.UserId
                SET
                    o.Address = ua.AddressLine,
                    o.City = ua.City,
                    o.State = ua.State,
                    o.Pincode = ua.Pincode;
                """);

            migrationBuilder.DropColumn(name: "Address2", table: "user_addresses");
            migrationBuilder.DropColumn(name: "Country", table: "user_addresses");
        }
    }
}
