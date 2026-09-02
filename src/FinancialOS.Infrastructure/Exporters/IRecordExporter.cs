using FinancialOS.Core.Models;

namespace FinancialOS.Infrastructure.Exporters;

/// <summary>Strategy interface for a single export format writer.</summary>
public interface IRecordExporter
{
    ExportFormat Format { get; }

    /// <summary>MIME content-type produced by this exporter (e.g. "text/csv; charset=utf-8").</summary>
    string ContentType { get; }

    /// <summary>Suggested file extension including the leading dot (e.g. ".csv").</summary>
    string FileExtension { get; }

    /// <summary>
    /// Consumes <paramref name="records"/> and writes the formatted output to
    /// <paramref name="destination"/>. The caller owns the stream lifetime.
    /// </summary>
    Task WriteAsync(
        IAsyncEnumerable<FinancialRecord> records,
        Stream destination,
        CancellationToken cancellationToken = default);
}
