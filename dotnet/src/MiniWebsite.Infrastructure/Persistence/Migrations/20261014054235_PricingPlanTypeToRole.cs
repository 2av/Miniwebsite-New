using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniWebsite.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PricingPlanTypeToRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RoleId",
                table: "pricing_plans",
                type: "int",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE pricing_plans p
                SET p.RoleId = (
                    SELECT r.RoleId FROM roles r
                    WHERE r.Prefix = CASE
                        WHEN p.PlanType = 'MW Plan (For MW Users)' THEN 'SBU'
                        WHEN p.PlanType IN ('MW Plan (For Franchise Partner)', 'Franchise Fees') THEN 'FRP'
                        WHEN p.PlanType = 'Digital Marketer Partnership Fees' THEN 'DMP'
                    END
                    ORDER BY r.RoleId LIMIT 1);
                """);

            migrationBuilder.Sql("""
                UPDATE pricing_plans p
                SET p.RoleId = (SELECT r.RoleId FROM roles r WHERE r.RoleName = p.PlanType ORDER BY r.RoleId LIMIT 1)
                WHERE p.RoleId IS NULL;
                """);

            migrationBuilder.Sql("""
                UPDATE pricing_plans p
                SET p.RoleId = (SELECT r.RoleId FROM roles r WHERE r.Prefix = 'SBU' ORDER BY r.RoleId LIMIT 1)
                WHERE p.RoleId IS NULL;
                """);

            migrationBuilder.DropIndex(
                name: "IX_pricing_plans_PlanType_PlanName",
                table: "pricing_plans");

            migrationBuilder.DropColumn(
                name: "PlanType",
                table: "pricing_plans");

            migrationBuilder.AlterColumn<int>(
                name: "RoleId",
                table: "pricing_plans",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_pricing_plans_roles_RoleId",
                table: "pricing_plans");

            migrationBuilder.DropIndex(
                name: "IX_pricing_plans_RoleId_PlanName",
                table: "pricing_plans");

            migrationBuilder.AddColumn<string>(
                name: "PlanType",
                table: "pricing_plans",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.Sql("""
                UPDATE pricing_plans p
                LEFT JOIN roles r ON r.RoleId = p.RoleId
                SET p.PlanType = CASE
                    WHEN r.Prefix = 'SBU' THEN 'MW Plan (For MW Users)'
                    WHEN r.Prefix = 'FRP' AND p.PlanName LIKE '%Franchise%' THEN 'Franchise Fees'
                    WHEN r.Prefix = 'FRP' THEN 'MW Plan (For Franchise Partner)'
                    WHEN r.Prefix = 'DMP' THEN 'Digital Marketer Partnership Fees'
                    ELSE LEFT(COALESCE(r.RoleName, ''), 100)
                END;
                """);

            migrationBuilder.DropColumn(
                name: "RoleId",
                table: "pricing_plans");

            migrationBuilder.CreateIndex(
                name: "IX_pricing_plans_PlanType_PlanName",
                table: "pricing_plans",
                columns: new[] { "PlanType", "PlanName" });
        }
    }
}
