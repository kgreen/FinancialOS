using FinancialOS.Web.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FinancialOS.Web.Pages;

public sealed class IndexModel : PageModel
{
    private readonly FinancialApiClient _apiClient;

    public IndexModel(FinancialApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public DashboardSummary Summary { get; private set; } = new("Checking connection…", 0, 0, 0);

    public string? ApiErrorMessage { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        try
        {
            var health = await _apiClient.GetHealthAsync(cancellationToken);
            var accounts = await _apiClient.GetAccountsAsync(pageSize: 1, cancellationToken: cancellationToken);
            var records = await _apiClient.GetRecordsAsync(pageSize: 1, cancellationToken: cancellationToken);
            var rules = await _apiClient.GetRulesAsync(pageSize: 1, cancellationToken: cancellationToken);

            Summary = new DashboardSummary(
                health ?? "API unavailable",
                accounts.TotalCount,
                records.TotalCount,
                rules.TotalCount);
        }
        catch (HttpRequestException)
        {
            ApiErrorMessage = "The FinancialOS API is currently unavailable. Start the API or update the Api:BaseUrl setting to the running service.";
            Summary = new DashboardSummary("API unavailable", 0, 0, 0);
        }
    }
}

public sealed record DashboardSummary(string HealthStatus, int AccountCount, int RecordCount, int RuleCount);

