using FinancialOS.Core.Contracts;
using FinancialOS.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace FinancialOS.Data;

public sealed class EfFinancialRepository : IFinancialRepository
{
    private readonly FinancialOsDbContext _dbContext;

    public EfFinancialRepository(FinancialOsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<FinancialEvidence> AddEvidenceAsync(FinancialEvidence evidence, CancellationToken cancellationToken = default)
    {
        evidence.Id = evidence.Id == Guid.Empty ? Guid.NewGuid() : evidence.Id;
        _dbContext.Evidence.Add(evidence);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return evidence;
    }

    public async Task<FinancialEvidence?> GetEvidenceAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Evidence.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<FinancialEvidence>> ListEvidenceAsync(CancellationToken cancellationToken = default)
    {
        var evidence = await _dbContext.Evidence.AsNoTracking().ToListAsync(cancellationToken);
        return evidence.OrderByDescending(item => item.UploadedAt).ToList();
    }

    public async Task<FinancialRecord> AddRecordAsync(FinancialRecord record, CancellationToken cancellationToken = default)
    {
        record.Id = record.Id == Guid.Empty ? Guid.NewGuid() : record.Id;
        _dbContext.Records.Add(record);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return record;
    }

    public async Task<FinancialRecord?> GetRecordAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Records.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<FinancialRecord>> ListRecordsAsync(CancellationToken cancellationToken = default)
    {
        var records = await _dbContext.Records.AsNoTracking().ToListAsync(cancellationToken);
        return records.OrderByDescending(item => item.OccurredOn).ToList();
    }

    public async Task<FinancialRecord?> UpdateRecordAsync(FinancialRecord record, CancellationToken cancellationToken = default)
    {
        _dbContext.Records.Update(record);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return record;
    }

    public async Task<IReadOnlyList<FinancialAccount>> ListAccountsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Accounts.AsNoTracking().ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Category>> ListCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Categories.AsNoTracking().ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Merchant>> ListMerchantsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Merchants.AsNoTracking().ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Rule>> ListRulesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Rules.AsNoTracking().ToListAsync(cancellationToken);
    }

    public async Task<PlanningScenario> AddPlanningScenarioAsync(PlanningScenario scenario, CancellationToken cancellationToken = default)
    {
        scenario.Id = scenario.Id == Guid.Empty ? Guid.NewGuid() : scenario.Id;
        _dbContext.PlanningScenarios.Add(scenario);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return scenario;
    }

    public async Task<PlanningScenario?> GetPlanningScenarioAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.PlanningScenarios.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<PlanningScenario>> ListPlanningScenariosAsync(CancellationToken cancellationToken = default)
    {
        var scenarios = await _dbContext.PlanningScenarios.AsNoTracking().ToListAsync(cancellationToken);
        return scenarios.OrderByDescending(item => item.CreatedAt).ToList();
    }
}
