using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechDaily.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInterviewQuizTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QuizQuestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Topic = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    QuestionText = table.Column<string>(type: "text", nullable: false),
                    Options = table.Column<string>(type: "text", nullable: false),
                    CorrectOptionIndex = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    ExplanationMarkdown = table.Column<string>(type: "text", nullable: false),
                    Tags = table.Column<string>(type: "text", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuizQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuizQuestions_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "UserQuizProgresses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsMastered = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    LastSelectedOptionIndex = table.Column<int>(type: "integer", nullable: true),
                    IsLastAnswerCorrect = table.Column<bool>(type: "boolean", nullable: true),
                    CorrectCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    IncorrectCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    LastAttemptedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserQuizProgresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserQuizProgresses_QuizQuestions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "QuizQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserQuizProgresses_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QuizQuestions_Category_Level",
                table: "QuizQuestions",
                columns: new[] { "Category", "Level" });

            migrationBuilder.CreateIndex(
                name: "IX_QuizQuestions_CreatedByUserId",
                table: "QuizQuestions",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_QuizQuestions_Topic_Level",
                table: "QuizQuestions",
                columns: new[] { "Topic", "Level" });

            migrationBuilder.CreateIndex(
                name: "IX_UserQuizProgresses_QuestionId",
                table: "UserQuizProgresses",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserQuizProgresses_UserId_IsMastered",
                table: "UserQuizProgresses",
                columns: new[] { "UserId", "IsMastered" });

            migrationBuilder.CreateIndex(
                name: "IX_UserQuizProgresses_UserId_QuestionId",
                table: "UserQuizProgresses",
                columns: new[] { "UserId", "QuestionId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserQuizProgresses");

            migrationBuilder.DropTable(
                name: "QuizQuestions");
        }
    }
}
