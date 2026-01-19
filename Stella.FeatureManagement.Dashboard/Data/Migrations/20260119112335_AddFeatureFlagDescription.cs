using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Stella.FeatureManagement.Dashboard.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFeatureFlagDescription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                schema: "features",
                table: "FeatureFlags",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                schema: "features",
                table: "FeatureFlags");
        }
    }
}
