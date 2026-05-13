using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SynapseEnqueteManagement.UI.Admin.Migrations
{
    /// <inheritdoc />
    public partial class AddPlatformMonitoringFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AiModelName",
                table: "PlatformSettings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CostDashboardUrl",
                table: "PlatformSettings",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AiModelName",
                table: "PlatformSettings");

            migrationBuilder.DropColumn(
                name: "CostDashboardUrl",
                table: "PlatformSettings");
        }
    }
}
