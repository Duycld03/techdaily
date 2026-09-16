using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechDaily.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class GeneralizeSpacedRepetitionCards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "TopicId",
                table: "SpacedRepetitionCards",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "BackMarkdown",
                table: "SpacedRepetitionCards",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FrontMarkdown",
                table: "SpacedRepetitionCards",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SourceHighlightId",
                table: "SpacedRepetitionCards",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SourceQuizQuestionId",
                table: "SpacedRepetitionCards",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceType",
                table: "SpacedRepetitionCards",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_SpacedRepetitionCards_SourceHighlightId",
                table: "SpacedRepetitionCards",
                column: "SourceHighlightId");

            migrationBuilder.CreateIndex(
                name: "IX_SpacedRepetitionCards_SourceQuizQuestionId",
                table: "SpacedRepetitionCards",
                column: "SourceQuizQuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_SpacedRepetitionCards_UserId_SourceHighlightId",
                table: "SpacedRepetitionCards",
                columns: new[] { "UserId", "SourceHighlightId" });

            migrationBuilder.CreateIndex(
                name: "IX_SpacedRepetitionCards_UserId_SourceQuizQuestionId",
                table: "SpacedRepetitionCards",
                columns: new[] { "UserId", "SourceQuizQuestionId" });

            migrationBuilder.AddForeignKey(
                name: "FK_SpacedRepetitionCards_QuizQuestions_SourceQuizQuestionId",
                table: "SpacedRepetitionCards",
                column: "SourceQuizQuestionId",
                principalTable: "QuizQuestions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_SpacedRepetitionCards_UserHighlights_SourceHighlightId",
                table: "SpacedRepetitionCards",
                column: "SourceHighlightId",
                principalTable: "UserHighlights",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SpacedRepetitionCards_QuizQuestions_SourceQuizQuestionId",
                table: "SpacedRepetitionCards");

            migrationBuilder.DropForeignKey(
                name: "FK_SpacedRepetitionCards_UserHighlights_SourceHighlightId",
                table: "SpacedRepetitionCards");

            migrationBuilder.DropIndex(
                name: "IX_SpacedRepetitionCards_SourceHighlightId",
                table: "SpacedRepetitionCards");

            migrationBuilder.DropIndex(
                name: "IX_SpacedRepetitionCards_SourceQuizQuestionId",
                table: "SpacedRepetitionCards");

            migrationBuilder.DropIndex(
                name: "IX_SpacedRepetitionCards_UserId_SourceHighlightId",
                table: "SpacedRepetitionCards");

            migrationBuilder.DropIndex(
                name: "IX_SpacedRepetitionCards_UserId_SourceQuizQuestionId",
                table: "SpacedRepetitionCards");

            migrationBuilder.DropColumn(
                name: "BackMarkdown",
                table: "SpacedRepetitionCards");

            migrationBuilder.DropColumn(
                name: "FrontMarkdown",
                table: "SpacedRepetitionCards");

            migrationBuilder.DropColumn(
                name: "SourceHighlightId",
                table: "SpacedRepetitionCards");

            migrationBuilder.DropColumn(
                name: "SourceQuizQuestionId",
                table: "SpacedRepetitionCards");

            migrationBuilder.DropColumn(
                name: "SourceType",
                table: "SpacedRepetitionCards");

            migrationBuilder.AlterColumn<Guid>(
                name: "TopicId",
                table: "SpacedRepetitionCards",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
