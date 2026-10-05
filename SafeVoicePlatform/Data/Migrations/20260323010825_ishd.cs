using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafeVoicePlatform.Data.Migrations
{
    /// <inheritdoc />
    public partial class Ishd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_SurveyResponses_RespondedAt",
                table: "SurveyResponses",
                column: "RespondedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyResponses_SurveyId",
                table: "SurveyResponses",
                column: "SurveyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SurveyResponses_RespondedAt",
                table: "SurveyResponses");

            migrationBuilder.DropIndex(
                name: "IX_SurveyResponses_SurveyId",
                table: "SurveyResponses");
        }
    }
}
