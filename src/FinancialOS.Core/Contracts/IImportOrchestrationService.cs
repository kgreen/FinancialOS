using FinancialOS.Core.Models;

namespace FinancialOS.Core.Contracts;

public sealed record ImportOrchestrationResult(
    FinancialEvidence Evidence,
    ImportJob Job,
    IReadOnlyList<FinancialRecord> CreatedRecords,
    bool WasDuplicate
);

public interface IImportOrchestrationService
{
    Task<ImportOrchestrationResult> ImportAsync(
        string fileName,
        Stream fileStream,
        Guid? institutionProfileId,
        CancellationToken cancellationToken = default);
}
