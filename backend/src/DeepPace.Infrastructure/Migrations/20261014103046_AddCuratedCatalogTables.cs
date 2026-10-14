using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

namespace DeepPace.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCuratedCatalogTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SourceAttribution",
                table: "DocumentBooks",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SourceCatalogDocumentId",
                table: "DocumentBooks",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SourceCatalogRevision",
                table: "DocumentBooks",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CatalogDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Language = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "en"),
                    SourceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AuthorOrSourceUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    AttributionText = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    PermissionBasis = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    PermissionEvidence = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ConfirmedDistributionRights = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ConfirmedAiProcessingRights = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    Visibility = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ProcessingStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TotalSlices = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    EstimatedReadMinutes = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    Revision = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    ConcurrencyVersion = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    ProcessingGeneration = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    ProcessingLeaseExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ProcessingStatusMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ProcessingErrorMessage = table.Column<string>(type: "text", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewedRevision = table.Column<int>(type: "integer", nullable: true),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PublishedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    HiddenByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    HiddenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogDocuments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CatalogSlices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CatalogDocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    SliceOrder = table.Column<int>(type: "integer", nullable: false),
                    ChapterTitle = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    OriginalTextMarkdown = table.Column<string>(type: "text", nullable: false),
                    SummaryMarkdown = table.Column<string>(type: "text", nullable: false),
                    KeyTakeaways = table.Column<string>(type: "text", nullable: false),
                    Language = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "en"),
                    Embedding = table.Column<Vector>(type: "vector(768)", nullable: true),
                    EstimatedReadMinutes = table.Column<int>(type: "integer", nullable: false),
                    IsAiFormatted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogSlices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CatalogSlices_CatalogDocuments_CatalogDocumentId",
                        column: x => x.CatalogDocumentId,
                        principalTable: "CatalogDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentBooks_CreatedByUserId_SourceCatalogDocumentId",
                table: "DocumentBooks",
                columns: new[] { "CreatedByUserId", "SourceCatalogDocumentId" },
                unique: true,
                filter: "\"CreatedByUserId\" IS NOT NULL AND \"SourceCatalogDocumentId\" IS NOT NULL AND \"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogDocuments_ProcessingStatus",
                table: "CatalogDocuments",
                column: "ProcessingStatus");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogDocuments_Visibility",
                table: "CatalogDocuments",
                column: "Visibility");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogDocuments_Visibility_PublishedAt",
                table: "CatalogDocuments",
                columns: new[] { "Visibility", "PublishedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CatalogSlices_CatalogDocumentId_SliceOrder",
                table: "CatalogSlices",
                columns: new[] { "CatalogDocumentId", "SliceOrder" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogSlices_Embedding",
                table: "CatalogSlices",
                column: "Embedding")
                .Annotation("Npgsql:IndexMethod", "hnsw")
                .Annotation("Npgsql:IndexOperators", new[] { "vector_cosine_ops" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CatalogSlices");

            migrationBuilder.DropTable(
                name: "CatalogDocuments");

            migrationBuilder.DropIndex(
                name: "IX_DocumentBooks_CreatedByUserId_SourceCatalogDocumentId",
                table: "DocumentBooks");

            migrationBuilder.DropColumn(
                name: "SourceAttribution",
                table: "DocumentBooks");

            migrationBuilder.DropColumn(
                name: "SourceCatalogDocumentId",
                table: "DocumentBooks");

            migrationBuilder.DropColumn(
                name: "SourceCatalogRevision",
                table: "DocumentBooks");
        }
    }
}
