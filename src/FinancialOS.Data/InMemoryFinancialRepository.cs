using FinancialOS.Core.Contracts;
using FinancialOS.Core.Models;

namespace FinancialOS.Data;

public sealed class InMemoryFinancialRepository : IFinancialRepository
{
    private readonly Dictionary<Guid, FinancialEvidence> _evidence = new();
    private readonly Dictionary<Guid, FinancialRecord> _records = new();
    private readonly List<FinancialAccount> _accounts = new();
    private readonly List<Category> _categories = new();
    private readonly List<Merchant> _merchants = new();
    private readonly List<Rule> _rules = new();

    public InMemoryFinancialRepository()
    {
        _accounts.Add(new FinancialAccount { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Name = "Primary Checking", Currency = "USD" });
        _categories.Add(new Category { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Name = "Housing" });
        _merchants.Add(new Merchant { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), Name = "Contoso Market" });
        _rules.Add(new Rule { Id = Guid.Parse("44444444-4444-4444-4444-444444444444"), Name = "Default Merchant Rule", MatchExpression = "merchant contains market" });
    }

    public Task<FinancialEvidence> AddEvidenceAsync(FinancialEvidence evidence, CancellationToken cancellationToken = default)
    {
        evidence.Id = evidence.Id == Guid.Empty ? Guid.NewGuid() : evidence.Id;
        _evidence[evidence.Id] = evidence;
        return Task.FromResult(evidence);
    }

    public Task<FinancialEvidence?> GetEvidenceAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _evidence.TryGetValue(id, out var evidence);
        return Task.FromResult(evidence);
    }

    public Task<IReadOnlyList<FinancialEvidence>> ListEvidenceAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<FinancialEvidence>>(_evidence.Values.OrderByDescending(item => item.UploadedAt).ToList());
    }

    public Task<FinancialRecord> AddRecordAsync(FinancialRecord record, CancellationToken cancellationToken = default)
    {
        record.Id = record.Id == Guid.Empty ? Guid.NewGuid() : record.Id;
        _records[record.Id] = record;
        return Task.FromResult(record);
    }

    public Task<FinancialRecord?> GetRecordAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _records.TryGetValue(id, out var record);
        return Task.FromResult(record);
    }

    public Task<IReadOnlyList<FinancialRecord>> ListRecordsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<FinancialRecord>>(_records.Values.OrderByDescending(item => item.OccurredOn).ToList());
    }

    public Task<FinancialRecord?> UpdateRecordAsync(FinancialRecord record, CancellationToken cancellationToken = default)
    {
        record.Id = record.Id == Guid.Empty ? Guid.NewGuid() : record.Id;
        _records[record.Id] = record;
        return Task.FromResult<FinancialRecord?>(record);
    }

    public Task<IReadOnlyList<FinancialAccount>> ListAccountsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<FinancialAccount>>(_accounts.ToList());
    }

    public Task<IReadOnlyList<Category>> ListCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<Category>>(_categories.ToList());
    }

    public Task<IReadOnlyList<Merchant>> ListMerchantsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<Merchant>>(_merchants.ToList());
    }

    public Task<IReadOnlyList<Rule>> ListRulesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<Rule>>(_rules.ToList());
    }
}
