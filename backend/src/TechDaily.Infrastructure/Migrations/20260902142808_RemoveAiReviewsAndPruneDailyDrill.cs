using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechDaily.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveAiReviewsAndPruneDailyDrill : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiReviews");

            migrationBuilder.DropColumn(
                name: "UserAnswerText",
                table: "DailyDrills");

            migrationBuilder.DropColumn(
                name: "UserAudioUrl",
                table: "DailyDrills");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserAnswerText",
                table: "DailyDrills",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserAudioUrl",
                table: "DailyDrills",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AiReviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DailyDrillId = table.Column<Guid>(type: "uuid", nullable: false),
                    AiModelUsed = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ImprovedAnswerMarkdown = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    MissingPoints = table.Column<string>(type: "text", nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    Strengths = table.Column<string>(type: "text", nullable: false),
                    SummaryFeedback = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiReviews_DailyDrills_DailyDrillId",
                        column: x => x.DailyDrillId,
                        principalTable: "DailyDrills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiReviews_DailyDrillId",
                table: "AiReviews",
                column: "DailyDrillId",
                unique: true);
        }
    }
}
