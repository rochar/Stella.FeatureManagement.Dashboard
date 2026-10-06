using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Stella.FeatureManagement.Dashboard.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFeatureFlagUsageDateIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_FeatureFlagUsages_Date",
                schema: "features",
                table: "FeatureFlagUsages",
                column: "Date");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FeatureFlagUsages_Date",
                schema: "features",
                table: "FeatureFlagUsages");
        }
    }
}
