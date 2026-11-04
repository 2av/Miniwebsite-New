using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniWebsite.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(Persistence.ApplicationDbContext))]
    [Migration("20261104050000_AllowWebsiteUploadFolder")]
    public class AllowWebsiteUploadFolder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // MariaDB rejects DROP CHECK. DROP CONSTRAINT replaces the folder rule.
            migrationBuilder.Sql("ALTER TABLE `uploaded_files` DROP CONSTRAINT `CK_uploaded_files_Folder`;");
            migrationBuilder.Sql("ALTER TABLE `uploaded_files` ADD CONSTRAINT `CK_uploaded_files_Folder` CHECK (`Folder` IN ('profile', 'verification', 'website'));");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE `uploaded_files` DROP CONSTRAINT `CK_uploaded_files_Folder`;");
            migrationBuilder.Sql("ALTER TABLE `uploaded_files` ADD CONSTRAINT `CK_uploaded_files_Folder` CHECK (`Folder` IN ('profile', 'verification'));");
        }
    }
}
