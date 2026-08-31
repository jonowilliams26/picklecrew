using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PickleCrew.Api.Data.Database.Migrations
{
    /// <inheritdoc />
    public partial class AllowAdditionalCrews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CrewProvisioningAttempts_WorkOsUserId",
                table: "CrewProvisioningAttempts");

            migrationBuilder.AddColumn<Guid>(
                name: "RequestId",
                table: "CrewProvisioningAttempts",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "CrewProvisioningAttempts"
                SET "RequestId" = "CrewUuid";
                """
            );

            migrationBuilder.AlterColumn<Guid>(
                name: "RequestId",
                table: "CrewProvisioningAttempts",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CrewProvisioningAttempts_WorkOsUserId_RequestId",
                table: "CrewProvisioningAttempts",
                columns: new[] { "WorkOsUserId", "RequestId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CrewProvisioningAttempts_WorkOsUserId_RequestId",
                table: "CrewProvisioningAttempts");

            migrationBuilder.DropColumn(
                name: "RequestId",
                table: "CrewProvisioningAttempts");

            migrationBuilder.CreateIndex(
                name: "IX_CrewProvisioningAttempts_WorkOsUserId",
                table: "CrewProvisioningAttempts",
                column: "WorkOsUserId",
                unique: true);
        }
    }
}
