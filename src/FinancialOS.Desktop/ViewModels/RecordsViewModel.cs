using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinancialOS.Desktop.Services;
using FinancialOS.Shared.Contracts;
using System.Collections.ObjectModel;

namespace FinancialOS.Desktop.ViewModels;

public sealed partial class RecordsViewModel : ObservableObject
{
    private readonly FinancialApiClient _api;

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private Guid? _accountId;
    [ObservableProperty] private DateOnly? _startDate;
    [ObservableProperty] private DateOnly? _endDate;

    public ObservableCollection<RecordResponse> Records { get; } = new();

    public RecordsViewModel(FinancialApiClient api)
    {
        _api = api;
    }

    [RelayCommand]
    public async Task LoadAsync(CancellationToken ct = default)
    {
        IsLoading    = true;
        ErrorMessage = null;
        try
        {
            var result = await _api.GetRecordsAsync(
                page: CurrentPage,
                accountId: AccountId,
                startDate: StartDate,
                endDate: EndDate,
                ct: ct);

            Records.Clear();
            foreach (var r in result.Items)
                Records.Add(r);
            TotalCount = result.TotalCount;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load records: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
