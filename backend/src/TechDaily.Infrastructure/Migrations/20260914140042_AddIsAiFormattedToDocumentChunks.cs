using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechDaily.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIsAiFormattedToDocumentChunks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAiFormatted",
                table: "DocumentChunks",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsAiFormatted",
                table: "DocumentChunks");
        }
    }
}
