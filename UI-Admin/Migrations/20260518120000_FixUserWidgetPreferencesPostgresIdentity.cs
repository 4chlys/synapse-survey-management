using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SynapseEnqueteManagement.UI.Admin.Migrations
{
    /// <inheritdoc />
    public partial class FixUserWidgetPreferencesPostgresIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder.ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                migrationBuilder.Sql(@"
                    CREATE SEQUENCE IF NOT EXISTS ""UserWidgetPreferences_Id_seq"";
                    ALTER TABLE ""UserWidgetPreferences""
                        ALTER COLUMN ""Id"" SET DEFAULT nextval('""UserWidgetPreferences_Id_seq""');
                    ALTER SEQUENCE ""UserWidgetPreferences_Id_seq""
                        OWNED BY ""UserWidgetPreferences"".""Id"";
                    SELECT setval(
                        '""UserWidgetPreferences_Id_seq""',
                        COALESCE((SELECT MAX(""Id"") FROM ""UserWidgetPreferences""), 0) + 1,
                        false
                    );
                ");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder.ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                migrationBuilder.Sql(@"
                    ALTER TABLE ""UserWidgetPreferences""
                        ALTER COLUMN ""Id"" DROP DEFAULT;
                ");
            }
        }
    }
}
