using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechDaily.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTechInsightsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TechInsights",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Slug = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    Tags = table.Column<string>(type: "text", nullable: false),
                    SummaryMarkdown = table.Column<string>(type: "text", nullable: false),
                    ProblemSnippet = table.Column<string>(type: "text", nullable: false),
                    SolutionSnippet = table.Column<string>(type: "text", nullable: false),
                    UnderTheHoodMarkdown = table.Column<string>(type: "text", nullable: false),
                    BenchmarkStats = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    SourceUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    LikesCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    BookmarksCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TechInsights", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TechInsights_Category_IsPublished",
                table: "TechInsights",
                columns: new[] { "Category", "IsPublished" });

            migrationBuilder.CreateIndex(
                name: "IX_TechInsights_Slug",
                table: "TechInsights",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TechInsights");
        }
    }
}
