using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SynapseEnqueteManagement.UI.Admin.Migrations
{
    /// <inheritdoc />
    public partial class AddAiAssistanceMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AiAssistanceMode",
                table: "PlatformSettings",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AiAssistanceMode",
                table: "PlatformSettings");
        }
    }
}
