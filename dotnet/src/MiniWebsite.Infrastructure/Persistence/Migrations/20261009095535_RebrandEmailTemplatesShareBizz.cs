using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniWebsite.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RebrandEmailTemplatesShareBizz : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var column in new[] { "Subject", "BodyHtml" })
            {
                migrationBuilder.Sql($@"
UPDATE `email_templates` SET `{column}` =
    REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(`{column}`,
        'https://www.miniwebsite.in', 'https://sharebizz.com'),
        'www.miniwebsite.in', 'www.sharebizz.com'),
        'MiniWebsite.in', 'ShareBizz'),
        'Mini Website', 'ShareBizz'),
        'MiniWebsite', 'ShareBizz');");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
