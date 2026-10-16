using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DeepPace.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminManagedCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TechInsights_Category_IsPublished",
                table: "TechInsights");

            migrationBuilder.DropIndex(
                name: "IX_QuizQuestions_Category_Level",
                table: "QuizQuestions");

            migrationBuilder.RenameColumn(
                name: "Category",
                table: "TermExplanationCaches",
                newName: "ContextLabel");

            migrationBuilder.CreateTable(
                name: "DocumentCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    NameEn = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    NameVi = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    ConcurrencyVersion = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentCategories", x => x.Id);
                    table.CheckConstraint("CK_DocumentCategories_ConcurrencyVersion", "\"ConcurrencyVersion\" > 0");
                    table.CheckConstraint("CK_DocumentCategories_SortOrder", "\"SortOrder\" >= 0 AND \"SortOrder\" <= 10000");
                });

            migrationBuilder.InsertData(
                table: "DocumentCategories",
                columns: new[] { "Id", "ConcurrencyVersion", "CreatedAt", "IsActive", "IsDeleted", "NameEn", "NameVi", "Slug", "UpdatedAt" },
                values: new object[] { new Guid("10000000-0000-4000-8000-000000000000"), 1, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, false, "Frontend & Web", "Frontend & Web", "frontend-web", null });

            migrationBuilder.InsertData(
                table: "DocumentCategories",
                columns: new[] { "Id", "ConcurrencyVersion", "CreatedAt", "IsActive", "IsDeleted", "NameEn", "NameVi", "Slug", "SortOrder", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("10000000-0000-4000-8000-000000000001"), 1, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, false, "Backend & Runtime", "Backend & Runtime", "backend-runtime", 1, null },
                    { new Guid("10000000-0000-4000-8000-000000000002"), 1, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, false, "Database & Storage", "Cơ Sở Dữ Liệu & Lưu Trữ", "database-storage", 2, null },
                    { new Guid("10000000-0000-4000-8000-000000000003"), 1, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, false, "Distributed Systems", "Hệ Thống Phân Tán", "system-design", 3, null },
                    { new Guid("10000000-0000-4000-8000-000000000004"), 1, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, false, "Clean Code & Software Design", "Mã Sạch & Thiết Kế Phần Mềm", "engineering-craft", 4, null },
                    { new Guid("10000000-0000-4000-8000-000000000005"), 1, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, false, "Mental Models & Decisions", "Mô Hình Tư Duy & Quyết Định", "mental-models", 5, null },
                    { new Guid("10000000-0000-4000-8000-000000000006"), 1, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, false, "Habits & Deep Work", "Thói Quen & Tập Trung Sâu", "habits-productivity", 6, null }
                });

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "PermissionKey", "RoleId" },
                values: new object[] { "ManageCategories", new Guid("20000000-0000-4000-8000-000000000002") });

            migrationBuilder.AddColumn<Guid>(
                name: "CategoryId",
                table: "TechInsights",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CategoryId",
                table: "QuizQuestions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CategoryId",
                table: "DocumentBooks",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CategoryId",
                table: "CatalogDocuments",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql(@"
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM ""DocumentBooks""
        WHERE TRIM(LOWER(""Category"")) NOT IN (
            'frontendweb', '0',
            'backendruntime', 'backenddotnet', 'dotnet', '1',
            'databasestorage', '2',
            'systemdesign', '3',
            'engineeringcraft', '4',
            'mentalmodels', '5',
            'habitsproductivity', '6'
        ) OR ""Category"" IS NULL OR TRIM(""Category"") = ''
    ) THEN
        RAISE EXCEPTION 'Category preflight failed: DocumentBooks contains unknown, empty, or null categories.';
    END IF;

    IF EXISTS (
        SELECT 1 FROM ""CatalogDocuments""
        WHERE TRIM(LOWER(""Category"")) NOT IN (
            'frontendweb', '0',
            'backendruntime', 'backenddotnet', 'dotnet', '1',
            'databasestorage', '2',
            'systemdesign', '3',
            'engineeringcraft', '4',
            'mentalmodels', '5',
            'habitsproductivity', '6'
        ) OR ""Category"" IS NULL OR TRIM(""Category"") = ''
    ) THEN
        RAISE EXCEPTION 'Category preflight failed: CatalogDocuments contains unknown, empty, or null categories.';
    END IF;

    IF EXISTS (
        SELECT 1 FROM ""QuizQuestions""
        WHERE ""Category"" NOT IN (0, 1, 2, 3, 4, 5, 6) OR ""Category"" IS NULL
    ) THEN
        RAISE EXCEPTION 'Category preflight failed: QuizQuestions contains unknown or null categories.';
    END IF;

    IF EXISTS (
        SELECT 1 FROM ""TechInsights""
        WHERE ""Category"" NOT IN (0, 1, 2, 3, 4, 5, 6) OR ""Category"" IS NULL
    ) THEN
        RAISE EXCEPTION 'Category preflight failed: TechInsights contains unknown or null categories.';
    END IF;
END $$;
");

            migrationBuilder.Sql(@"
UPDATE ""DocumentBooks"" SET ""CategoryId"" = CASE
    WHEN TRIM(LOWER(""Category"")) IN ('frontendweb', '0') THEN '10000000-0000-4000-8000-000000000000'::uuid
    WHEN TRIM(LOWER(""Category"")) IN ('backendruntime', 'backenddotnet', 'dotnet', '1') THEN '10000000-0000-4000-8000-000000000001'::uuid
    WHEN TRIM(LOWER(""Category"")) IN ('databasestorage', '2') THEN '10000000-0000-4000-8000-000000000002'::uuid
    WHEN TRIM(LOWER(""Category"")) IN ('systemdesign', '3') THEN '10000000-0000-4000-8000-000000000003'::uuid
    WHEN TRIM(LOWER(""Category"")) IN ('engineeringcraft', '4') THEN '10000000-0000-4000-8000-000000000004'::uuid
    WHEN TRIM(LOWER(""Category"")) IN ('mentalmodels', '5') THEN '10000000-0000-4000-8000-000000000005'::uuid
    WHEN TRIM(LOWER(""Category"")) IN ('habitsproductivity', '6') THEN '10000000-0000-4000-8000-000000000006'::uuid
END;

UPDATE ""CatalogDocuments"" SET ""CategoryId"" = CASE
    WHEN TRIM(LOWER(""Category"")) IN ('frontendweb', '0') THEN '10000000-0000-4000-8000-000000000000'::uuid
    WHEN TRIM(LOWER(""Category"")) IN ('backendruntime', 'backenddotnet', 'dotnet', '1') THEN '10000000-0000-4000-8000-000000000001'::uuid
    WHEN TRIM(LOWER(""Category"")) IN ('databasestorage', '2') THEN '10000000-0000-4000-8000-000000000002'::uuid
    WHEN TRIM(LOWER(""Category"")) IN ('systemdesign', '3') THEN '10000000-0000-4000-8000-000000000003'::uuid
    WHEN TRIM(LOWER(""Category"")) IN ('engineeringcraft', '4') THEN '10000000-0000-4000-8000-000000000004'::uuid
    WHEN TRIM(LOWER(""Category"")) IN ('mentalmodels', '5') THEN '10000000-0000-4000-8000-000000000005'::uuid
    WHEN TRIM(LOWER(""Category"")) IN ('habitsproductivity', '6') THEN '10000000-0000-4000-8000-000000000006'::uuid
END;

UPDATE ""QuizQuestions"" SET ""CategoryId"" = CASE
    WHEN ""Category"" = 0 THEN '10000000-0000-4000-8000-000000000000'::uuid
    WHEN ""Category"" = 1 THEN '10000000-0000-4000-8000-000000000001'::uuid
    WHEN ""Category"" = 2 THEN '10000000-0000-4000-8000-000000000002'::uuid
    WHEN ""Category"" = 3 THEN '10000000-0000-4000-8000-000000000003'::uuid
    WHEN ""Category"" = 4 THEN '10000000-0000-4000-8000-000000000004'::uuid
    WHEN ""Category"" = 5 THEN '10000000-0000-4000-8000-000000000005'::uuid
    WHEN ""Category"" = 6 THEN '10000000-0000-4000-8000-000000000006'::uuid
END;

UPDATE ""TechInsights"" SET ""CategoryId"" = CASE
    WHEN ""Category"" = 0 THEN '10000000-0000-4000-8000-000000000000'::uuid
    WHEN ""Category"" = 1 THEN '10000000-0000-4000-8000-000000000001'::uuid
    WHEN ""Category"" = 2 THEN '10000000-0000-4000-8000-000000000002'::uuid
    WHEN ""Category"" = 3 THEN '10000000-0000-4000-8000-000000000003'::uuid
    WHEN ""Category"" = 4 THEN '10000000-0000-4000-8000-000000000004'::uuid
    WHEN ""Category"" = 5 THEN '10000000-0000-4000-8000-000000000005'::uuid
    WHEN ""Category"" = 6 THEN '10000000-0000-4000-8000-000000000006'::uuid
END;
");

            migrationBuilder.AlterColumn<Guid>(
                name: "CategoryId",
                table: "TechInsights",
                type: "uuid",
                nullable: false);

            migrationBuilder.AlterColumn<Guid>(
                name: "CategoryId",
                table: "QuizQuestions",
                type: "uuid",
                nullable: false);

            migrationBuilder.AlterColumn<Guid>(
                name: "CategoryId",
                table: "DocumentBooks",
                type: "uuid",
                nullable: false);

            migrationBuilder.AlterColumn<Guid>(
                name: "CategoryId",
                table: "CatalogDocuments",
                type: "uuid",
                nullable: false);

            migrationBuilder.DropColumn(
                name: "Category",
                table: "TechInsights");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "QuizQuestions");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "DocumentBooks");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "CatalogDocuments");

            migrationBuilder.CreateIndex(
                name: "IX_TechInsights_CategoryId_IsPublished",
                table: "TechInsights",
                columns: new[] { "CategoryId", "IsPublished" });

            migrationBuilder.CreateIndex(
                name: "IX_QuizQuestions_CategoryId_Level",
                table: "QuizQuestions",
                columns: new[] { "CategoryId", "Level" });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentBooks_CategoryId",
                table: "DocumentBooks",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogDocuments_CategoryId",
                table: "CatalogDocuments",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentCategories_IsActive_SortOrder_Slug",
                table: "DocumentCategories",
                columns: new[] { "IsActive", "SortOrder", "Slug" });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentCategories_Slug",
                table: "DocumentCategories",
                column: "Slug",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.AddForeignKey(
                name: "FK_CatalogDocuments_DocumentCategories_CategoryId",
                table: "CatalogDocuments",
                column: "CategoryId",
                principalTable: "DocumentCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentBooks_DocumentCategories_CategoryId",
                table: "DocumentBooks",
                column: "CategoryId",
                principalTable: "DocumentCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_QuizQuestions_DocumentCategories_CategoryId",
                table: "QuizQuestions",
                column: "CategoryId",
                principalTable: "DocumentCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TechInsights_DocumentCategories_CategoryId",
                table: "TechInsights",
                column: "CategoryId",
                principalTable: "DocumentCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CatalogDocuments_DocumentCategories_CategoryId",
                table: "CatalogDocuments");

            migrationBuilder.DropForeignKey(
                name: "FK_DocumentBooks_DocumentCategories_CategoryId",
                table: "DocumentBooks");

            migrationBuilder.DropForeignKey(
                name: "FK_QuizQuestions_DocumentCategories_CategoryId",
                table: "QuizQuestions");

            migrationBuilder.DropForeignKey(
                name: "FK_TechInsights_DocumentCategories_CategoryId",
                table: "TechInsights");

            migrationBuilder.DropTable(
                name: "DocumentCategories");

            migrationBuilder.DropIndex(
                name: "IX_TechInsights_CategoryId_IsPublished",
                table: "TechInsights");

            migrationBuilder.DropIndex(
                name: "IX_QuizQuestions_CategoryId_Level",
                table: "QuizQuestions");

            migrationBuilder.DropIndex(
                name: "IX_DocumentBooks_CategoryId",
                table: "DocumentBooks");

            migrationBuilder.DropIndex(
                name: "IX_CatalogDocuments_CategoryId",
                table: "CatalogDocuments");

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionKey", "RoleId" },
                keyValues: new object[] { "ManageCategories", new Guid("20000000-0000-4000-8000-000000000002") });

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "TechInsights");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "QuizQuestions");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "DocumentBooks");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "CatalogDocuments");

            migrationBuilder.RenameColumn(
                name: "ContextLabel",
                table: "TermExplanationCaches",
                newName: "Category");

            migrationBuilder.AddColumn<int>(
                name: "Category",
                table: "TechInsights",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Category",
                table: "QuizQuestions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "DocumentBooks",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "CatalogDocuments",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_TechInsights_Category_IsPublished",
                table: "TechInsights",
                columns: new[] { "Category", "IsPublished" });

            migrationBuilder.CreateIndex(
                name: "IX_QuizQuestions_Category_Level",
                table: "QuizQuestions",
                columns: new[] { "Category", "Level" });
        }
    }
}
