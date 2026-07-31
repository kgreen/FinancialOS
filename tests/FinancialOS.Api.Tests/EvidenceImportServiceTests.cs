using FinancialOS.Infrastructure.Import;

namespace FinancialOS.Api.Tests;

public sealed class EvidenceImportServiceTests
{
    [Fact]
    public async Task ImportAsync_PersistsEvidenceMetadata()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"financialos-{Guid.NewGuid():N}.csv");
        await File.WriteAllTextAsync(tempFile, "date,amount\n2026-01-01,12.34\n");

        await using var stream = File.OpenRead(tempFile);
        var service = new EvidenceImportService();

        var result = await service.ImportAsync("sample.csv", stream);

        Assert.Equal("sample.csv", result.Evidence.OriginalFileName);
        Assert.NotEmpty(result.Evidence.Sha256Hash);
        Assert.True(result.SizeBytes > 0);

        stream.Dispose();
        File.Delete(tempFile);
    }
}
