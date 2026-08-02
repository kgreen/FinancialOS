using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace FinancialOS.Desktop.ViewModels;

public sealed partial class ErrorViewModel : ObservableObject
{
    [ObservableProperty] private string _errorTitle = "Connection Error";
    [ObservableProperty] private string _errorMessage = string.Empty;

    private readonly Func<Task> _retryAction;

    public ErrorViewModel(Func<Task> retryAction)
    {
        _retryAction = retryAction;
    }

    [RelayCommand]
    private async Task RetryAsync() => await _retryAction();
}
