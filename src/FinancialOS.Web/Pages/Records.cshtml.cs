using FinancialOS.Core.Models;
using FinancialOS.Shared.Contracts;
using FinancialOS.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FinancialOS.Web.Pages;

public sealed class RecordsModel : PageModel
{
    private readonly FinancialApiClient _apiClient;

    public RecordsModel(FinancialApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public int PageSize { get; set; } = 10;

    [BindProperty(SupportsGet = true)]
    public string? StartDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? EndDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Merchant { get; set; }

    [BindProperty(SupportsGet = true)]
    public decimal? MinAmount { get; set; }

    [BindProperty(SupportsGet = true)]
    public decimal? MaxAmount { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SortBy { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool SortDescending { get; set; } = true;

    public PagedResult<RecordResponse> Records { get; private set; } = new(Array.Empty<RecordResponse>(), 1, 10, 0);

    public string? ApiErrorMessage { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        try
        {
            var startDate = DateOnly.TryParse(StartDate, out var sd) ? sd : (DateOnly?)null;
            var endDate   = DateOnly.TryParse(EndDate,   out var ed) ? ed : (DateOnly?)null;

            Records = await _apiClient.GetRecordsAsync(
                page:           PageNumber,
                pageSize:       PageSize,
                startDate:      startDate,
                endDate:        endDate,
                merchant:       Merchant,
                minAmount:      MinAmount,
                maxAmount:      MaxAmount,
                sortBy:         SortBy,
                sortDescending: SortDescending,
                cancellationToken: cancellationToken);
        }
        catch (HttpRequestException)
        {
            ApiErrorMessage = "Unable to reach the FinancialOS API. Start the API service or update the configured base URL.";
            Records = new PagedResult<RecordResponse>(Array.Empty<RecordResponse>(), PageNumber, PageSize, 0);
        }
    }
}
