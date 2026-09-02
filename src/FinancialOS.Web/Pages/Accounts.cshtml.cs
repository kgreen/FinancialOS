using FinancialOS.Core.Models;
using FinancialOS.Shared.Contracts;
using FinancialOS.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FinancialOS.Web.Pages;

public sealed class AccountsModel : PageModel
{
    private readonly FinancialApiClient _apiClient;

    public AccountsModel(FinancialApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public int PageSize { get; set; } = 10;

    public PagedResult<ReferenceItemResponse> Accounts { get; private set; } = new(Array.Empty<ReferenceItemResponse>(), 1, 10, 0);

    public string? ApiErrorMessage { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        try
        {
            Accounts = await _apiClient.GetAccountsAsync(PageNumber, PageSize, cancellationToken);
        }
        catch (HttpRequestException)
        {
            ApiErrorMessage = "Unable to reach the FinancialOS API. Start the API service or update the configured base URL.";
            Accounts = new PagedResult<ReferenceItemResponse>(Array.Empty<ReferenceItemResponse>(), PageNumber, PageSize, 0);
        }
    }
}
