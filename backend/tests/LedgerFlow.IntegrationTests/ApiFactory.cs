using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using LedgerFlow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;

namespace LedgerFlow.IntegrationTests;

/// <summary>
/// Hosts the real API against a throwaway SQL Server database that is migrated, seeded, and dropped per test run.
/// Set LEDGERFLOW_TEST_SQL to point at a different server (CI uses a SQL Server service container).
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string Password = "LedgerFlow!2026";

    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly Dictionary<string, HttpClient> _clients = [];
    private readonly string _connectionString;

    public ApiFactory()
    {
        var server = Environment.GetEnvironmentVariable("LEDGERFLOW_TEST_SQL")
            ?? "Server=localhost,1433;User Id=sa;Password=LedgerFlow!Dev2026;TrustServerCertificate=True;Encrypt=True";
        _connectionString = new SqlConnectionStringBuilder(server)
        {
            InitialCatalog = $"LedgerFlow_Tests_{Guid.NewGuid():N}",
        }.ConnectionString;
    }

    public HttpClient Clerk => _clients["clerk"];

    public HttpClient Approver => _clients["approver"];

    public HttpClient Controller => _clients["controller"];

    public async Task InitializeAsync()
    {
        foreach (var user in new[] { "clerk", "approver", "controller" })
        {
            _clients[user] = await SignInAsync($"{user}@ledgerflow.dev");
        }
    }

    public async Task<T> RunInScopeAsync<T>(Func<IServiceProvider, Task<T>> work)
    {
        ArgumentNullException.ThrowIfNull(work);
        await using var scope = Services.CreateAsyncScope();
        return await work(scope.ServiceProvider);
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await RunInScopeAsync(sp => sp.GetRequiredService<LedgerFlowDbContext>().Database.EnsureDeletedAsync());
        foreach (var client in _clients.Values)
        {
            client.Dispose();
        }

        await DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:LedgerFlow", _connectionString);
        builder.UseSetting("Database:MigrateOnStartup", "true");
        builder.UseSetting("Database:Seed", "true");
        builder.UseSetting("Hangfire:ServerEnabled", "false");
    }

    private async Task<HttpClient> SignInAsync(string email)
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password = Password });
        response.EnsureSuccessStatusCode();
        var token = await response.Content.ReadFromJsonAsync<TokenResponse>(Json);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!.AccessToken);
        return client;
    }

    private sealed record TokenResponse(string AccessToken);
}
