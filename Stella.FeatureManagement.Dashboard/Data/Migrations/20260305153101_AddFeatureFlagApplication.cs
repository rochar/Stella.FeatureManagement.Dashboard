using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Stella.FeatureManagement.Dashboard.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFeatureFlagApplication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Application",
                schema: "features",
                table: "FeatureFlags",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "Default");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Application",
                schema: "features",
                table: "FeatureFlags");
        }
    }
}
