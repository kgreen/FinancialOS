using System.ComponentModel.DataAnnotations;
using FinancialOS.Core.Models;
using FinancialOS.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FinancialOS.Web.Pages;

public sealed class TestingModel : PageModel
{
    private readonly FinancialApiClient _apiClient;

    public TestingModel(FinancialApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [BindProperty]
    [Required]
    public IFormFile? EvidenceFile { get; set; }

    [BindProperty]
    public string ExportFormat { get; set; } = "Csv";

    [BindProperty]
    public DateTime StartDate { get; set; } = DateTime.Today.AddMonths(-1);

    [BindProperty]
    public DateTime EndDate { get; set; } = DateTime.Today;

    public string? ImportMessage { get; private set; }

    public string? ExportMessage { get; private set; }

    public string? ExportFileName { get; private set; }

    public async Task<IActionResult> OnPostImportAsync(CancellationToken cancellationToken)
    {
        if (EvidenceFile is null || EvidenceFile.Length == 0)
        {
            ImportMessage = "Please choose a file to upload.";
            return Page();
        }

        try
        {
            var result = await _apiClient.UploadEvidenceAsync(EvidenceFile, cancellationToken);
            if (!result.Success)
            {
                ImportMessage = result.Error ?? "The import request failed.";
                return Page();
            }

            ImportMessage = $"Import completed with status {result.Data?.Status}. Parsed transactions: {result.Data?.ParsedTransactionCount}.";
            return Page();
        }
        catch (HttpRequestException)
        {
            ImportMessage = "The FinancialOS API is unavailable. Start the API service or update the configured base URL.";
            return Page();
        }
    }

    public async Task<IActionResult> OnPostExportAsync(CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<ExportFormat>(ExportFormat, out var format))
        {
            ExportMessage = "Unsupported export format.";
            return Page();
        }

        try
        {
            var result = await _apiClient.ExportAsync(format, DateOnly.FromDateTime(StartDate), DateOnly.FromDateTime(EndDate), cancellationToken);
            if (!result.Success)
            {
                ExportMessage = result.Error ?? "The export request failed.";
                return Page();
            }

            ExportFileName = result.FileName;
            ExportMessage = $"Export ready for download ({result.FileName}).";
            return File(result.Data ?? Array.Empty<byte>(), "application/octet-stream", result.FileName ?? "export.csv");
        }
        catch (HttpRequestException)
        {
            ExportMessage = "The FinancialOS API is unavailable. Start the API service or update the configured base URL.";
            return Page();
        }
    }
}
