using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeepPace.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminDocumentOperations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "CurrentAttemptGeneration",
                table: "DocumentBooks",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<Guid>(
                name: "CurrentAttemptId",
                table: "DocumentBooks",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeclaredLanguage",
                table: "DocumentBooks",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IngestionControlVersion",
                table: "DocumentBooks",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<Guid>(
                name: "CurrentAttemptId",
                table: "CatalogDocuments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AccountSourceQuotas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChargedBytes = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    ReservedBytes = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountSourceQuotas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IngestionAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetScope = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TargetDocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    PredecessorAttemptId = table.Column<Guid>(type: "uuid", nullable: true),
                    Origin = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    InitiatorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RequestId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CommandFingerprint = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    SourceId = table.Column<Guid>(type: "uuid", nullable: true),
                    Generation = table.Column<long>(type: "bigint", nullable: false),
                    Stage = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    State = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ProgressPercent = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    QueuedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EndedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ClaimToken = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ClaimExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ErrorCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SafeErrorGuidance = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IngestionAttempts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IngestionSources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceKind = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    StorageIdentity = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ByteLength = table.Column<long>(type: "bigint", nullable: false),
                    IntegritySha256 = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    QuotaAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeclaredLanguage = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "en"),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    FirstFailureAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RetentionExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Availability = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    DeletionState = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IngestionSources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StorageReservations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReservationToken = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReservedBytes = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsReleased = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IsCommitted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StorageReservations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentBooks_CurrentAttemptId",
                table: "DocumentBooks",
                column: "CurrentAttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogDocuments_CurrentAttemptId",
                table: "CatalogDocuments",
                column: "CurrentAttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountSourceQuotas_AccountId",
                table: "AccountSourceQuotas",
                column: "AccountId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IngestionAttempts_QueuedAtUtc",
                table: "IngestionAttempts",
                column: "QueuedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_IngestionAttempts_State_ClaimExpiresAtUtc",
                table: "IngestionAttempts",
                columns: new[] { "State", "ClaimExpiresAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_IngestionAttempts_TargetScope_TargetDocumentId",
                table: "IngestionAttempts",
                columns: new[] { "TargetScope", "TargetDocumentId" },
                unique: true,
                filter: "\"State\" IN ('Queued', 'Running')");

            migrationBuilder.CreateIndex(
                name: "IX_IngestionSources_QuotaAccountId",
                table: "IngestionSources",
                column: "QuotaAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_IngestionSources_RetentionExpiresAtUtc_Availability",
                table: "IngestionSources",
                columns: new[] { "RetentionExpiresAtUtc", "Availability" });

            migrationBuilder.CreateIndex(
                name: "IX_StorageReservations_AccountId_ExpiresAtUtc",
                table: "StorageReservations",
                columns: new[] { "AccountId", "ExpiresAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_StorageReservations_ReservationToken",
                table: "StorageReservations",
                column: "ReservationToken",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountSourceQuotas");

            migrationBuilder.DropTable(
                name: "IngestionAttempts");

            migrationBuilder.DropTable(
                name: "IngestionSources");

            migrationBuilder.DropTable(
                name: "StorageReservations");

            migrationBuilder.DropIndex(
                name: "IX_DocumentBooks_CurrentAttemptId",
                table: "DocumentBooks");

            migrationBuilder.DropIndex(
                name: "IX_CatalogDocuments_CurrentAttemptId",
                table: "CatalogDocuments");

            migrationBuilder.DropColumn(
                name: "CurrentAttemptGeneration",
                table: "DocumentBooks");

            migrationBuilder.DropColumn(
                name: "CurrentAttemptId",
                table: "DocumentBooks");

            migrationBuilder.DropColumn(
                name: "DeclaredLanguage",
                table: "DocumentBooks");

            migrationBuilder.DropColumn(
                name: "IngestionControlVersion",
                table: "DocumentBooks");

            migrationBuilder.DropColumn(
                name: "CurrentAttemptId",
                table: "CatalogDocuments");
        }
    }
}
