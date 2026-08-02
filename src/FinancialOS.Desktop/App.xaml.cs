using FinancialOS.Desktop.Configuration;
using FinancialOS.Desktop.Services;
using FinancialOS.Desktop.ViewModels;
using FinancialOS.Desktop.Views;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Windows;

namespace FinancialOS.Desktop;

public partial class App : Application
{
    private readonly IHost _host;

    public App()
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration(config =>
            {
                config.AddJsonFile("appsettings.json", optional: true);
            })
            .ConfigureServices((ctx, services) =>
            {
                services.Configure<ApiClientOptions>(
                    ctx.Configuration.GetSection(ApiClientOptions.SectionName));

                services.AddHttpClient<FinancialApiClient>();

                services.AddTransient<AccountsViewModel>();
                services.AddTransient<RecordsViewModel>();
                services.AddTransient<MainViewModel>();
                services.AddTransient<MainWindow>(sp =>
                {
                    var win = new MainWindow();
                    win.DataContext = sp.GetRequiredService<MainViewModel>();
                    return win;
                });
            })
            .Build();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        await _host.StartAsync();
        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        await _host.StopAsync();
        _host.Dispose();
        base.OnExit(e);
    }
}
