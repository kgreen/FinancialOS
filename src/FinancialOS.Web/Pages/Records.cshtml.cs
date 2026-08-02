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

    public PagedResult<RecordResponse> Records { get; private set; } = new(Array.Empty<RecordResponse>(), 1, 10, 0);

    public string? ApiErrorMessage { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        try
        {
            Records = await _apiClient.GetRecordsAsync(PageNumber, PageSize, cancellationToken);
        }
        catch (HttpRequestException)
        {
            ApiErrorMessage = "Unable to reach the FinancialOS API. Start the API service or update the configured base URL.";
            Records = new PagedResult<RecordResponse>(Array.Empty<RecordResponse>(), PageNumber, PageSize, 0);
        }
    }
}
