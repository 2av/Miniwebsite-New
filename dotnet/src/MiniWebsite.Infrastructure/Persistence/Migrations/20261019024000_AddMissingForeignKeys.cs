using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniWebsite.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE card_products p
                LEFT JOIN product_categories c ON c.Id = p.ProductCategory
                SET p.ProductCategory = NULL
                WHERE p.ProductCategory IS NOT NULL AND c.Id IS NULL;

                UPDATE card_services p
                LEFT JOIN product_categories c ON c.Id = p.ProductCategory
                SET p.ProductCategory = NULL
                WHERE p.ProductCategory IS NOT NULL AND c.Id IS NULL;

                UPDATE customer_followups f
                INNER JOIN customers c ON c.Id = f.CustomerId
                LEFT JOIN users u ON u.Id = f.OwnerUserId
                SET f.OwnerUserId = c.OwnerUserId
                WHERE u.Id IS NULL;

                DELETE f FROM customer_followups f
                LEFT JOIN users u ON u.Id = f.OwnerUserId
                WHERE u.Id IS NULL;

                UPDATE user_referral_settings r
                LEFT JOIN users u ON u.Id = r.SenderUserId
                SET r.SenderUserId = NULL
                WHERE r.SenderUserId IS NOT NULL AND u.Id IS NULL;

                UPDATE joining_deals d
                LEFT JOIN joining_deals other ON other.Id = d.MwDealId
                SET d.MwDealId = NULL
                WHERE d.MwDealId IS NOT NULL AND other.Id IS NULL;

                UPDATE joining_deals d
                LEFT JOIN joining_deals other ON other.Id = d.FranchiseDealId
                SET d.FranchiseDealId = NULL
                WHERE d.FranchiseDealId IS NOT NULL AND other.Id IS NULL;
                """);

            migrationBuilder.DropColumn(name: "RelatedId", table: "notifications");

            migrationBuilder.CreateIndex(name: "IX_card_products_ProductCategory", table: "card_products", column: "ProductCategory");
            migrationBuilder.CreateIndex(name: "IX_card_services_ProductCategory", table: "card_services", column: "ProductCategory");
            migrationBuilder.CreateIndex(name: "IX_customer_followups_OwnerUserId", table: "customer_followups", column: "OwnerUserId");
            migrationBuilder.CreateIndex(name: "IX_joining_deals_FranchiseDealId", table: "joining_deals", column: "FranchiseDealId");
            migrationBuilder.CreateIndex(name: "IX_joining_deals_MwDealId", table: "joining_deals", column: "MwDealId");
            migrationBuilder.CreateIndex(name: "IX_user_referral_settings_SenderUserId", table: "user_referral_settings", column: "SenderUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_card_products_product_categories_ProductCategory",
                table: "card_products",
                column: "ProductCategory",
                principalTable: "product_categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
            migrationBuilder.AddForeignKey(
                name: "FK_card_services_product_categories_ProductCategory",
                table: "card_services",
                column: "ProductCategory",
                principalTable: "product_categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
            migrationBuilder.AddForeignKey(
                name: "FK_customer_followups_users_OwnerUserId",
                table: "customer_followups",
                column: "OwnerUserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey(
                name: "FK_joining_deals_joining_deals_FranchiseDealId",
                table: "joining_deals",
                column: "FranchiseDealId",
                principalTable: "joining_deals",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
            migrationBuilder.AddForeignKey(
                name: "FK_joining_deals_joining_deals_MwDealId",
                table: "joining_deals",
                column: "MwDealId",
                principalTable: "joining_deals",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
            migrationBuilder.AddForeignKey(
                name: "FK_user_referral_settings_users_SenderUserId",
                table: "user_referral_settings",
                column: "SenderUserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "FK_card_products_product_categories_ProductCategory", table: "card_products");
            migrationBuilder.DropForeignKey(name: "FK_card_services_product_categories_ProductCategory", table: "card_services");
            migrationBuilder.DropForeignKey(name: "FK_customer_followups_users_OwnerUserId", table: "customer_followups");
            migrationBuilder.DropForeignKey(name: "FK_joining_deals_joining_deals_FranchiseDealId", table: "joining_deals");
            migrationBuilder.DropForeignKey(name: "FK_joining_deals_joining_deals_MwDealId", table: "joining_deals");
            migrationBuilder.DropForeignKey(name: "FK_user_referral_settings_users_SenderUserId", table: "user_referral_settings");

            migrationBuilder.DropIndex(name: "IX_card_products_ProductCategory", table: "card_products");
            migrationBuilder.DropIndex(name: "IX_card_services_ProductCategory", table: "card_services");
            migrationBuilder.DropIndex(name: "IX_customer_followups_OwnerUserId", table: "customer_followups");
            migrationBuilder.DropIndex(name: "IX_joining_deals_FranchiseDealId", table: "joining_deals");
            migrationBuilder.DropIndex(name: "IX_joining_deals_MwDealId", table: "joining_deals");
            migrationBuilder.DropIndex(name: "IX_user_referral_settings_SenderUserId", table: "user_referral_settings");

            migrationBuilder.AddColumn<int>(name: "RelatedId", table: "notifications", type: "int", nullable: true);
        }
    }
}
