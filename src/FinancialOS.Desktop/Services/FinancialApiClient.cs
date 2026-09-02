using FinancialOS.Desktop.Configuration;
using FinancialOS.Core.Models;
using FinancialOS.Shared.Contracts;
using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Net.Http.Json;

namespace FinancialOS.Desktop.Services;

/// <summary>Typed HTTP client for the FinancialOS API.</summary>
public sealed class FinancialApiClient
{
    private readonly HttpClient _http;

    public FinancialApiClient(HttpClient http, IOptions<ApiClientOptions> options)
    {
        _http = http;
        _http.BaseAddress = new Uri(options.Value.BaseUrl.TrimEnd('/') + "/");
        _http.Timeout = TimeSpan.FromSeconds(options.Value.TimeoutSeconds);
    }

    public async Task<PagedResult<ReferenceItemResponse>> GetAccountsAsync(
        int page = 1, int pageSize = 25, CancellationToken ct = default)
    {
        var result = await _http.GetFromJsonAsync<PagedResult<ReferenceItemResponse>>(
            $"api/v1/accounts?page={page}&pageSize={pageSize}", ct);
        return result ?? new PagedResult<ReferenceItemResponse>([], page, pageSize, 0);
    }

    public async Task<PagedResult<RecordResponse>> GetRecordsAsync(
        int page = 1,
        int pageSize = 25,
        Guid? accountId = null,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        CancellationToken ct = default)
    {
        var qs = $"api/v1/records?page={page}&pageSize={pageSize}";
        if (accountId.HasValue)  qs += $"&accountId={accountId}";
        if (startDate.HasValue)  qs += $"&startDate={startDate:yyyy-MM-dd}";
        if (endDate.HasValue)    qs += $"&endDate={endDate:yyyy-MM-dd}";

        var result = await _http.GetFromJsonAsync<PagedResult<RecordResponse>>(qs, ct);
        return result ?? new PagedResult<RecordResponse>([], page, pageSize, 0);
    }
}
