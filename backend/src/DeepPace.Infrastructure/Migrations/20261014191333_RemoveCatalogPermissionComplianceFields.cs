using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeepPace.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCatalogPermissionComplianceFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConfirmedAiProcessingRights",
                table: "CatalogDocuments");

            migrationBuilder.DropColumn(
                name: "ConfirmedDistributionRights",
                table: "CatalogDocuments");

            migrationBuilder.DropColumn(
                name: "PermissionBasis",
                table: "CatalogDocuments");

            migrationBuilder.DropColumn(
                name: "PermissionEvidence",
                table: "CatalogDocuments");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ConfirmedAiProcessingRights",
                table: "CatalogDocuments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ConfirmedDistributionRights",
                table: "CatalogDocuments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PermissionBasis",
                table: "CatalogDocuments",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PermissionEvidence",
                table: "CatalogDocuments",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");
        }
    }
}
