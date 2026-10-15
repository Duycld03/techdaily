using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeepPace.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminAccountsAndAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AdminControlVersion",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "AuthenticationVersion",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Active");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SuspendedAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SuspendedByUserId",
                table: "Users",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SuspensionReason",
                table: "Users",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AuthenticationVersion",
                table: "RefreshTokens",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "AdminAuditEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TargetType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TargetId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Outcome = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ErrorCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ChangedFields = table.Column<string>(type: "text", nullable: false),
                    StatePayload = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminAuditEvents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdminAuditEvents_ActorUserId_OccurredAt_Id",
                table: "AdminAuditEvents",
                columns: new[] { "ActorUserId", "OccurredAt", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_AdminAuditEvents_OccurredAt_Id",
                table: "AdminAuditEvents",
                columns: new[] { "OccurredAt", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_AdminAuditEvents_TargetType_TargetId_OccurredAt_Id",
                table: "AdminAuditEvents",
                columns: new[] { "TargetType", "TargetId", "OccurredAt", "Id" });

            // Ensure backfill data invariants on existing rows
            migrationBuilder.Sql("UPDATE \"Users\" SET \"AuthenticationVersion\" = 1 WHERE \"AuthenticationVersion\" = 0;");
            migrationBuilder.Sql("UPDATE \"Users\" SET \"AdminControlVersion\" = 1 WHERE \"AdminControlVersion\" = 0;");
            migrationBuilder.Sql("UPDATE \"Users\" SET \"Status\" = 'Active' WHERE \"Status\" IS NULL OR \"Status\" = '';");
            migrationBuilder.Sql("UPDATE \"RefreshTokens\" SET \"AuthenticationVersion\" = 1 WHERE \"AuthenticationVersion\" = 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdminAuditEvents");

            migrationBuilder.DropColumn(
                name: "AdminControlVersion",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AuthenticationVersion",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SuspendedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SuspendedByUserId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SuspensionReason",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AuthenticationVersion",
                table: "RefreshTokens");
        }
    }
}
