using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinancialOS.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStewardshipEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Goals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    Period = table.Column<string>(type: "TEXT", nullable: false),
                    StartDate = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    EndDate = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    TargetAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    Currency = table.Column<string>(type: "TEXT", nullable: false),
                    CategoryId = table.Column<Guid>(type: "TEXT", nullable: true),
                    AccountId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_Goals", x => x.Id));

            migrationBuilder.CreateTable(
                name: "Budgets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Period = table.Column<string>(type: "TEXT", nullable: false),
                    StartDate = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    EndDate = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    LimitAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    Currency = table.Column<string>(type: "TEXT", nullable: false),
                    CategoryId = table.Column<Guid>(type: "TEXT", nullable: true),
                    AccountId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_Budgets", x => x.Id));

            migrationBuilder.CreateIndex(
                name: "IX_Goal_StartDate",
                table: "Goals",
                column: "StartDate");

            migrationBuilder.CreateIndex(
                name: "IX_Goal_EndDate",
                table: "Goals",
                column: "EndDate");

            migrationBuilder.CreateIndex(
                name: "IX_Budget_StartDate",
                table: "Budgets",
                column: "StartDate");

            migrationBuilder.CreateIndex(
                name: "IX_Budget_EndDate",
                table: "Budgets",
                column: "EndDate");

            migrationBuilder.DropIndex(
                name: "IX_DuplicateCandidate_Confidence",
                table: "DuplicateCandidates");

            migrationBuilder.DropIndex(
                name: "IX_DuplicateCandidate_EvaluatedAtUtc",
                table: "DuplicateCandidates");

            migrationBuilder.DropIndex(
                name: "IX_DuplicateCandidate_MatchedRecordId",
                table: "DuplicateCandidates");

            migrationBuilder.DropIndex(
                name: "IX_DuplicateCandidate_RecordId",
                table: "DuplicateCandidates");

            migrationBuilder.CreateIndex(
                name: "IX_ImportJobs_InstitutionProfileId",
                table: "ImportJobs",
                column: "InstitutionProfileId");

            migrationBuilder.AddForeignKey(
                name: "FK_Records_ImportJobs_ImportJobId",
                table: "Records",
                column: "ImportJobId",
                principalTable: "ImportJobs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Records_ImportJobs_ImportJobId",
                table: "Records");

            migrationBuilder.DropIndex(
                name: "IX_ImportJobs_InstitutionProfileId",
                table: "ImportJobs");

            migrationBuilder.CreateIndex(
                name: "IX_DuplicateCandidate_Confidence",
                table: "DuplicateCandidates",
                column: "Confidence");

            migrationBuilder.CreateIndex(
                name: "IX_DuplicateCandidate_EvaluatedAtUtc",
                table: "DuplicateCandidates",
                column: "EvaluatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_DuplicateCandidate_MatchedRecordId",
                table: "DuplicateCandidates",
                column: "MatchedRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_DuplicateCandidate_RecordId",
                table: "DuplicateCandidates",
                column: "RecordId");

            migrationBuilder.DropTable(
                name: "Goals");

            migrationBuilder.DropTable(
                name: "Budgets");
        }
    }
}
