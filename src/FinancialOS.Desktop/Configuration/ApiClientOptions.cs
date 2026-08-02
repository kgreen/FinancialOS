namespace FinancialOS.Desktop.Configuration;

/// <summary>Strongly-typed options for the FinancialOS API HTTP client.</summary>
public sealed class ApiClientOptions
{
    public const string SectionName = "ApiClient";

    /// <summary>Base URL of the FinancialOS API (e.g. "http://localhost:5000").</summary>
    public string BaseUrl { get; set; } = "http://localhost:5000";

    /// <summary>HTTP request timeout in seconds.</summary>
    public int TimeoutSeconds { get; set; } = 30;
}
