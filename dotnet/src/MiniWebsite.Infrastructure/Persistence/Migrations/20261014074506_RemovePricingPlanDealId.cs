using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniWebsite.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemovePricingPlanDealId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_pricing_plans_coupons_DealId",
                table: "pricing_plans");

            migrationBuilder.DropIndex(
                name: "IX_pricing_plans_DealId",
                table: "pricing_plans");

            migrationBuilder.DropColumn(
                name: "DealId",
                table: "pricing_plans");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DealId",
                table: "pricing_plans",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_pricing_plans_DealId",
                table: "pricing_plans",
                column: "DealId");

            migrationBuilder.AddForeignKey(
                name: "FK_pricing_plans_coupons_DealId",
                table: "pricing_plans",
                column: "DealId",
                principalTable: "coupons",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
