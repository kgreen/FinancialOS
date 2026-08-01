using FinancialOS.Infrastructure.Import;
using FinancialOS.Data;

namespace FinancialOS.Api.Tests;

public sealed class EvidenceImportServiceTests
{
    [Fact]
    public async Task ImportAsync_PersistsEvidenceMetadata()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"financialos-{Guid.NewGuid():N}.csv");
        await File.WriteAllTextAsync(tempFile, "date,amount\n2026-01-01,12.34\n");

        await using var stream = File.OpenRead(tempFile);
        var service = new EvidenceImportService(new InMemoryFinancialRepository());

        var result = await service.ImportAsync("sample.csv", stream);

        Assert.Equal("sample.csv", result.Evidence.OriginalFileName);
        Assert.NotEmpty(result.Evidence.Sha256Hash);
        Assert.True(result.SizeBytes > 0);
        Assert.False(result.WasDuplicate);
        Assert.Null(result.ExistingImportJob);

        stream.Dispose();
        File.Delete(tempFile);
    }

    [Fact]
    public async Task ImportAsync_RejectsWhitespaceOnlyFile()
    {
        await using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("  \r\n\t  "));
        var service = new EvidenceImportService(new InMemoryFinancialRepository());

        var ex = await Assert.ThrowsAsync<EvidenceImportValidationException>(() => service.ImportAsync("blank.csv", stream));

        Assert.Equal(400, ex.StatusCode);
    }
}
