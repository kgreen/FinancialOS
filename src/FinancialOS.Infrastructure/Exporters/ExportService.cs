using FinancialOS.Core.Contracts;
using FinancialOS.Core.Models;
using System.IO.Pipelines;

namespace FinancialOS.Infrastructure.Exporters;

/// <summary>
/// Orchestrates export generation. Streams records from the repository through the
/// appropriate IRecordExporter and returns an ExportSnapshot ready to read.
/// </summary>
public sealed class ExportService : IExportService
{
    private readonly IFinancialRepository _repository;
    private readonly IEnumerable<IRecordExporter> _exporters;

    public ExportService(IFinancialRepository repository, IEnumerable<IRecordExporter> exporters)
    {
        _repository = repository;
        _exporters  = exporters;
    }

    public Task<ExportSnapshot> ExportAsync(
        ExportRequest request,
        CancellationToken cancellationToken = default)
    {
        var exporter = _exporters.FirstOrDefault(e => e.Format == request.Format)
            ?? throw new InvalidOperationException($"No exporter registered for format '{request.Format}'.");

        var filter  = request.ToFilterCriteria();
        var records = _repository.StreamRecordsAsync(filter, cancellationToken);

        var pipe = new Pipe();
        _ = WriteExportAsync(pipe, exporter, records, cancellationToken);

        return Task.FromResult(new ExportSnapshot
        {
            Content     = pipe.Reader.AsStream(),
            FileName    = BuildFileName(request),
            ContentType = exporter.ContentType,
            Format      = request.Format,
            GeneratedAt = DateTimeOffset.UtcNow,
            RecordCount = 0,
        });
    }

    private static async Task WriteExportAsync(
        Pipe pipe,
        IRecordExporter exporter,
        IAsyncEnumerable<FinancialRecord> records,
        CancellationToken cancellationToken)
    {
        Exception? error = null;
        try
        {
            await exporter.WriteAsync(records, pipe.Writer.AsStream(leaveOpen: true), cancellationToken);
        }
        catch (Exception exception)
        {
            error = exception;
        }
        finally
        {
            await pipe.Writer.CompleteAsync(error);
        }
    }

    private static string BuildFileName(ExportRequest request)
    {
        var range = $"{request.StartDate:yyyy-MM-dd}_{request.EndDate:yyyy-MM-dd}";
        return request.Format switch
        {
            ExportFormat.Csv        => $"financialos-export-{range}.csv",
            ExportFormat.Json       => $"financialos-export-{range}.json",
            ExportFormat.Ynab4      => $"financialos-export-{range}-ynab4.csv",
            ExportFormat.Goodbudget => $"financialos-export-{range}-goodbudget.csv",
            _ => $"financialos-export-{range}.bin",
        };
    }
}
