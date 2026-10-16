using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniWebsite.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PlanCommissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_pricing_plans_roles_RoleId",
                table: "pricing_plans");

            migrationBuilder.DropIndex(
                name: "IX_pricing_plans_RoleId_PlanName",
                table: "pricing_plans");

            migrationBuilder.DropColumn(
                name: "BonusAmount",
                table: "coupons");

            migrationBuilder.DropColumn(
                name: "RenewalBonusAmount",
                table: "coupons");

            migrationBuilder.DropColumn(
                name: "RoleId",
                table: "pricing_plans");

            migrationBuilder.AddColumn<int>(
                name: "ValidityMonths",
                table: "pricing_plans",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PlanCategory",
                table: "pricing_plans",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "DB Profile")
                .Annotation("MySql:CharSet", "utf8mb4");

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

            migrationBuilder.Sql(
                """
                UPDATE pricing_plans SET
                    ValidityMonths = CASE
                        WHEN PlanName LIKE '%Lifetime%' THEN NULL
                        WHEN PlanName LIKE '%1 Year%' THEN 12
                        WHEN PlanName LIKE '%2 Year%' THEN 24
                        WHEN PlanName LIKE '%3 Year%' THEN 36
                        WHEN PlanName LIKE '%Annual%' THEN 12
                        ELSE NULL END,
                    PlanCategory = CASE
                        WHEN PlanName LIKE '%Franchise%' THEN 'Franchise'
                        WHEN PlanName LIKE '%Membership%' THEN 'Membership'
                        ELSE 'DB Profile' END;
                """);

            migrationBuilder.CreateTable(
                name: "plan_commissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    PricingPlanId = table.Column<int>(type: "int", nullable: false),
                    DiscountType = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DiscountAmount = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    EarningType = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EarningAmount = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    RenewalEarningPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    DeductFromWallet = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plan_commissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_plan_commissions_pricing_plans_PricingPlanId",
                        column: x => x.PricingPlanId,
                        principalTable: "pricing_plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_plan_commissions_roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "roles",
                        principalColumn: "RoleId",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_pricing_plans_PlanName",
                table: "pricing_plans",
                column: "PlanName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_plan_commissions_PricingPlanId",
                table: "plan_commissions",
                column: "PricingPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_plan_commissions_RoleId_PricingPlanId",
                table: "plan_commissions",
                columns: new[] { "RoleId", "PricingPlanId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "plan_commissions");

            migrationBuilder.DropIndex(
                name: "IX_pricing_plans_PlanName",
                table: "pricing_plans");

            migrationBuilder.DropColumn(
                name: "PlanCategory",
                table: "pricing_plans");

            migrationBuilder.DropColumn(
                name: "RenewalPrice",
                table: "pricing_plans");

            migrationBuilder.DropColumn(
                name: "RenewalPriceGst",
                table: "pricing_plans");

            migrationBuilder.DropColumn(
                name: "ValidityMonths",
                table: "pricing_plans");

            migrationBuilder.AddColumn<int>(
                name: "RoleId",
                table: "pricing_plans",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BonusAmount",
                table: "coupons",
                type: "decimal(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "RenewalBonusAmount",
                table: "coupons",
                type: "decimal(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_pricing_plans_RoleId_PlanName",
                table: "pricing_plans",
                columns: new[] { "RoleId", "PlanName" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_pricing_plans_roles_RoleId",
                table: "pricing_plans",
                column: "RoleId",
                principalTable: "roles",
                principalColumn: "RoleId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
