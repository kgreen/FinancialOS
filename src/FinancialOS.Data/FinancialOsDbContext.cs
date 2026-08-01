using FinancialOS.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace FinancialOS.Data;

public sealed class FinancialOsDbContext : DbContext
{
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureFinancialEvidence(modelBuilder);
        ConfigureFinancialRecord(modelBuilder);
        ConfigureFinancialAccount(modelBuilder);
        ConfigureCategory(modelBuilder);
        ConfigureMerchant(modelBuilder);
        ConfigureRule(modelBuilder);
        ConfigurePlanningScenario(modelBuilder);
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
                        .ToList());

            entity.HasIndex(item => item.CreatedAt)
                .HasDatabaseName("IX_PlanningScenario_CreatedAt");
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
