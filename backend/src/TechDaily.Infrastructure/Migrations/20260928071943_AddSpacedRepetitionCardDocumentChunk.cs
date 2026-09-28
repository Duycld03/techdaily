using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechDaily.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSpacedRepetitionCardDocumentChunk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SourceDocumentChunkId",
                table: "SpacedRepetitionCards",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SpacedRepetitionCards_SourceDocumentChunkId",
                table: "SpacedRepetitionCards",
                column: "SourceDocumentChunkId");

            migrationBuilder.CreateIndex(
                name: "IX_SpacedRepetitionCards_UserId_SourceDocumentChunkId",
                table: "SpacedRepetitionCards",
                columns: new[] { "UserId", "SourceDocumentChunkId" });

            migrationBuilder.AddForeignKey(
                name: "FK_SpacedRepetitionCards_DocumentChunks_SourceDocumentChunkId",
                table: "SpacedRepetitionCards",
                column: "SourceDocumentChunkId",
                principalTable: "DocumentChunks",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SpacedRepetitionCards_DocumentChunks_SourceDocumentChunkId",
                table: "SpacedRepetitionCards");

            migrationBuilder.DropIndex(
                name: "IX_SpacedRepetitionCards_SourceDocumentChunkId",
                table: "SpacedRepetitionCards");

            migrationBuilder.DropIndex(
                name: "IX_SpacedRepetitionCards_UserId_SourceDocumentChunkId",
                table: "SpacedRepetitionCards");

            migrationBuilder.DropColumn(
                name: "SourceDocumentChunkId",
                table: "SpacedRepetitionCards");
        }
    }
}
