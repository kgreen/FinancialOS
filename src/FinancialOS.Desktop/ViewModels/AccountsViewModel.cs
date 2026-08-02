using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinancialOS.Desktop.Services;
using FinancialOS.Shared.Contracts;
using System.Collections.ObjectModel;

namespace FinancialOS.Desktop.ViewModels;

public sealed partial class AccountsViewModel : ObservableObject
{
    private readonly FinancialApiClient _api;

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _totalCount;

    public ObservableCollection<ReferenceItemResponse> Accounts { get; } = new();

    public AccountsViewModel(FinancialApiClient api)
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
            var result = await _api.GetAccountsAsync(page: CurrentPage, ct: ct);
            Accounts.Clear();
            foreach (var a in result.Items)
                Accounts.Add(a);
            TotalCount = result.TotalCount;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load accounts: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
