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

    private static void ConfigureGlobalBehaviors(ModelBuilder modelBuilder)
    {
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            var deleteStrategy = DeleteBehavior.Cascade;
            foreach (var relationship in entity.GetForeignKeys())
            {
                relationship.DeleteBehavior = deleteStrategy;
            }
        }
    }
}
