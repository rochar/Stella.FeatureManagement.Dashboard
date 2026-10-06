using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Stella.FeatureManagement.Dashboard.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFeatureFlagUsage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FeatureFlagUsages",
                schema: "features",
                columns: table => new
                {
                    FeatureFlagId = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    EnabledCount = table.Column<long>(type: "bigint", nullable: false),
                    DisabledCount = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeatureFlagUsages", x => new { x.FeatureFlagId, x.Date });
                    table.ForeignKey(
                        name: "FK_FeatureFlagUsages_FeatureFlags_FeatureFlagId",
                        column: x => x.FeatureFlagId,
                        principalSchema: "features",
                        principalTable: "FeatureFlags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FeatureFlagUsages",
                schema: "features");
        }
    }
}
