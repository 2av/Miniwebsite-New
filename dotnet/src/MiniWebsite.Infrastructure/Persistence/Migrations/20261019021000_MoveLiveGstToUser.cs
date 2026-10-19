using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniWebsite.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MoveLiveGstToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE user_addresses ua
                INNER JOIN (
                    SELECT c.UserId, c.GstNumber, c.Gst, c.GstAddress, c.GstCity, c.GstState, c.GstPincode
                    FROM cards c
                    INNER JOIN (
                        SELECT UserId, MAX(Id) AS Id
                        FROM cards
                        WHERE UserId IS NOT NULL
                        GROUP BY UserId
                    ) pick ON pick.Id = c.Id
                ) src ON src.UserId = ua.UserId
                SET
                    ua.Gst = IF(ua.Gst IS NULL OR ua.Gst = '', LEFT(COALESCE(NULLIF(src.GstNumber, ''), NULLIF(src.Gst, '')), 50), ua.Gst),
                    ua.AddressLine = IF(ua.AddressLine IS NULL OR ua.AddressLine = '', LEFT(src.GstAddress, 500), ua.AddressLine),
                    ua.City = IF(ua.City IS NULL OR ua.City = '', LEFT(src.GstCity, 100), ua.City),
                    ua.State = IF(ua.State IS NULL OR ua.State = '', LEFT(src.GstState, 100), ua.State),
                    ua.Pincode = IF(ua.Pincode IS NULL OR ua.Pincode = '', LEFT(src.GstPincode, 20), ua.Pincode);
                """);

            migrationBuilder.DropColumn(name: "Gst", table: "cards");
            migrationBuilder.DropColumn(name: "GstAddress", table: "cards");
            migrationBuilder.DropColumn(name: "GstCity", table: "cards");
            migrationBuilder.DropColumn(name: "GstContact", table: "cards");
            migrationBuilder.DropColumn(name: "GstEmail", table: "cards");
            migrationBuilder.DropColumn(name: "GstName", table: "cards");
            migrationBuilder.DropColumn(name: "GstNumber", table: "cards");
            migrationBuilder.DropColumn(name: "GstPincode", table: "cards");
            migrationBuilder.DropColumn(name: "GstState", table: "cards");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(name: "Gst", table: "cards", type: "longtext", nullable: true).Annotation("MySql:CharSet", "utf8mb4");
            migrationBuilder.AddColumn<string>(name: "GstAddress", table: "cards", type: "longtext", nullable: true).Annotation("MySql:CharSet", "utf8mb4");
            migrationBuilder.AddColumn<string>(name: "GstCity", table: "cards", type: "longtext", nullable: true).Annotation("MySql:CharSet", "utf8mb4");
            migrationBuilder.AddColumn<string>(name: "GstContact", table: "cards", type: "longtext", nullable: true).Annotation("MySql:CharSet", "utf8mb4");
            migrationBuilder.AddColumn<string>(name: "GstEmail", table: "cards", type: "longtext", nullable: true).Annotation("MySql:CharSet", "utf8mb4");
            migrationBuilder.AddColumn<string>(name: "GstName", table: "cards", type: "longtext", nullable: true).Annotation("MySql:CharSet", "utf8mb4");
            migrationBuilder.AddColumn<string>(name: "GstNumber", table: "cards", type: "longtext", nullable: false, defaultValue: "").Annotation("MySql:CharSet", "utf8mb4");
            migrationBuilder.AddColumn<string>(name: "GstPincode", table: "cards", type: "longtext", nullable: true).Annotation("MySql:CharSet", "utf8mb4");
            migrationBuilder.AddColumn<string>(name: "GstState", table: "cards", type: "longtext", nullable: true).Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.Sql(
                """
                UPDATE cards c
                INNER JOIN user_addresses ua ON ua.UserId = c.UserId
                SET
                    c.GstNumber = IFNULL(ua.Gst, ''),
                    c.Gst = ua.Gst,
                    c.GstAddress = ua.AddressLine,
                    c.GstCity = ua.City,
                    c.GstState = ua.State,
                    c.GstPincode = ua.Pincode;
                """);
        }
    }
}
