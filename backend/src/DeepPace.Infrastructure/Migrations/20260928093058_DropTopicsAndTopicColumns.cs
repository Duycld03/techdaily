using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeepPace.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DropTopicsAndTopicColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InterviewQuestions_Topics_TopicId",
                table: "InterviewQuestions");

            migrationBuilder.DropForeignKey(
                name: "FK_SpacedRepetitionCards_Topics_TopicId",
                table: "SpacedRepetitionCards");

            migrationBuilder.DropTable(
                name: "Topics");

            migrationBuilder.DropIndex(
                name: "IX_SpacedRepetitionCards_TopicId",
                table: "SpacedRepetitionCards");

            migrationBuilder.DropIndex(
                name: "IX_SpacedRepetitionCards_UserId_TopicId",
                table: "SpacedRepetitionCards");

            migrationBuilder.DropIndex(
                name: "IX_InterviewQuestions_TopicId",
                table: "InterviewQuestions");

            migrationBuilder.DropColumn(
                name: "TopicId",
                table: "SpacedRepetitionCards");

            migrationBuilder.DropColumn(
                name: "TopicId",
                table: "InterviewQuestions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TopicId",
                table: "SpacedRepetitionCards",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TopicId",
                table: "InterviewQuestions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Topics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BenchmarkSnippet = table.Column<string>(type: "text", nullable: true),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DayOrder = table.Column<int>(type: "integer", nullable: false),
                    DeepDiveMarkdown = table.Column<string>(type: "text", nullable: false),
                    Difficulty = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    Slug = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Summary = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Topics", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SpacedRepetitionCards_TopicId",
                table: "SpacedRepetitionCards",
                column: "TopicId");

            migrationBuilder.CreateIndex(
                name: "IX_SpacedRepetitionCards_UserId_TopicId",
                table: "SpacedRepetitionCards",
                columns: new[] { "UserId", "TopicId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InterviewQuestions_TopicId",
                table: "InterviewQuestions",
                column: "TopicId");

            migrationBuilder.CreateIndex(
                name: "IX_Topics_DayOrder",
                table: "Topics",
                column: "DayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_Topics_Slug",
                table: "Topics",
                column: "Slug",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_InterviewQuestions_Topics_TopicId",
                table: "InterviewQuestions",
                column: "TopicId",
                principalTable: "Topics",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SpacedRepetitionCards_Topics_TopicId",
                table: "SpacedRepetitionCards",
                column: "TopicId",
                principalTable: "Topics",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
