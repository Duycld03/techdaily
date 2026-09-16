using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechDaily.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PruneMicroQuizFromDocumentChunks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MicroQuiz_AnswerIndex",
                table: "DocumentChunks");

            migrationBuilder.DropColumn(
                name: "MicroQuiz_Explanation",
                table: "DocumentChunks");

            migrationBuilder.DropColumn(
                name: "MicroQuiz_Options",
                table: "DocumentChunks");

            migrationBuilder.DropColumn(
                name: "MicroQuiz_Question",
                table: "DocumentChunks");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MicroQuiz_AnswerIndex",
                table: "DocumentChunks",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "MicroQuiz_Explanation",
                table: "DocumentChunks",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MicroQuiz_Options",
                table: "DocumentChunks",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MicroQuiz_Question",
                table: "DocumentChunks",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
