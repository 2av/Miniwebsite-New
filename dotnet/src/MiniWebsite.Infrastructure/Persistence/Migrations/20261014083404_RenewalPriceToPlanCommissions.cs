using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniWebsite.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenewalPriceToPlanCommissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "RenewalPrice",
                table: "plan_commissions",
                type: "decimal(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RenewalPriceGst",
                table: "plan_commissions",
                type: "decimal(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE plan_commissions c
                JOIN pricing_plans p ON p.Id = c.PricingPlanId
                SET c.RenewalPrice = p.RenewalPrice, c.RenewalPriceGst = p.RenewalPriceGst
                WHERE p.RenewalPrice IS NOT NULL;
                """);

            migrationBuilder.DropColumn(
                name: "RenewalPrice",
                table: "pricing_plans");

            migrationBuilder.DropColumn(
                name: "RenewalPriceGst",
                table: "pricing_plans");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RenewalPrice",
                table: "plan_commissions");

            migrationBuilder.DropColumn(
                name: "RenewalPriceGst",
                table: "plan_commissions");

            migrationBuilder.AddColumn<decimal>(
                name: "RenewalPrice",
                table: "pricing_plans",
                type: "decimal(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RenewalPriceGst",
                table: "pricing_plans",
                type: "decimal(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);
        }
    }
}
