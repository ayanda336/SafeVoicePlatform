using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafeVoicePlatform.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddReferralSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Referrals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReportId = table.Column<int>(type: "int", nullable: false),
                    SupportOrgId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ReferredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReferredBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ContactMade = table.Column<bool>(type: "bit", nullable: false),
                    ContactDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Outcome = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Referrals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Referrals_Reports_ReportId",
                        column: x => x.ReportId,
                        principalTable: "Reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Referrals_SupportOrganizations_SupportOrgId",
                        column: x => x.SupportOrgId,
                        principalTable: "SupportOrganizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Referrals_ReferredAt",
                table: "Referrals",
                column: "ReferredAt");

            migrationBuilder.CreateIndex(
                name: "IX_Referrals_ReportId",
                table: "Referrals",
                column: "ReportId");

            migrationBuilder.CreateIndex(
                name: "IX_Referrals_Status",
                table: "Referrals",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Referrals_SupportOrgId",
                table: "Referrals",
                column: "SupportOrgId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Referrals");
        }
    }
}
