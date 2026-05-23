using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SynapseEnqueteManagement.UI.Admin.Migrations
{
    /// <inheritdoc />
    public partial class AddQuestionIdToWidgetPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "QuestionId",
                table: "UserWidgetPreferences",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "QuestionId",
                table: "UserWidgetPreferences");
        }
    }
}
