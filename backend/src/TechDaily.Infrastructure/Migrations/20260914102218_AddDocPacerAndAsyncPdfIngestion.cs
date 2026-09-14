using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechDaily.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDocPacerAndAsyncPdfIngestion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "TopicId",
                table: "InterviewQuestions",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "DocumentChunkId",
                table: "InterviewQuestions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ErrorMessage",
                table: "DocumentBooks",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsFeatured",
                table: "DocumentBooks",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ProgressPercentage",
                table: "DocumentBooks",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "DocumentBooks",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StatusMessage",
                table: "DocumentBooks",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.Sql("UPDATE \"DocumentBooks\" SET \"Status\" = 'Ready' WHERE \"Status\" = '' OR \"Status\" IS NULL;");
            migrationBuilder.Sql("UPDATE \"DocumentBooks\" SET \"IsFeatured\" = true WHERE \"Slug\" = '30-day-senior-curriculum';");

            migrationBuilder.CreateTable(
                name: "UserBookPacers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentBookId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrentChunkOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    DailyPaceChunks = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    LastReadDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserBookPacers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserBookPacers_DocumentBooks_DocumentBookId",
                        column: x => x.DocumentBookId,
                        principalTable: "DocumentBooks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserBookPacers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InterviewQuestions_DocumentChunkId",
                table: "InterviewQuestions",
                column: "DocumentChunkId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentBooks_IsFeatured",
                table: "DocumentBooks",
                column: "IsFeatured");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentBooks_Status",
                table: "DocumentBooks",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_UserBookPacers_DocumentBookId",
                table: "UserBookPacers",
                column: "DocumentBookId");

            migrationBuilder.CreateIndex(
                name: "IX_UserBookPacers_UserId_DocumentBookId",
                table: "UserBookPacers",
                columns: new[] { "UserId", "DocumentBookId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserBookPacers_UserId_IsActive",
                table: "UserBookPacers",
                columns: new[] { "UserId", "IsActive" });

            migrationBuilder.AddForeignKey(
                name: "FK_InterviewQuestions_DocumentChunks_DocumentChunkId",
                table: "InterviewQuestions",
                column: "DocumentChunkId",
                principalTable: "DocumentChunks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InterviewQuestions_DocumentChunks_DocumentChunkId",
                table: "InterviewQuestions");

            migrationBuilder.DropTable(
                name: "UserBookPacers");

            migrationBuilder.DropIndex(
                name: "IX_InterviewQuestions_DocumentChunkId",
                table: "InterviewQuestions");

            migrationBuilder.DropIndex(
                name: "IX_DocumentBooks_IsFeatured",
                table: "DocumentBooks");

            migrationBuilder.DropIndex(
                name: "IX_DocumentBooks_Status",
                table: "DocumentBooks");

            migrationBuilder.DropColumn(
                name: "DocumentChunkId",
                table: "InterviewQuestions");

            migrationBuilder.DropColumn(
                name: "ErrorMessage",
                table: "DocumentBooks");

            migrationBuilder.DropColumn(
                name: "IsFeatured",
                table: "DocumentBooks");

            migrationBuilder.DropColumn(
                name: "ProgressPercentage",
                table: "DocumentBooks");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "DocumentBooks");

            migrationBuilder.DropColumn(
                name: "StatusMessage",
                table: "DocumentBooks");

            migrationBuilder.AlterColumn<Guid>(
                name: "TopicId",
                table: "InterviewQuestions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
