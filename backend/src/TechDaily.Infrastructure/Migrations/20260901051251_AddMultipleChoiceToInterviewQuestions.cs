using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechDaily.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMultipleChoiceToInterviewQuestions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CorrectOptionIndex",
                table: "InterviewQuestions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ExplanationMarkdown",
                table: "InterviewQuestions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Options",
                table: "InterviewQuestions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsCorrect",
                table: "DailyDrills",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Score",
                table: "DailyDrills",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SelectedOptionIndex",
                table: "DailyDrills",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CorrectOptionIndex",
                table: "InterviewQuestions");

            migrationBuilder.DropColumn(
                name: "ExplanationMarkdown",
                table: "InterviewQuestions");

            migrationBuilder.DropColumn(
                name: "Options",
                table: "InterviewQuestions");

            migrationBuilder.DropColumn(
                name: "IsCorrect",
                table: "DailyDrills");

            migrationBuilder.DropColumn(
                name: "Score",
                table: "DailyDrills");

            migrationBuilder.DropColumn(
                name: "SelectedOptionIndex",
                table: "DailyDrills");
        }
    }
}
