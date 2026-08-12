using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PickleCrew.Api.Data.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddCrewOnboarding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CrewProvisioningAttempts",
                columns: table => new
                {
                    CrewUuid = table.Column<Guid>(type: "uuid", nullable: false),
                    CrewName = table.Column<string>(type: "text", nullable: false),
                    WorkOsUserId = table.Column<string>(type: "text", nullable: false),
                    WorkOsOrganizationId = table.Column<string>(type: "text", nullable: true),
                    WorkOsMembershipId = table.Column<string>(type: "text", nullable: true),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrewProvisioningAttempts", x => x.CrewUuid);
                });

            migrationBuilder.CreateTable(
                name: "Crews",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Uuid = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    WorkOsOrganizationId = table.Column<string>(type: "text", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Crews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Crews_CrewProvisioningAttempts_Uuid",
                        column: x => x.Uuid,
                        principalTable: "CrewProvisioningAttempts",
                        principalColumn: "CrewUuid",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CrewProvisioningAttempts_WorkOsUserId",
                table: "CrewProvisioningAttempts",
                column: "WorkOsUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Crews_Uuid",
                table: "Crews",
                column: "Uuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Crews_WorkOsOrganizationId",
                table: "Crews",
                column: "WorkOsOrganizationId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Crews");

            migrationBuilder.DropTable(
                name: "CrewProvisioningAttempts");
        }
    }
}
