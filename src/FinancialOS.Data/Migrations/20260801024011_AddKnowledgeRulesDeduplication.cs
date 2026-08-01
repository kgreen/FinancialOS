using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinancialOS.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddKnowledgeRulesDeduplication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if (ActiveProvider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
            {
                migrationBuilder.Sql("""
                    DELETE FROM "Rules"
                    WHERE rowid NOT IN (
                        SELECT MIN(rowid)
                        FROM "Rules"
                        GROUP BY "Name"
                    );
                    """);
                migrationBuilder.Sql("""
                    DELETE FROM "Evidence"
                    WHERE rowid NOT IN (
                        SELECT MIN(rowid)
                        FROM "Evidence"
                        GROUP BY "Sha256Hash"
                    );
                    """);
                migrationBuilder.Sql("""
                    DELETE FROM "Categories"
                    WHERE rowid NOT IN (
                        SELECT MIN(rowid)
                        FROM "Categories"
                        GROUP BY "Name"
                    );
                    """);
            }
            else if (ActiveProvider.Contains("Npgsql", StringComparison.OrdinalIgnoreCase)
                || ActiveProvider.Contains("PostgreSQL", StringComparison.OrdinalIgnoreCase))
            {
                migrationBuilder.Sql("""
                    DELETE FROM "Rules" r
                    USING "Rules" r2
                    WHERE r."Name" = r2."Name"
                      AND r.ctid > r2.ctid;
                    """);
                migrationBuilder.Sql("""
                    DELETE FROM "Evidence" e
                    USING "Evidence" e2
                    WHERE e."Sha256Hash" = e2."Sha256Hash"
                      AND e.ctid > e2.ctid;
                    """);
                migrationBuilder.Sql("""
                    DELETE FROM "Categories" c
                    USING "Categories" c2
                    WHERE c."Name" = c2."Name"
                      AND c.ctid > c2.ctid;
                    """);
            }

            migrationBuilder.CreateTable(
                name: "CanonicalMerchants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", nullable: false),
                    NormalizedKey = table.Column<string>(type: "TEXT", nullable: false),
                    DefaultCategoryId = table.Column<Guid>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CanonicalMerchants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClassificationRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    Scope = table.Column<string>(type: "TEXT", nullable: false),
                    ScopeReferenceId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ConditionJson = table.Column<string>(type: "TEXT", nullable: false),
                    TargetMerchantId = table.Column<Guid>(type: "TEXT", nullable: true),
                    TargetCategoryId = table.Column<Guid>(type: "TEXT", nullable: true),
                    EffectiveFromUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    EffectiveToUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassificationRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DuplicateCandidates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CandidateGroupKey = table.Column<string>(type: "TEXT", nullable: false),
                    RecordId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MatchedRecordId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Confidence = table.Column<decimal>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    ReasonCodes = table.Column<string>(type: "TEXT", nullable: false),
                    SignalSnapshotJson = table.Column<string>(type: "TEXT", nullable: false),
                    EvaluatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ReviewedByUserId = table.Column<string>(type: "TEXT", nullable: true),
                    ReviewedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DuplicateCandidates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MerchantAliases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CanonicalMerchantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AliasRawText = table.Column<string>(type: "TEXT", nullable: false),
                    AliasNormalizedText = table.Column<string>(type: "TEXT", nullable: false),
                    MatchStrategy = table.Column<string>(type: "TEXT", nullable: false),
                    ConfidenceWeight = table.Column<decimal>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MerchantAliases", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NormalizationDecisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FinancialRecordId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CanonicalMerchantId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CategoryId = table.Column<Guid>(type: "TEXT", nullable: true),
                    RuleId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Confidence = table.Column<decimal>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    ReasonCodes = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    SupersededByDecisionId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NormalizationDecisions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProvenanceEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FinancialRecordId = table.Column<Guid>(type: "TEXT", nullable: false),
                    StepType = table.Column<string>(type: "TEXT", nullable: false),
                    StepSequence = table.Column<long>(type: "INTEGER", nullable: false),
                    Source = table.Column<string>(type: "TEXT", nullable: false),
                    SourceReference = table.Column<string>(type: "TEXT", nullable: false),
                    Confidence = table.Column<decimal>(type: "TEXT", nullable: true),
                    DecisionSummary = table.Column<string>(type: "TEXT", nullable: false),
                    ReasonCodes = table.Column<string>(type: "TEXT", nullable: false),
                    ActorId = table.Column<string>(type: "TEXT", nullable: true),
                    CorrelationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProvenanceEntries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Rule_Name_Unique",
                table: "Rules",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinancialRecord_AccountId",
                table: "Records",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialRecord_AccountId_Status",
                table: "Records",
                columns: new[] { "AccountId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_FinancialRecord_EvidenceId",
                table: "Records",
                column: "EvidenceId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialRecord_OccurredOn",
                table: "Records",
                column: "OccurredOn");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialRecord_Status",
                table: "Records",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PlanningScenario_CreatedAt",
                table: "PlanningScenarios",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Merchant_Name",
                table: "Merchants",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialEvidence_Sha256Hash_Unique",
                table: "Evidence",
                column: "Sha256Hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinancialEvidence_UploadedAt",
                table: "Evidence",
                column: "UploadedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Category_Name_Unique",
                table: "Categories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinancialAccount_Name",
                table: "Accounts",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_CanonicalMerchant_NormalizedKey_Unique",
                table: "CanonicalMerchants",
                column: "NormalizedKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClassificationRule_Name_Unique",
                table: "ClassificationRules",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClassificationRule_Priority",
                table: "ClassificationRules",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_ClassificationRule_Status",
                table: "ClassificationRules",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_DuplicateCandidate_GroupKey",
                table: "DuplicateCandidates",
                column: "CandidateGroupKey");

            migrationBuilder.CreateIndex(
                name: "IX_DuplicateCandidate_Status",
                table: "DuplicateCandidates",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_DuplicateCandidate_Record_MatchedRecord",
                table: "DuplicateCandidates",
                columns: new[] { "RecordId", "MatchedRecordId" });

            migrationBuilder.CreateIndex(
                name: "IX_DuplicateCandidate_Status_Confidence_EvaluatedAtUtc",
                table: "DuplicateCandidates",
                columns: new[] { "Status", "Confidence", "EvaluatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_MerchantAlias_Canonical_AliasNormalized",
                table: "MerchantAliases",
                columns: new[] { "CanonicalMerchantId", "AliasNormalizedText" });

            migrationBuilder.CreateIndex(
                name: "IX_MerchantAlias_IsActive",
                table: "MerchantAliases",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_NormalizationDecision_CreatedAtUtc",
                table: "NormalizationDecisions",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_NormalizationDecision_RecordId",
                table: "NormalizationDecisions",
                column: "FinancialRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_ProvenanceEntry_CorrelationId",
                table: "ProvenanceEntries",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_ProvenanceEntry_Record_StepSequence_Unique",
                table: "ProvenanceEntries",
                columns: new[] { "FinancialRecordId", "StepSequence" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CanonicalMerchants");

            migrationBuilder.DropTable(
                name: "ClassificationRules");

            migrationBuilder.DropTable(
                name: "DuplicateCandidates");

            migrationBuilder.DropTable(
                name: "MerchantAliases");

            migrationBuilder.DropTable(
                name: "NormalizationDecisions");

            migrationBuilder.DropTable(
                name: "ProvenanceEntries");

            migrationBuilder.DropIndex(
                name: "IX_Rule_Name_Unique",
                table: "Rules");

            migrationBuilder.DropIndex(
                name: "IX_FinancialRecord_AccountId",
                table: "Records");

            migrationBuilder.DropIndex(
                name: "IX_FinancialRecord_AccountId_Status",
                table: "Records");

            migrationBuilder.DropIndex(
                name: "IX_FinancialRecord_EvidenceId",
                table: "Records");

            migrationBuilder.DropIndex(
                name: "IX_FinancialRecord_OccurredOn",
                table: "Records");

            migrationBuilder.DropIndex(
                name: "IX_FinancialRecord_Status",
                table: "Records");

            migrationBuilder.DropIndex(
                name: "IX_PlanningScenario_CreatedAt",
                table: "PlanningScenarios");

            migrationBuilder.DropIndex(
                name: "IX_Merchant_Name",
                table: "Merchants");

            migrationBuilder.DropIndex(
                name: "IX_FinancialEvidence_Sha256Hash_Unique",
                table: "Evidence");

            migrationBuilder.DropIndex(
                name: "IX_FinancialEvidence_UploadedAt",
                table: "Evidence");

            migrationBuilder.DropIndex(
                name: "IX_Category_Name_Unique",
                table: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_FinancialAccount_Name",
                table: "Accounts");
        }
    }
}
