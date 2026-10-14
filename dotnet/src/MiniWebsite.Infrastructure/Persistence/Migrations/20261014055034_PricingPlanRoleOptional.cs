using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniWebsite.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PricingPlanRoleOptional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "RoleId",
                table: "pricing_plans",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE pricing_plans p
                SET p.RoleId = (SELECT r.RoleId FROM roles r WHERE r.Prefix = 'SBU' ORDER BY r.RoleId LIMIT 1)
                WHERE p.RoleId IS NULL;
                """);

            migrationBuilder.AlterColumn<int>(
                name: "RoleId",
                table: "pricing_plans",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
