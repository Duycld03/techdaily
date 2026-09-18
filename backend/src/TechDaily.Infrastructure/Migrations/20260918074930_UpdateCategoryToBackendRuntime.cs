using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechDaily.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCategoryToBackendRuntime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE \"Topics\" SET \"Category\" = 'BackendRuntime' WHERE \"Category\" IN ('BackendDotNet', 'DotNet');");
            migrationBuilder.Sql("UPDATE \"DocumentBooks\" SET \"Category\" = 'BackendRuntime' WHERE \"Category\" IN ('BackendDotNet', 'DotNet');");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE \"Topics\" SET \"Category\" = 'BackendDotNet' WHERE \"Category\" = 'BackendRuntime';");
            migrationBuilder.Sql("UPDATE \"DocumentBooks\" SET \"Category\" = 'BackendDotNet' WHERE \"Category\" = 'BackendRuntime';");
        }
    }
}
