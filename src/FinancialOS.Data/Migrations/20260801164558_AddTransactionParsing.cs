using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinancialOS.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionParsing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClassificationReasonCode",
                table: "Records",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClassificationStatus",
                table: "Records",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalReferenceId",
                table: "Records",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ImportJobId",
                table: "Records",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RowIndex",
                table: "Records",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "InstitutionProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    ColumnMappings = table.Column<string>(type: "TEXT", nullable: false),
                    AmountLayout = table.Column<string>(type: "TEXT", nullable: false),
                    DebitColumnName = table.Column<string>(type: "TEXT", nullable: true),
                    CreditColumnName = table.Column<string>(type: "TEXT", nullable: true),
                    DateFormatPattern = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InstitutionProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ImportJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EvidenceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    InstitutionProfileId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ParserType = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    TotalRows = table.Column<int>(type: "INTEGER", nullable: false),
                    ParsedCount = table.Column<int>(type: "INTEGER", nullable: false),
                    FailedRowCount = table.Column<int>(type: "INTEGER", nullable: false),
                    FailedRows = table.Column<string>(type: "TEXT", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImportJobs_Evidence_EvidenceId",
                        column: x => x.EvidenceId,
                        principalTable: "Evidence",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ImportJobs_InstitutionProfiles_InstitutionProfileId",
                        column: x => x.InstitutionProfileId,
                        principalTable: "InstitutionProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FinancialRecord_ExternalReferenceId",
                table: "Records",
                column: "ExternalReferenceId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialRecord_ImportJobId",
                table: "Records",
                column: "ImportJobId");

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

            migrationBuilder.CreateIndex(
                name: "IX_ImportJob_EvidenceId",
                table: "ImportJobs",
                column: "EvidenceId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportJob_Status",
                table: "ImportJobs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_InstitutionProfile_Name_Unique",
                table: "InstitutionProfiles",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImportJobs");

            migrationBuilder.DropTable(
                name: "InstitutionProfiles");

            migrationBuilder.DropIndex(
                name: "IX_FinancialRecord_ExternalReferenceId",
                table: "Records");

            migrationBuilder.DropIndex(
                name: "IX_FinancialRecord_ImportJobId",
                table: "Records");

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

            migrationBuilder.DropColumn(
                name: "ClassificationReasonCode",
                table: "Records");

            migrationBuilder.DropColumn(
                name: "ClassificationStatus",
                table: "Records");

            migrationBuilder.DropColumn(
                name: "ExternalReferenceId",
                table: "Records");

            migrationBuilder.DropColumn(
                name: "ImportJobId",
                table: "Records");

            migrationBuilder.DropColumn(
                name: "RowIndex",
                table: "Records");
        }
    }
}
