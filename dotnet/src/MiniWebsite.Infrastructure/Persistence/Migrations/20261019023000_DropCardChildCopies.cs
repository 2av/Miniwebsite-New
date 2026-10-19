using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniWebsite.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DropCardChildCopies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE card_products p
                LEFT JOIN cards existing ON existing.Id = p.CardId
                INNER JOIN cards c ON c.Slug = p.CardSlug AND c.Slug <> ''
                SET p.CardId = c.Id
                WHERE existing.Id IS NULL;

                UPDATE card_services p
                LEFT JOIN cards existing ON existing.Id = p.CardId
                INNER JOIN cards c ON c.Slug = p.CardSlug AND c.Slug <> ''
                SET p.CardId = c.Id
                WHERE existing.Id IS NULL;

                UPDATE card_offers p
                LEFT JOIN cards existing ON existing.Id = p.CardId
                INNER JOIN cards c ON c.Slug = p.CardSlug AND c.Slug <> ''
                SET p.CardId = c.Id
                WHERE existing.Id IS NULL;

                UPDATE card_gallery_images p
                LEFT JOIN cards existing ON existing.Id = p.CardId
                INNER JOIN cards c ON c.Slug = p.CardSlug AND c.Slug <> ''
                SET p.CardId = c.Id
                WHERE existing.Id IS NULL;

                UPDATE card_views p
                LEFT JOIN cards existing ON existing.Id = p.CardId
                INNER JOIN cards c ON c.Slug = p.CardSlug AND c.Slug <> ''
                SET p.CardId = c.Id
                WHERE existing.Id IS NULL;
                """);

            foreach (var table in new[] { "card_products", "card_services", "card_offers", "card_gallery_images" })
            {
                migrationBuilder.DropColumn(name: "CardSlug", table: table);
                migrationBuilder.DropColumn(name: "UserId", table: table);
            }

            migrationBuilder.DropColumn(name: "CardSlug", table: "card_views");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var table in new[] { "card_products", "card_services", "card_offers", "card_gallery_images" })
            {
                migrationBuilder.AddColumn<string>(name: "CardSlug", table: table, type: "longtext", nullable: false, defaultValue: "")
                    .Annotation("MySql:CharSet", "utf8mb4");
                migrationBuilder.AddColumn<int>(name: "UserId", table: table, type: "int", nullable: false, defaultValue: 0);
            }

            migrationBuilder.AddColumn<string>(name: "CardSlug", table: "card_views", type: "varchar(200)", maxLength: 200, nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.Sql(
                """
                UPDATE card_products p INNER JOIN cards c ON c.Id = p.CardId
                SET p.CardSlug = IFNULL(c.Slug, ''), p.UserId = IFNULL(c.UserId, 0);
                UPDATE card_services p INNER JOIN cards c ON c.Id = p.CardId
                SET p.CardSlug = IFNULL(c.Slug, ''), p.UserId = IFNULL(c.UserId, 0);
                UPDATE card_offers p INNER JOIN cards c ON c.Id = p.CardId
                SET p.CardSlug = IFNULL(c.Slug, ''), p.UserId = IFNULL(c.UserId, 0);
                UPDATE card_gallery_images p INNER JOIN cards c ON c.Id = p.CardId
                SET p.CardSlug = IFNULL(c.Slug, ''), p.UserId = IFNULL(c.UserId, 0);
                UPDATE card_views p INNER JOIN cards c ON c.Id = p.CardId
                SET p.CardSlug = LEFT(IFNULL(c.Slug, ''), 200);
                """);
        }
    }
}
