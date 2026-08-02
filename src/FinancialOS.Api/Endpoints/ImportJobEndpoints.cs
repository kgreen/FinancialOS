using FinancialOS.Core.Contracts;
using FinancialOS.Core.Models;
using FinancialOS.Shared.Contracts;

namespace FinancialOS.Api.Endpoints;

public static class ImportJobEndpoints
{
    public static IEndpointRouteBuilder MapImportJobEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/import-jobs", ListImportJobs);
        app.MapGet("/api/v1/import-jobs/{id:guid}", GetImportJob);
        return app;
    }

    private static async Task<IResult> ListImportJobs(
        IFinancialRepository repository,
        CancellationToken cancellationToken)
    {
        var jobs = await repository.ListImportJobsAsync(cancellationToken);
        var items = jobs.Select(job => MapToResponse(job)).ToList();
        return Results.Ok(new PagedResult<ImportJobResponse>(items, 1, items.Count, items.Count));
    }

    private static async Task<IResult> GetImportJob(
        Guid id,
        IFinancialRepository repository,
        CancellationToken cancellationToken)
    {
        var job = await repository.GetImportJobAsync(id, cancellationToken);
        if (job is null)
        {
            return Results.Problem(
                detail: "Import job not found.",
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found",
                type: "https://tools.ietf.org/html/rfc9110#section-15.5.5",
                instance: $"/api/v1/import-jobs/{id}");
        }

        return Results.Ok(MapToResponse(job));
    }

    private static ImportJobResponse MapToResponse(ImportJob job)
    {
        var failedRows = job.FailedRows.Select(f => new FailedRowDto(f.RowIndex, f.Reason)).ToList();

        var parserType = job.ParserType.ToString() switch
        {
            "CsvConfigured" => "csvConfigured",
            "CsvAutoDetected" => "csvAutoDetected",
            "Ofx" => "ofx",
            _ => job.ParserType.ToString().ToLowerInvariant()
        };

        var status = job.Status.ToString() switch
        {
            "PartialSuccess" => "partialSuccess",
            var s => s.ToLowerInvariant()
        };

        return new ImportJobResponse(
            Id: job.Id,
            EvidenceId: job.EvidenceId,
            InstitutionProfileId: job.InstitutionProfileId,
            ParserType: parserType,
            Status: status,
            TotalRows: job.TotalRows,
            ParsedCount: job.ParsedCount,
            FailedRowCount: job.FailedRowCount,
            StartedAt: job.StartedAt,
            CompletedAt: job.CompletedAt,
            FailedRows: failedRows
        );
    }
}
