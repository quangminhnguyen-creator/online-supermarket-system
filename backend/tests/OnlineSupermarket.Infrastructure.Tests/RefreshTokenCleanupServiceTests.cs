using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using OnlineSupermarket.Domain.Identity;
using OnlineSupermarket.Infrastructure.BackgroundServices;
using OnlineSupermarket.Infrastructure.Persistence;

namespace OnlineSupermarket.Infrastructure.Tests;

public class RefreshTokenCleanupServiceTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly string _dbName = Guid.NewGuid().ToString();

    public RefreshTokenCleanupServiceTests()
    {
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(opts =>
            opts.UseInMemoryDatabase(_dbName));
        services.AddLogging();

        _serviceProvider = services.BuildServiceProvider();
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
    }

    private static RefreshToken CreateToken(Guid userId, byte[] hashInput, DateTime expiresAtUtc)
    {
        var tokenHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(hashInput));
        var ctor = typeof(RefreshToken).GetConstructor(
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
            null,
            new[] { typeof(Guid), typeof(Guid), typeof(string), typeof(DateTime), typeof(DateTime) },
            null)!;

        return (RefreshToken)ctor.Invoke(new object[]
        {
            Guid.NewGuid(), userId, tokenHash, expiresAtUtc, DateTime.UtcNow
        });
    }

    private RefreshTokenCleanupService CreateService()
    {
        return new RefreshTokenCleanupService(
            _serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            _serviceProvider.GetRequiredService<ILogger<RefreshTokenCleanupService>>());
    }

    [Fact]
    public async Task CleanupAll_RemovesExpiredActiveTokens()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var activeToken = CreateToken(userId, [1], now.AddDays(7));
        var expiredToken = CreateToken(userId, [2], now.AddDays(-2));
        var oldRevokedToken = CreateToken(userId, [3], now.AddDays(7));
        oldRevokedToken.Revoke(now.AddDays(-30));

        dbContext.RefreshTokens.AddRange(activeToken, expiredToken, oldRevokedToken);
        await dbContext.SaveChangesAsync();

        // Act — call the production cleanup (uses its own scope internally)
        var service = CreateService();
        await service.CleanupExpiredTokensCoreAsync(CancellationToken.None);

        // Assert via the test's own scoped context
        var remaining = await dbContext.RefreshTokens.ToListAsync();
        Assert.Equal(2, remaining.Count);
        Assert.Contains(remaining, t => t.Id == activeToken.Id);
        Assert.Contains(remaining, t => t.Id == oldRevokedToken.Id);
        Assert.DoesNotContain(remaining, t => t.Id == expiredToken.Id);
    }

    [Fact]
    public async Task CleanupAll_RemovesOldRevokedTokens()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var oldRevokedToken = CreateToken(userId, [1], now.AddDays(7));
        oldRevokedToken.Revoke(now.AddDays(-30));
        var recentRevokedToken = CreateToken(userId, [2], now.AddDays(7));
        recentRevokedToken.Revoke(now.AddDays(-1));
        var activeToken = CreateToken(userId, [3], now.AddDays(7));

        dbContext.RefreshTokens.AddRange(oldRevokedToken, recentRevokedToken, activeToken);
        await dbContext.SaveChangesAsync();

        // Act — call the production cleanup (uses its own scope internally)
        var service = CreateService();
        await service.CleanupOldRevokedTokensCoreAsync(CancellationToken.None);

        // Assert via the test's own scoped context
        var remaining = await dbContext.RefreshTokens.ToListAsync();
        Assert.Equal(2, remaining.Count);
        Assert.Contains(remaining, t => t.Id == recentRevokedToken.Id);
        Assert.Contains(remaining, t => t.Id == activeToken.Id);
        Assert.DoesNotContain(remaining, t => t.Id == oldRevokedToken.Id);
    }
}
