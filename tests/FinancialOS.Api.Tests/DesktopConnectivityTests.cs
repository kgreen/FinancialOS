using System.Net;
using FinancialOS.Desktop.Configuration;
using FinancialOS.Desktop.Services;
using FinancialOS.Desktop.ViewModels;
using Microsoft.Extensions.Options;

namespace FinancialOS.Api.Tests;

/// <summary>
/// T061 — Desktop connectivity tests: 5xx responses surface as error messages on
/// ViewModels and do not propagate unhandled exceptions to the UI layer.
/// </summary>
public sealed class DesktopConnectivityTests
{
    private static FinancialApiClient BuildClient(HttpStatusCode statusCode)
    {
        var handler = new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(statusCode));

        var http = new HttpClient(handler);
        var options = Options.Create(new ApiClientOptions
        {
            BaseUrl        = "http://localhost:5000",
            TimeoutSeconds = 30
        });
        return new FinancialApiClient(http, options);
    }

    // ── FinancialApiClient error surfacing ────────────────────────────────────

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.BadGateway)]
    public async Task GetAccountsAsync_5xxResponse_ThrowsException(HttpStatusCode statusCode)
    {
        var client = BuildClient(statusCode);
        await Assert.ThrowsAnyAsync<Exception>(() => client.GetAccountsAsync());
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task GetRecordsAsync_5xxResponse_ThrowsException(HttpStatusCode statusCode)
    {
        var client = BuildClient(statusCode);
        await Assert.ThrowsAnyAsync<Exception>(() => client.GetRecordsAsync());
    }

    // ── AccountsViewModel error handling ─────────────────────────────────────

    [Fact]
    public async Task AccountsViewModel_WhenApiFails_SetsErrorMessageWithoutCrashing()
    {
        var client = BuildClient(HttpStatusCode.InternalServerError);
        var vm     = new AccountsViewModel(client);

        // Must not throw
        await vm.LoadAsync();

        Assert.NotNull(vm.ErrorMessage);
        Assert.False(string.IsNullOrWhiteSpace(vm.ErrorMessage));
        Assert.Empty(vm.Accounts);
    }

    [Fact]
    public async Task AccountsViewModel_WhenConnectionRefused_SetsErrorMessageWithoutCrashing()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            throw new HttpRequestException("Connection refused"));

        var http    = new HttpClient(handler);
        var options = Options.Create(new ApiClientOptions
        {
            BaseUrl        = "http://localhost:5000",
            TimeoutSeconds = 30
        });
        var client = new FinancialApiClient(http, options);
        var vm     = new AccountsViewModel(client);

        await vm.LoadAsync();

        Assert.NotNull(vm.ErrorMessage);
        Assert.Empty(vm.Accounts);
    }

    // ── No NullReferenceException propagates ─────────────────────────────────

    [Fact]
    public async Task AccountsViewModel_AfterError_IsNotInLoadingState()
    {
        var client = BuildClient(HttpStatusCode.InternalServerError);
        var vm     = new AccountsViewModel(client);

        await vm.LoadAsync();

        Assert.False(vm.IsLoading, "IsLoading must be reset to false after an error.");
    }
}
