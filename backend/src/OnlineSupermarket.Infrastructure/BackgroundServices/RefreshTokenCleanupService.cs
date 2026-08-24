using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using OnlineSupermarket.Infrastructure.Persistence;

namespace OnlineSupermarket.Infrastructure.BackgroundServices;

public sealed class RefreshTokenCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RefreshTokenCleanupService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromHours(1);
    private static readonly TimeSpan RevokedRetention = TimeSpan.FromDays(7);

    public RefreshTokenCleanupService(
        IServiceScopeFactory scopeFactory,
        ILogger<RefreshTokenCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RefreshTokenCleanupService started.");

        // Graceful startup: wait a bit for app/database to fully initialize
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("RefreshTokenCleanupService stopped during startup delay.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupAllAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error during refresh token cleanup. Will retry next interval.");
            }

            try
            {
                await Task.Delay(_interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("RefreshTokenCleanupService stopped.");
    }

    /// <summary>Calls both cleanup methods in sequence, matching production behavior.</summary>
    public async Task CleanupAllAsync(CancellationToken cancellationToken = default)
    {
        await CleanupExpiredTokensCoreAsync(cancellationToken);
        await CleanupOldRevokedTokensCoreAsync(cancellationToken);
    }

    // Single implementation — used by both ExecuteAsync (via CleanupAllAsync) and tests
    internal Task CleanupExpiredTokensCoreAsync(CancellationToken cancellationToken)
    {
        return CleanupExpiredTokensCoreImplAsync(cancellationToken);
    }

    internal Task CleanupOldRevokedTokensCoreAsync(CancellationToken cancellationToken)
    {
        return CleanupOldRevokedTokensCoreImplAsync(cancellationToken);
    }

    private async Task CleanupExpiredTokensCoreImplAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTime.UtcNow;
        var expiredTokens = await dbContext.RefreshTokens
            .Where(t => t.ExpiresAtUtc <= now && t.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);

        if (expiredTokens.Count > 0)
        {
            dbContext.RefreshTokens.RemoveRange(expiredTokens);
            await dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Cleaned up {Count} expired refresh tokens.", expiredTokens.Count);
        }
    }

    private async Task CleanupOldRevokedTokensCoreImplAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var cutoff = DateTime.UtcNow.Subtract(RevokedRetention);
        var oldRevokedTokens = await dbContext.RefreshTokens
            .Where(t => t.RevokedAtUtc != null && t.RevokedAtUtc < cutoff)
            .ToListAsync(cancellationToken);

        if (oldRevokedTokens.Count > 0)
        {
            dbContext.RefreshTokens.RemoveRange(oldRevokedTokens);
            await dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Cleaned up {Count} old revoked refresh tokens.", oldRevokedTokens.Count);
        }
    }
}
