using Microsoft.EntityFrameworkCore;
using OnlineSupermarket.Infrastructure.Persistence;
using Testcontainers.MySql;
using Xunit;

namespace OnlineSupermarket.Infrastructure.Tests.Persistence;

public class MySqlFixture : IAsyncLifetime
{
    private readonly MySqlContainer _mySqlContainer = new MySqlBuilder()
        .WithImage("mysql:8.4")
        .Build();

    public string ConnectionString => _mySqlContainer.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _mySqlContainer.StartAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL(ConnectionString)
            .Options;

        using var dbContext = new AppDbContext(options);
        await dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _mySqlContainer.DisposeAsync();
    }
}
