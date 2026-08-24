using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using OnlineSupermarket.Domain.Identity;
using OnlineSupermarket.Infrastructure.Identity;
using OnlineSupermarket.Infrastructure.Persistence;

namespace OnlineSupermarket.Infrastructure.Services;

public interface IPasswordResetService
{
    Task<(string RawToken, string TokenHash)> GenerateResetTokenAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> ConfirmResetAsync(string token, string newPasswordHash, CancellationToken cancellationToken = default);
}

public sealed class PasswordResetService : IPasswordResetService
{
    private readonly AppDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;

    public PasswordResetService(AppDbContext dbContext, IPasswordHasher passwordHasher)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
    }

    public async Task<(string RawToken, string TokenHash)> GenerateResetTokenAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var rawToken = GenerateSecureToken();
        var tokenHash = HashToken(rawToken);
        var expiresAt = TimeSpan.FromHours(1);

        var passwordResetToken = PasswordResetToken.Issue(userId, tokenHash, expiresAt);
        _dbContext.PasswordResetTokens.Add(passwordResetToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return (rawToken, tokenHash);
    }

    public async Task<(bool Success, string? Error)> ConfirmResetAsync(
        string token,
        string newPasswordHash,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return (false, "Token is required.");
        }

        if (string.IsNullOrWhiteSpace(newPasswordHash))
        {
            return (false, "New password is required.");
        }

        var tokenHash = HashToken(token);

        // Step 1: check token validity (read-only projection, no locking)
        var tokenRecord = await _dbContext.PasswordResetTokens
            .Where(t => t.TokenHash == tokenHash)
            .OrderByDescending(t => t.CreatedAtUtc)
            .Select(t => new { t.Id, t.IsUsed, t.ExpiresAtUtc, t.UserId })
            .FirstOrDefaultAsync(cancellationToken);

        if (tokenRecord == null)
        {
            return (false, "Invalid token.");
        }

        if (tokenRecord.IsUsed)
        {
            return (false, "Token has already been used.");
        }

        if (DateTime.UtcNow >= tokenRecord.ExpiresAtUtc)
        {
            return (false, "Token has expired.");
        }

        var supportsExecuteDelete = _dbContext.Database.ProviderName != "Microsoft.EntityFrameworkCore.InMemory";

        if (supportsExecuteDelete)
        {
            // Relational path: both consume + password update in one transaction
            var strategy = _dbContext.Database.CreateExecutionStrategy();
            var result = (Success: false, Error: (string?)null);
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _dbContext.Database
                    .BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);

                // Step 2: atomic consume — conditional DELETE; concurrent requests race, only one wins
                var deleted = await _dbContext.PasswordResetTokens
                    .Where(t => t.Id == tokenRecord.Id && !t.IsUsed && t.ExpiresAtUtc > DateTime.UtcNow)
                    .ExecuteDeleteAsync(cancellationToken);

                if (deleted == 0)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    result = (false, "Token has already been used.");
                    return;
                }

                // Step 3: update password within the same transaction
                var user = await _dbContext.Users
                    .FirstOrDefaultAsync(u => u.Id == tokenRecord.UserId, cancellationToken);

                if (user == null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    result = (false, "Invalid token.");
                    return;
                }

                user.UpdatePassword(newPasswordHash);
                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                result = (true, null);
            });

            return result;
        }
        else
        {
            // In-memory fallback: single SaveChanges — EF change tracker provides atomicity
            var trackedToken = await _dbContext.PasswordResetTokens
                .FirstOrDefaultAsync(t => t.Id == tokenRecord.Id, cancellationToken);

            if (trackedToken == null || trackedToken.IsUsed || DateTime.UtcNow >= trackedToken.ExpiresAtUtc)
            {
                return (false, "Token has already been used.");
            }

            var user = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Id == tokenRecord.UserId, cancellationToken);

            if (user == null)
            {
                return (false, "Invalid token.");
            }

            trackedToken.MarkAsUsed();
            user.UpdatePassword(newPasswordHash);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return (true, null);
        }
    }

    private static string GenerateSecureToken()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(randomBytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    private static string HashToken(string rawToken)
    {
        var bytes = Encoding.UTF8.GetBytes(rawToken);
        var hashBytes = SHA256.HashData(bytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
