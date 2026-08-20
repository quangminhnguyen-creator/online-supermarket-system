using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using OnlineSupermarket.Infrastructure.Persistence;

namespace OnlineSupermarket.Api.Tests.Auth;

public sealed class AuthTestApiFactory : WebApplicationFactory<Program>
{
    private const string VariableName = "ConnectionStrings__DefaultConnection";
    private readonly string? _originalConnectionString;
    private readonly string _dbName = Guid.NewGuid().ToString();
    private readonly InMemoryDatabaseRoot _databaseRoot = new();

    public AuthTestApiFactory()
    {
        _originalConnectionString = Environment.GetEnvironmentVariable(VariableName);
        Environment.SetEnvironmentVariable(
            VariableName,
            "Server=localhost;Port=3306;Database=test;User=test;Password=test");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:DefaultConnection", "Server=localhost;Port=3306;Database=test;User=test;Password=test");

        builder.ConfigureServices(services =>
        {
            var efServices = services.Where(d =>
                d.ServiceType.Namespace?.StartsWith("Microsoft.EntityFrameworkCore") == true ||
                d.ServiceType.Namespace?.StartsWith("MySql.EntityFrameworkCore") == true ||
                d.ServiceType == typeof(AppDbContext) ||
                d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                d.ServiceType == typeof(DbContextOptions)).ToList();

            foreach (var descriptor in efServices)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseInMemoryDatabase(_dbName, _databaseRoot);
                options.ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        try
        {
            base.Dispose(disposing);
        }
        finally
        {
            Environment.SetEnvironmentVariable(VariableName, _originalConnectionString);
        }
    }
}
