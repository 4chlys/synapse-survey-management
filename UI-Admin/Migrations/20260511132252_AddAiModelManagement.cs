using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SynapseEnqueteManagement.UI.Admin.Migrations
{
    /// <inheritdoc />
    public partial class AddAiModelManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PromptCommentModeration",
                table: "PlatformSettings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PromptCommentSupport",
                table: "PlatformSettings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PromptIdeaSupport",
                table: "PlatformSettings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PromptImageModeration",
                table: "PlatformSettings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PromptModeration",
                table: "PlatformSettings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PromptQuestionSupport",
                table: "PlatformSettings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PromptRewrite",
                table: "PlatformSettings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SelectedAiModel",
                table: "PlatformSettings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SystemAiSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                        .Annotation("Sqlite:Autoincrement", true),
                    AllowedModelIds = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemAiSettings", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SystemAiSettings");

            migrationBuilder.DropColumn(
                name: "PromptCommentModeration",
                table: "PlatformSettings");

            migrationBuilder.DropColumn(
                name: "PromptCommentSupport",
                table: "PlatformSettings");

            migrationBuilder.DropColumn(
                name: "PromptIdeaSupport",
                table: "PlatformSettings");

            migrationBuilder.DropColumn(
                name: "PromptImageModeration",
                table: "PlatformSettings");

            migrationBuilder.DropColumn(
                name: "PromptModeration",
                table: "PlatformSettings");

            migrationBuilder.DropColumn(
                name: "PromptQuestionSupport",
                table: "PlatformSettings");

            migrationBuilder.DropColumn(
                name: "PromptRewrite",
                table: "PlatformSettings");

            migrationBuilder.DropColumn(
                name: "SelectedAiModel",
                table: "PlatformSettings");
        }
    }
}
