using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechDaily.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVectorHnswIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_TermExplanationCaches_Embedding",
                table: "TermExplanationCaches",
                column: "Embedding")
                .Annotation("Npgsql:IndexMethod", "hnsw")
                .Annotation("Npgsql:IndexOperators", new[] { "vector_cosine_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentChunks_Embedding",
                table: "DocumentChunks",
                column: "Embedding")
                .Annotation("Npgsql:IndexMethod", "hnsw")
                .Annotation("Npgsql:IndexOperators", new[] { "vector_cosine_ops" });

            migrationBuilder.Sql(
                "CREATE INDEX IF NOT EXISTS \"IX_QuizQuestions_LowerTopic_Level\" ON \"QuizQuestions\" (lower(\"Topic\"), \"Level\") WHERE \"IsDeleted\" = false;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "DROP INDEX IF EXISTS \"IX_QuizQuestions_LowerTopic_Level\";");

            migrationBuilder.DropIndex(
                name: "IX_TermExplanationCaches_Embedding",
                table: "TermExplanationCaches");

            migrationBuilder.DropIndex(
                name: "IX_DocumentChunks_Embedding",
                table: "DocumentChunks");
        }
    }
}
