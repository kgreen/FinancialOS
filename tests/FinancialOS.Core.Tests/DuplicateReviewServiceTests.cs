using FinancialOS.Core.Knowledge.Deduplication;
using FinancialOS.Core.Knowledge.Provenance;
using FinancialOS.Core.Models;
using FinancialOS.Data;

namespace FinancialOS.Core.Tests;

public sealed class DuplicateReviewServiceTests
{
    [Fact]
    public async Task Evaluate_WhenNoOtherRecordsExist_ReturnsNull()
    {
        var repository = new InMemoryFinancialRepository();
        var service = new DuplicateReviewService(repository, new DuplicateScoringService(), new ProvenanceWriter(repository));
        var record = new FinancialRecord
        {
            Id = Guid.NewGuid(),
            AccountId = Guid.NewGuid(),
            Amount = new Money(25m, "USD"),
            OccurredOn = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            Description = "Solo transaction"
        };

        var result = await service.EvaluateAsync(record);

        Assert.Null(result);
    }
}
