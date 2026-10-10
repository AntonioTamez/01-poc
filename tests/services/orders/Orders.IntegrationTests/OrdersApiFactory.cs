using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Orders.Infrastructure;
using Testcontainers.PostgreSql;

namespace Orders.IntegrationTests;

/// <summary>Hosts the Orders API in memory against a disposable PostgreSQL container.</summary>
public sealed class OrdersApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine").Build();

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();
        await Services.EnsureOrdersDatabaseCreatedAsync();
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting(
            $"ConnectionStrings:{DependencyInjection.ConnectionStringName}",
            _postgres.GetConnectionString());
    }
}
