using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace FinancialOS.Desktop.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableObject? _currentView;

    private readonly AccountsViewModel _accountsViewModel;
    private readonly RecordsViewModel  _recordsViewModel;

    public MainViewModel(AccountsViewModel accountsViewModel, RecordsViewModel recordsViewModel)
    {
        _accountsViewModel = accountsViewModel;
        _recordsViewModel  = recordsViewModel;
        _currentView       = accountsViewModel;
    }

    [RelayCommand]
    private void ShowAccounts() => CurrentView = _accountsViewModel;

    [RelayCommand]
    private void ShowRecords() => CurrentView = _recordsViewModel;
}
