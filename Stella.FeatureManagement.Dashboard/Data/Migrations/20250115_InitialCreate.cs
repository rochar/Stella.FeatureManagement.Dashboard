using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Stella.FeatureManagement.Dashboard.Data.Migrations;

/// <inheritdoc />
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "FeatureFlags",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_FeatureFlags", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "FeatureFilters",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                FeatureFlagId = table.Column<int>(type: "INTEGER", nullable: false),
                FilterType = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                Parameters = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_FeatureFilters", x => x.Id);
                table.ForeignKey(
                    name: "FK_FeatureFilters_FeatureFlags_FeatureFlagId",
                    column: x => x.FeatureFlagId,
                    principalTable: "FeatureFlags",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_FeatureFilters_FeatureFlagId",
            table: "FeatureFilters",
            column: "FeatureFlagId");

        migrationBuilder.CreateIndex(
            name: "IX_FeatureFlags_Name",
            table: "FeatureFlags",
            column: "Name",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "FeatureFilters");

        migrationBuilder.DropTable(
            name: "FeatureFlags");
    }
}
