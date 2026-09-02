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
                name: "FK_ImportJobs_Evidence_EvidenceId",
                table: "ImportJobs",
                column: "EvidenceId",
                principalTable: "Evidence",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ImportJobs_InstitutionProfiles_InstitutionProfileId",
                table: "ImportJobs",
                column: "InstitutionProfileId",
                principalTable: "InstitutionProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

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
                name: "FK_ImportJobs_Evidence_EvidenceId",
                table: "ImportJobs");

            migrationBuilder.DropForeignKey(
                name: "FK_ImportJobs_InstitutionProfiles_InstitutionProfileId",
                table: "ImportJobs");

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
        }
    }
}
