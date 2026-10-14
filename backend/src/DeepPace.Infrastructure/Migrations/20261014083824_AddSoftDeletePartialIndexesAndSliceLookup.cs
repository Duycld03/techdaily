using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeepPace.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSoftDeletePartialIndexesAndSliceLookup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_Email",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_UserQuizProgresses_UserId_QuestionId",
                table: "UserQuizProgresses");

            migrationBuilder.DropIndex(
                name: "IX_UserPushSubscriptions_UserId_Endpoint",
                table: "UserPushSubscriptions");

            migrationBuilder.DropIndex(
                name: "IX_UserInsightBookmarks_UserId_InsightId",
                table: "UserInsightBookmarks");

            migrationBuilder.DropIndex(
                name: "IX_UserBookPacers_UserId_DocumentBookId",
                table: "UserBookPacers");

            migrationBuilder.DropIndex(
                name: "IX_TechInsights_Slug",
                table: "TechInsights");

            migrationBuilder.DropIndex(
                name: "IX_DocumentChunks_DocumentBookId",
                table: "DocumentChunks");

            migrationBuilder.DropIndex(
                name: "IX_DocumentBooks_Slug",
                table: "DocumentBooks");

            migrationBuilder.DropIndex(
                name: "IX_DailyDrills_UserId_ScheduledDate_QuestionId",
                table: "DailyDrills");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_UserQuizProgresses_UserId_QuestionId",
                table: "UserQuizProgresses",
                columns: new[] { "UserId", "QuestionId" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_UserPushSubscriptions_UserId_Endpoint",
                table: "UserPushSubscriptions",
                columns: new[] { "UserId", "Endpoint" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_UserInsightBookmarks_UserId_InsightId",
                table: "UserInsightBookmarks",
                columns: new[] { "UserId", "InsightId" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_UserBookPacers_UserId_DocumentBookId",
                table: "UserBookPacers",
                columns: new[] { "UserId", "DocumentBookId" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_TechInsights_Slug",
                table: "TechInsights",
                column: "Slug",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentChunks_DocumentBookId_ChunkOrder",
                table: "DocumentChunks",
                columns: new[] { "DocumentBookId", "ChunkOrder" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentBooks_Slug",
                table: "DocumentBooks",
                column: "Slug",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_DailyDrills_UserId_ScheduledDate_QuestionId",
                table: "DailyDrills",
                columns: new[] { "UserId", "ScheduledDate", "QuestionId" },
                unique: true,
                filter: "\"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_Email",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_UserQuizProgresses_UserId_QuestionId",
                table: "UserQuizProgresses");

            migrationBuilder.DropIndex(
                name: "IX_UserPushSubscriptions_UserId_Endpoint",
                table: "UserPushSubscriptions");

            migrationBuilder.DropIndex(
                name: "IX_UserInsightBookmarks_UserId_InsightId",
                table: "UserInsightBookmarks");

            migrationBuilder.DropIndex(
                name: "IX_UserBookPacers_UserId_DocumentBookId",
                table: "UserBookPacers");

            migrationBuilder.DropIndex(
                name: "IX_TechInsights_Slug",
                table: "TechInsights");

            migrationBuilder.DropIndex(
                name: "IX_DocumentChunks_DocumentBookId_ChunkOrder",
                table: "DocumentChunks");

            migrationBuilder.DropIndex(
                name: "IX_DocumentBooks_Slug",
                table: "DocumentBooks");

            migrationBuilder.DropIndex(
                name: "IX_DailyDrills_UserId_ScheduledDate_QuestionId",
                table: "DailyDrills");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserQuizProgresses_UserId_QuestionId",
                table: "UserQuizProgresses",
                columns: new[] { "UserId", "QuestionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserPushSubscriptions_UserId_Endpoint",
                table: "UserPushSubscriptions",
                columns: new[] { "UserId", "Endpoint" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserInsightBookmarks_UserId_InsightId",
                table: "UserInsightBookmarks",
                columns: new[] { "UserId", "InsightId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserBookPacers_UserId_DocumentBookId",
                table: "UserBookPacers",
                columns: new[] { "UserId", "DocumentBookId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TechInsights_Slug",
                table: "TechInsights",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocumentChunks_DocumentBookId",
                table: "DocumentChunks",
                column: "DocumentBookId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentBooks_Slug",
                table: "DocumentBooks",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DailyDrills_UserId_ScheduledDate_QuestionId",
                table: "DailyDrills",
                columns: new[] { "UserId", "ScheduledDate", "QuestionId" },
                unique: true);
        }
    }
}
