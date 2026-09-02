using FinancialOS.Core.Models;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace FinancialOS.Data;

public sealed class FinancialOsDbContext : DbContext
{
    private static readonly ValueComparer<List<string>> StringListComparer = new(
        (left, right) => left != null && right != null && left.SequenceEqual(right),
        list => list == null ? 0 : list.Aggregate(0, (hash, item) => HashCode.Combine(hash, item.GetHashCode())),
        list => list == null ? new List<string>() : list.ToList());

    private static readonly ValueComparer<List<Guid>> GuidListComparer = new(
        (left, right) => left != null && right != null && left.SequenceEqual(right),
        list => list == null ? 0 : list.Aggregate(0, (hash, item) => HashCode.Combine(hash, item.GetHashCode())),
        list => list == null ? new List<Guid>() : list.ToList());

    public FinancialOsDbContext(DbContextOptions<FinancialOsDbContext> options) : base(options)
    {
    }

    public DbSet<FinancialEvidence> Evidence => Set<FinancialEvidence>();
    public DbSet<FinancialRecord> Records => Set<FinancialRecord>();
    public DbSet<FinancialAccount> Accounts => Set<FinancialAccount>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Merchant> Merchants => Set<Merchant>();
    public DbSet<Rule> Rules => Set<Rule>();
    public DbSet<PlanningScenario> PlanningScenarios => Set<PlanningScenario>();
    public DbSet<ClassificationRule> ClassificationRules => Set<ClassificationRule>();
    public DbSet<CanonicalMerchant> CanonicalMerchants => Set<CanonicalMerchant>();
    public DbSet<MerchantAliasMap> MerchantAliases => Set<MerchantAliasMap>();
    public DbSet<NormalizationDecision> NormalizationDecisions => Set<NormalizationDecision>();
    public DbSet<DuplicateCandidate> DuplicateCandidates => Set<DuplicateCandidate>();
    public DbSet<ProvenanceEntry> ProvenanceEntries => Set<ProvenanceEntry>();
    public DbSet<InstitutionProfile> InstitutionProfiles => Set<InstitutionProfile>();
    public DbSet<ImportJob> ImportJobs => Set<ImportJob>();
    public DbSet<Goal> Goals => Set<Goal>();
    public DbSet<Budget> Budgets => Set<Budget>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureFinancialEvidence(modelBuilder);
        ConfigureFinancialRecord(modelBuilder);
        ConfigureFinancialAccount(modelBuilder);
        ConfigureCategory(modelBuilder);
        ConfigureMerchant(modelBuilder);
        ConfigureRule(modelBuilder);
        ConfigurePlanningScenario(modelBuilder);
        ConfigureClassificationRule(modelBuilder);
        ConfigureCanonicalMerchant(modelBuilder);
        ConfigureMerchantAlias(modelBuilder);
        ConfigureNormalizationDecision(modelBuilder);
        ConfigureDuplicateCandidate(modelBuilder);
        ConfigureProvenanceEntry(modelBuilder);
        ConfigureInstitutionProfile(modelBuilder);
        ConfigureImportJob(modelBuilder);
        ConfigureGoal(modelBuilder);
        ConfigureBudget(modelBuilder);
        ConfigureGlobalBehaviors(modelBuilder);
    }

    private static void ConfigureFinancialEvidence(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FinancialEvidence>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.Property(item => item.SourceType).HasConversion<string>();
            entity.Property(item => item.OriginalFileName).IsRequired();
            entity.Property(item => item.StoragePath).IsRequired();
            entity.Property(item => item.Sha256Hash).IsRequired();

            entity.HasIndex(item => item.Sha256Hash)
                .IsUnique()
                .HasDatabaseName("IX_FinancialEvidence_Sha256Hash_Unique");

            entity.HasIndex(item => item.UploadedAt)
                .HasDatabaseName("IX_FinancialEvidence_UploadedAt");
        });
    }

    private static void ConfigureFinancialRecord(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FinancialRecord>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Status).HasConversion<string>();

            entity.OwnsOne(item => item.Amount, owned =>
            {
                owned.Property(money => money.Amount).HasColumnName("Amount");
                owned.Property(money => money.Currency).HasColumnName("Currency");
            });

            entity.OwnsOne(item => item.ClassificationConfidence, owned =>
            {
                owned.Property(confidence => confidence.Score).HasColumnName("ClassificationConfidence");
            });

            entity.OwnsOne(item => item.Provenance, owned =>
            {
                owned.Property(provenance => provenance.Source).HasColumnName("ProvenanceSource");
                owned.Property(provenance => provenance.RuleName).HasColumnName("ProvenanceRuleName");
                owned.Property(provenance => provenance.AlgorithmVersion).HasColumnName("ProvenanceAlgorithmVersion");
            });

            entity.HasIndex(item => item.AccountId)
                .HasDatabaseName("IX_FinancialRecord_AccountId");

            entity.HasIndex(item => item.EvidenceId)
                .HasDatabaseName("IX_FinancialRecord_EvidenceId");

            entity.HasIndex(item => item.Status)
                .HasDatabaseName("IX_FinancialRecord_Status");

            entity.HasIndex(item => item.OccurredOn)
                .HasDatabaseName("IX_FinancialRecord_OccurredOn");

            entity.HasIndex(item => new { item.AccountId, item.Status })
                .HasDatabaseName("IX_FinancialRecord_AccountId_Status");

            // spec 003 new columns
            entity.Property(item => item.ClassificationStatus)
                .HasConversion<string>()
                .HasColumnName("ClassificationStatus");

            entity.HasIndex(item => item.ImportJobId)
                .HasDatabaseName("IX_FinancialRecord_ImportJobId");

            entity.HasIndex(item => item.ExternalReferenceId)
                .HasDatabaseName("IX_FinancialRecord_ExternalReferenceId");

            entity.HasOne<ImportJob>().WithMany()
                .HasForeignKey(item => item.ImportJobId)
                .IsRequired(false);
        });
    }

    private static void ConfigureFinancialAccount(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FinancialAccount>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Name).IsRequired();
            entity.Property(item => item.Currency).IsRequired();

            entity.HasIndex(item => item.Name)
                .HasDatabaseName("IX_FinancialAccount_Name");
        });
    }

    private static void ConfigureCategory(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Name).IsRequired();

            entity.HasIndex(item => item.Name)
                .IsUnique()
                .HasDatabaseName("IX_Category_Name_Unique");
        });
    }

    private static void ConfigureMerchant(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Merchant>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Name).IsRequired();

            entity.HasIndex(item => item.Name)
                .HasDatabaseName("IX_Merchant_Name");
        });
    }

    private static void ConfigureRule(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Rule>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Name).IsRequired();
            entity.Property(item => item.MatchExpression).IsRequired();

            entity.HasIndex(item => item.Name)
                .IsUnique()
                .HasDatabaseName("IX_Rule_Name_Unique");
        });
    }

    private static void ConfigurePlanningScenario(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PlanningScenario>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Name).IsRequired();
            entity.Property(item => item.Currency).IsRequired();

            entity.Property(item => item.RelatedRecordIds)
                .HasConversion(
                    list => string.Join(',', list),
                    value => value.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(Guid.Parse)
                        .ToList())
                .Metadata.SetValueComparer(GuidListComparer);

            entity.HasIndex(item => item.CreatedAt)
                .HasDatabaseName("IX_PlanningScenario_CreatedAt");
        });
    }

    private static void ConfigureClassificationRule(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ClassificationRule>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Name).IsRequired();
            entity.Property(item => item.Status).HasConversion<string>();
            entity.Property(item => item.Scope).HasConversion<string>();
            entity.Property(item => item.ConditionJson).IsRequired();
            entity.Property(item => item.UpdatedAtUtc).IsRequired();

            entity.HasIndex(item => item.Name)
                .IsUnique()
                .HasDatabaseName("IX_ClassificationRule_Name_Unique");
            entity.HasIndex(item => item.Priority)
                .HasDatabaseName("IX_ClassificationRule_Priority");
            entity.HasIndex(item => item.Status)
                .HasDatabaseName("IX_ClassificationRule_Status");
        });
    }

    private static void ConfigureCanonicalMerchant(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CanonicalMerchant>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.Property(item => item.DisplayName).IsRequired();
            entity.Property(item => item.NormalizedKey).IsRequired();

            entity.HasIndex(item => item.NormalizedKey)
                .IsUnique()
                .HasDatabaseName("IX_CanonicalMerchant_NormalizedKey_Unique");
        });
    }

    private static void ConfigureMerchantAlias(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MerchantAliasMap>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.Property(item => item.AliasRawText).IsRequired();
            entity.Property(item => item.AliasNormalizedText).IsRequired();
            entity.Property(item => item.MatchStrategy).HasConversion<string>();

            entity.HasIndex(item => new { item.CanonicalMerchantId, item.AliasNormalizedText })
                .HasDatabaseName("IX_MerchantAlias_Canonical_AliasNormalized");
            entity.HasIndex(item => item.IsActive)
                .HasDatabaseName("IX_MerchantAlias_IsActive");
        });
    }

    private static void ConfigureNormalizationDecision(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<NormalizationDecision>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Status).HasConversion<string>();
            entity.Property(item => item.ReasonCodes)
                .HasConversion(
                    list => JsonSerializer.Serialize(list, JsonSerializerOptions.Default),
                    value => JsonSerializer.Deserialize<List<string>>(value, JsonSerializerOptions.Default) ?? new List<string>())
                .Metadata.SetValueComparer(StringListComparer);

            entity.HasIndex(item => item.FinancialRecordId)
                .HasDatabaseName("IX_NormalizationDecision_RecordId");
            entity.HasIndex(item => item.CreatedAtUtc)
                .HasDatabaseName("IX_NormalizationDecision_CreatedAtUtc");
        });
    }

    private static void ConfigureDuplicateCandidate(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DuplicateCandidate>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.Property(item => item.CandidateGroupKey).IsRequired();
            entity.Property(item => item.Status).HasConversion<string>();
            entity.Property(item => item.SignalSnapshotJson).IsRequired();
            entity.Property(item => item.ReasonCodes)
                .HasConversion(
                    list => JsonSerializer.Serialize(list, JsonSerializerOptions.Default),
                    value => JsonSerializer.Deserialize<List<string>>(value, JsonSerializerOptions.Default) ?? new List<string>())
                .Metadata.SetValueComparer(StringListComparer);

            entity.HasIndex(item => item.CandidateGroupKey)
                .HasDatabaseName("IX_DuplicateCandidate_GroupKey");
            entity.HasIndex(item => item.Status)
                .HasDatabaseName("IX_DuplicateCandidate_Status");
            entity.HasIndex(item => new { item.Status, item.Confidence, item.EvaluatedAtUtc })
                .HasDatabaseName("IX_DuplicateCandidate_Status_Confidence_EvaluatedAtUtc");
            entity.HasIndex(item => new { item.RecordId, item.MatchedRecordId })
                .HasDatabaseName("IX_DuplicateCandidate_Record_MatchedRecord");
        });
    }

    private static void ConfigureProvenanceEntry(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProvenanceEntry>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.Property(item => item.StepType).HasConversion<string>();
            entity.Property(item => item.Source).HasConversion<string>();
            entity.Property(item => item.SourceReference).IsRequired();
            entity.Property(item => item.DecisionSummary).IsRequired();
            entity.Property(item => item.ReasonCodes)
                .HasConversion(
                    list => JsonSerializer.Serialize(list, JsonSerializerOptions.Default),
                    value => JsonSerializer.Deserialize<List<string>>(value, JsonSerializerOptions.Default) ?? new List<string>())
                .Metadata.SetValueComparer(StringListComparer);

            entity.HasIndex(item => new { item.FinancialRecordId, item.StepSequence })
                .IsUnique()
                .HasDatabaseName("IX_ProvenanceEntry_Record_StepSequence_Unique");
            entity.HasIndex(item => item.CorrelationId)
                .HasDatabaseName("IX_ProvenanceEntry_CorrelationId");
        });
    }

    private static readonly ValueComparer<Dictionary<string, string>> DictionaryComparer = new(
        (left, right) => left != null && right != null && left.Count == right.Count && left.All(kv => right.ContainsKey(kv.Key) && right[kv.Key] == kv.Value),
        dict => dict == null ? 0 : dict.Aggregate(0, (hash, kv) => HashCode.Combine(hash, kv.Key.GetHashCode(), kv.Value == null ? 0 : kv.Value.GetHashCode())),
        dict => dict == null ? new Dictionary<string, string>() : new Dictionary<string, string>(dict));

    private static readonly ValueComparer<List<FailedRowEntry>> FailedRowListComparer = new(
        (left, right) => left != null && right != null && left.Count == right.Count && left.Zip(right).All(pair => pair.First.RowIndex == pair.Second.RowIndex && pair.First.Reason == pair.Second.Reason),
        list => list == null ? 0 : list.Aggregate(0, (hash, item) => HashCode.Combine(hash, item.RowIndex, item.Reason.GetHashCode())),
        list => list == null ? new List<FailedRowEntry>() : list.ToList());

    private static void ConfigureInstitutionProfile(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InstitutionProfile>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.AmountLayout).HasConversion<string>();

            entity.Property(e => e.ColumnMappings)
                .HasConversion(
                    dict => JsonSerializer.Serialize(dict, JsonSerializerOptions.Default),
                    json => JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonSerializerOptions.Default) ?? new())
                .Metadata.SetValueComparer(DictionaryComparer);

            entity.HasIndex(e => e.Name)
                .IsUnique()
                .HasDatabaseName("IX_InstitutionProfile_Name_Unique");

            entity.HasQueryFilter(e => !e.IsDeleted);
        });
    }

    private static void ConfigureImportJob(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ImportJob>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ParserType).HasConversion<string>();
            entity.Property(e => e.Status).HasConversion<string>();

            entity.Property(e => e.FailedRows)
                .HasConversion(
                    list => JsonSerializer.Serialize(list, JsonSerializerOptions.Default),
                    json => JsonSerializer.Deserialize<List<FailedRowEntry>>(json, JsonSerializerOptions.Default) ?? new())
                .Metadata.SetValueComparer(FailedRowListComparer);

            entity.HasIndex(e => e.EvidenceId)
                .HasDatabaseName("IX_ImportJob_EvidenceId");

            entity.HasIndex(e => e.Status)
                .HasDatabaseName("IX_ImportJob_Status");

            entity.HasOne<FinancialEvidence>().WithMany()
                .HasForeignKey(e => e.EvidenceId);

            entity.HasOne<InstitutionProfile>().WithMany()
                .HasForeignKey(e => e.InstitutionProfileId)
                .IsRequired(false);
        });
    }

    private static void ConfigureGoal(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Goal>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Name).IsRequired();
            entity.Property(item => item.Type).HasConversion<string>();
            entity.Property(item => item.Period).HasConversion<string>();
            entity.Property(item => item.Currency).IsRequired();
            entity.HasIndex(item => item.StartDate).HasDatabaseName("IX_Goal_StartDate");
            entity.HasIndex(item => item.EndDate).HasDatabaseName("IX_Goal_EndDate");
        });
    }

    private static void ConfigureBudget(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Budget>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Name).IsRequired();
            entity.Property(item => item.Period).HasConversion<string>();
            entity.Property(item => item.Currency).IsRequired();
            entity.HasIndex(item => item.StartDate).HasDatabaseName("IX_Budget_StartDate");
            entity.HasIndex(item => item.EndDate).HasDatabaseName("IX_Budget_EndDate");
        });
    }

    private static void ConfigureGlobalBehaviors(ModelBuilder modelBuilder)
    {
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var relationship in entity.GetForeignKeys())
            {
                // Required FKs cascade (e.g. FinancialRecord → Evidence).
                // Optional FKs restrict to prevent accidental deletion of audit/provenance data;
                // normal soft-delete patterns handle removal at the application layer.
                relationship.DeleteBehavior = relationship.IsRequired
                    ? DeleteBehavior.Cascade
                    : DeleteBehavior.Restrict;
            }
        }
    }
}
