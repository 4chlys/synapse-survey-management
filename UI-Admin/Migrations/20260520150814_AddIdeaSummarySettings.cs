using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SynapseEnqueteManagement.UI.Admin.Migrations
{
    /// <inheritdoc />
    public partial class AddIdeaSummarySettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IdeaSummaryMaxIdeas",
                table: "PlatformSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PromptIdeaSummary",
                table: "PlatformSettings",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IdeaSummaryMaxIdeas",
                table: "PlatformSettings");

            migrationBuilder.DropColumn(
                name: "PromptIdeaSummary",
                table: "PlatformSettings");
        }
    }
}
