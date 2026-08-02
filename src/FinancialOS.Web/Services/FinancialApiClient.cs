using System.Net.Http.Headers;
using System.Net.Http.Json;
using FinancialOS.Core.Models;
using FinancialOS.Shared.Contracts;
using Microsoft.Extensions.Options;

namespace FinancialOS.Web.Services;

public sealed class FinancialApiClient
{
    private readonly HttpClient _httpClient;

    public FinancialApiClient(HttpClient httpClient, IOptions<ApiOptions> options)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(options.Value.BaseUrl);
    }

    public async Task<string?> GetHealthAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/health", cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public async Task<PagedResult<RecordResponse>> GetRecordsAsync(int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"/api/v1/records?page={page}&pageSize={pageSize}", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PagedResult<RecordResponse>>(cancellationToken: cancellationToken)
            ?? new PagedResult<RecordResponse>(Array.Empty<RecordResponse>(), page, pageSize, 0);
    }

    public async Task<PagedResult<ReferenceItemResponse>> GetAccountsAsync(int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"/api/v1/accounts?page={page}&pageSize={pageSize}", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PagedResult<ReferenceItemResponse>>(cancellationToken: cancellationToken)
            ?? new PagedResult<ReferenceItemResponse>(Array.Empty<ReferenceItemResponse>(), page, pageSize, 0);
    }

    public async Task<PagedResult<RuleItemResponse>> GetRulesAsync(int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"/api/v1/classification-rules?page={page}&pageSize={pageSize}", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PagedResult<RuleItemResponse>>(cancellationToken: cancellationToken)
            ?? new PagedResult<RuleItemResponse>(Array.Empty<RuleItemResponse>(), page, pageSize, 0);
    }

    public async Task<ImportResult> UploadEvidenceAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        using var form = new MultipartFormDataContent();
        await using var stream = file.OpenReadStream();
        var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(file.ContentType ?? "application/octet-stream");
        form.Add(fileContent, "file", file.FileName);

        var response = await _httpClient.PostAsync("/api/v1/evidence", form, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return new ImportResult(false, null, content);
        }

        var importResponse = await response.Content.ReadFromJsonAsync<EvidenceImportResponse>(cancellationToken: cancellationToken);
        return new ImportResult(true, importResponse, null);
    }

    public async Task<ExportResult> ExportAsync(ExportFormat format, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
    {
        var request = new ExportRequest
        {
            Format = format,
            StartDate = startDate,
            EndDate = endDate
        };

        var response = await _httpClient.PostAsJsonAsync("/api/v1/exports", request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            return new ExportResult(false, null, null, null, body);
        }

        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        var fileName = response.Content.Headers.ContentDisposition?.FileNameStar ?? response.Content.Headers.ContentDisposition?.FileName ?? $"export-{DateTime.UtcNow:yyyyMMddHHmmss}.{GetExtension(format)}";
        var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
        return new ExportResult(true, bytes, fileName, contentType, null);
    }

    private static string GetExtension(ExportFormat format) => format switch
    {
        ExportFormat.Json => "json",
        ExportFormat.Ynab4 => "csv",
        ExportFormat.Goodbudget => "csv",
        _ => "csv"
    };
}

public sealed record ImportResult(bool Success, EvidenceImportResponse? Data, string? Error);

public sealed record ExportResult(bool Success, byte[]? Data, string? FileName, string? ContentType, string? Error);

public sealed record EvidenceImportResponse(
    Guid EvidenceId,
    Guid ImportJobId,
    string Status,
    string ParserType,
    int ParsedTransactionCount,
    int FailedRowCount,
    IReadOnlyList<ImportRecordSummary> Records);

public sealed record ImportRecordSummary(
    Guid Id,
    string Date,
    decimal Amount,
    string Currency,
    string Description,
    string ClassificationStatus,
    decimal? ClassificationConfidence,
    string? ClassificationReasonCode);
