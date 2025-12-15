using Integrations.Data.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Respawn;
using Testcontainers.MsSql;
using Xunit;

[CollectionDefinition("Database collection")]
public class IntegrationTestFactory
    : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _dbContainer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .WithPassword("exampleStrong(!)Password")   // SQL Server requires strong pw
        .Build();

    public HttpClient Client { get; private set; } = null!;
    public AppDbContext DbContext { get; private set; } = null!;
    private Respawner _respawner = null!;
    public DbContextOptions<AppDbContext> DbOptions { get; private set; } = null!;


    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();

        using var scope = Services.CreateScope();
        DbOptions = scope.ServiceProvider.GetRequiredService<DbContextOptions<AppDbContext>>();
        DbContext = new AppDbContext(DbOptions);


        await DbContext.Database.EnsureCreatedAsync();

        using (var conn = new System.Data.SqlClient.SqlConnection(_dbContainer.GetConnectionString()))
        {
            await conn.OpenAsync();

            _respawner = await Respawner.CreateAsync(conn, new RespawnerOptions
            {
                DbAdapter = DbAdapter.SqlServer,
            });
        }
    }

    public async Task DisposeAsync()
    {
        await _dbContainer.StopAsync();
        await _dbContainer.DisposeAsync();
    }

    public AppDbContext CreateDbContext()
    {
        var scope = Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<AppDbContext>();
    }


    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Remove old DbContext
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>)
            );
            if (descriptor != null)
                services.Remove(descriptor);

            // Add SQL Server DbContext
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(_dbContainer.GetConnectionString()));
        });

        builder.UseEnvironment("Testing");
    }

    public async Task ResetDatabaseAsync()
    {
        using var conn = new System.Data.SqlClient.SqlConnection(_dbContainer.GetConnectionString());
        await conn.OpenAsync();
        await _respawner.ResetAsync(conn);
    }
}

[CollectionDefinition(nameof(DatabaseTestCollection))]
public class DatabaseTestCollection : ICollectionFixture<IntegrationTestFactory> { }
