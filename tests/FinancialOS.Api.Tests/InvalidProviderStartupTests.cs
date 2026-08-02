using Microsoft.AspNetCore.Mvc.Testing;

namespace FinancialOS.Api.Tests;

/// <summary>
/// T073 — Verifies that an unrecognised DatabaseProvider value causes a clean startup failure
/// with a useful <see cref="InvalidOperationException"/> message.
/// T074 — Verifies that an absent DatabaseProvider key defaults to sqlite and starts successfully.
/// </summary>
public sealed class InvalidProviderStartupTests
{
    /// <summary>
    /// T073 — Setting DatabaseProvider to an unknown value must throw
    /// <see cref="InvalidOperationException"/> at startup whose message names the bad value.
    /// </summary>
    [Fact]
    public void Startup_WithUnrecognisedProvider_ThrowsInvalidOperationException()
    {
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(host =>
        {
            host.UseSetting("DatabaseProvider", "invalidvalue");
            host.UseSetting("ConnectionStrings:Sqlite", ":memory:");
        });

        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            // Triggering Services forces the host to build, which calls AddConfiguredDatabase.
            _ = factory.Services;
        });

        Assert.Contains("DatabaseProvider", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("invalidvalue", ex.Message, StringComparison.OrdinalIgnoreCase);

        factory.Dispose();
    }

    /// <summary>
    /// T074 — When DatabaseProvider is absent from config the application must default to
    /// sqlite and start without error.
    /// </summary>
    [Fact]
    public async Task Startup_WithMissingProviderKey_DefaultsToSqliteAndSucceeds()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"financialos-missing-provider-{Guid.NewGuid():N}.db");

        try
        {
            var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(host =>
            {
                // Explicitly omit DatabaseProvider key; only supply the connection string.
                host.UseSetting("ConnectionStrings:Sqlite", dbPath);
            });

            // Should not throw.
            var client = factory.CreateClient();
            var response = await client.GetAsync("/health");
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

            factory.Dispose();
        }
        finally
        {
            try { if (File.Exists(dbPath)) File.Delete(dbPath); } catch (IOException) { /* file still held by OS; GC will release */ }
        }
    }
}
