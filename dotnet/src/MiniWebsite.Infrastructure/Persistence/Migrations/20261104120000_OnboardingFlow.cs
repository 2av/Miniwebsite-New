using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniWebsite.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(Persistence.ApplicationDbContext))]
    [Migration("20261104120000_OnboardingFlow")]
    public class OnboardingFlow : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE `users`
                    ADD COLUMN `OnboardingStage` varchar(40) NOT NULL DEFAULT 'Ready to sell',
                    ADD COLUMN `OnboardingStageAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
                    ADD COLUMN `PaidAt` datetime(6) NULL,
                    ADD COLUMN `ReferralEligibleAt` datetime(6) NULL;
                """);

            migrationBuilder.Sql("""
                UPDATE `users` u
                INNER JOIN `roles` r ON r.`RoleId` = u.`RoleId`
                SET u.`ReferralEligibleAt` = u.`CreatedAt`
                WHERE r.`Prefix` = 'SBU';
                """);

            migrationBuilder.Sql("""
                ALTER TABLE `users` ADD CONSTRAINT `CK_users_OnboardingStage`
                CHECK (`OnboardingStage` IN (
                    'Account only', 'Payment due', 'Features', 'Document verification',
                    'Waiting for review', 'Approved', 'Ready to sell'));
                """);

            migrationBuilder.Sql("""
                ALTER TABLE `referral_earnings`
                    ADD COLUMN `PayoutAcceptance` varchar(40) NULL,
                    ADD COLUMN `PayoutInvoiceId` int NULL;
                """);
            migrationBuilder.Sql("CREATE INDEX `IX_referral_earnings_PayoutInvoiceId` ON `referral_earnings` (`PayoutInvoiceId`);");
            migrationBuilder.Sql("""
                ALTER TABLE `referral_earnings`
                ADD CONSTRAINT `FK_referral_earnings_invoices_PayoutInvoiceId`
                FOREIGN KEY (`PayoutInvoiceId`) REFERENCES `invoices` (`Id`) ON DELETE SET NULL;
                """);

            migrationBuilder.Sql("ALTER TABLE `registration_pending` ADD COLUMN `IssuedPassword` varchar(255) NULL;");

            migrationBuilder.Sql("""
                CREATE TABLE `account_invites` (
                    `Id` int NOT NULL AUTO_INCREMENT,
                    `CreatedAt` datetime(6) NOT NULL,
                    `UpdatedAt` datetime(6) NULL,
                    `IsDeleted` tinyint(1) NOT NULL,
                    `Email` varchar(255) NOT NULL,
                    `RolePrefix` varchar(20) NOT NULL,
                    `TokenHash` varchar(128) NOT NULL,
                    `InvitedByUserId` int NULL,
                    `ExpiresAt` datetime(6) NOT NULL,
                    `IsConsumed` tinyint(1) NOT NULL,
                    CONSTRAINT `PK_account_invites` PRIMARY KEY (`Id`),
                    CONSTRAINT `FK_account_invites_users_InvitedByUserId` FOREIGN KEY (`InvitedByUserId`) REFERENCES `users` (`Id`) ON DELETE SET NULL
                );
                """);
            migrationBuilder.Sql("CREATE UNIQUE INDEX `IX_account_invites_TokenHash` ON `account_invites` (`TokenHash`);");
            migrationBuilder.Sql("CREATE INDEX `IX_account_invites_Email` ON `account_invites` (`Email`);");
            migrationBuilder.Sql("CREATE INDEX `IX_account_invites_InvitedByUserId` ON `account_invites` (`InvitedByUserId`);");

            migrationBuilder.Sql("""
                CREATE TABLE `field_sales_deals` (
                    `Id` int NOT NULL AUTO_INCREMENT,
                    `UserId` int NOT NULL,
                    `Details` varchar(2000) NOT NULL,
                    `Amount` decimal(12,2) NOT NULL,
                    `DealDate` datetime(6) NOT NULL,
                    `CreatedAt` datetime(6) NOT NULL,
                    CONSTRAINT `PK_field_sales_deals` PRIMARY KEY (`Id`),
                    CONSTRAINT `FK_field_sales_deals_users_UserId` FOREIGN KEY (`UserId`) REFERENCES `users` (`Id`) ON DELETE CASCADE
                );
                """);
            migrationBuilder.Sql("CREATE INDEX `IX_field_sales_deals_UserId` ON `field_sales_deals` (`UserId`);");
            migrationBuilder.Sql("CREATE INDEX `IX_field_sales_deals_UserId_DealDate` ON `field_sales_deals` (`UserId`, `DealDate`);");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS `field_sales_deals`;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS `account_invites`;");
            migrationBuilder.Sql("ALTER TABLE `registration_pending` DROP COLUMN `IssuedPassword`;");
            migrationBuilder.Sql("ALTER TABLE `referral_earnings` DROP FOREIGN KEY `FK_referral_earnings_invoices_PayoutInvoiceId`;");
            migrationBuilder.Sql("DROP INDEX `IX_referral_earnings_PayoutInvoiceId` ON `referral_earnings`;");
            migrationBuilder.Sql("ALTER TABLE `referral_earnings` DROP COLUMN `PayoutAcceptance`, DROP COLUMN `PayoutInvoiceId`;");
            migrationBuilder.Sql("ALTER TABLE `users` DROP CONSTRAINT `CK_users_OnboardingStage`;");
            migrationBuilder.Sql("ALTER TABLE `users` DROP COLUMN `OnboardingStage`, DROP COLUMN `OnboardingStageAt`, DROP COLUMN `PaidAt`, DROP COLUMN `ReferralEligibleAt`;");
        }
    }
}
