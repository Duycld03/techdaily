using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechDaily.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentChunkAudio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DocumentChunkAudios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentChunkId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    VoiceId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    MimeType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, defaultValue: "audio/mpeg"),
                    AudioData = table.Column<byte[]>(type: "bytea", nullable: false),
                    CharacterCount = table.Column<int>(type: "integer", nullable: false),
                    DurationSeconds = table.Column<double>(type: "double precision", nullable: false, defaultValue: 0.0),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentChunkAudios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentChunkAudios_DocumentChunks_DocumentChunkId",
                        column: x => x.DocumentChunkId,
                        principalTable: "DocumentChunks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentChunkAudios_Lookup",
                table: "DocumentChunkAudios",
                columns: new[] { "DocumentChunkId", "ContentHash", "VoiceId" },
                unique: true,
                filter: "\"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentChunkAudios");
        }
    }
}
