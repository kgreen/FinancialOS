using FinancialOS.Core.Models;

namespace FinancialOS.Core.Contracts;

/// <summary>Orchestrates export generation across all supported formats.</summary>
public interface IExportService
{
    /// <summary>
    /// Streams all matching records through the chosen format exporter and returns an
    /// <see cref="ExportSnapshot"/> whose <c>Content</c> stream is ready to read.
    /// The caller must dispose the snapshot Content stream after writing the HTTP response.
    /// </summary>
    Task<ExportSnapshot> ExportAsync(ExportRequest request, CancellationToken cancellationToken = default);
}
