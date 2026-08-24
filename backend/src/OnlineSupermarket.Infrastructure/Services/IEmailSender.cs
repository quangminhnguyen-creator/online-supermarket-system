using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace OnlineSupermarket.Infrastructure.Services;

public interface IEmailSender
{
    Task SendPasswordResetEmailAsync(string email, string resetUrl, CancellationToken cancellationToken = default);
}

public sealed class DevEmailSender : IEmailSender
{
    private readonly ILogger<DevEmailSender> _logger;

    public DevEmailSender(ILogger<DevEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendPasswordResetEmailAsync(string email, string resetUrl, CancellationToken cancellationToken = default)
    {
        // In production: integrate with real email provider (SendGrid, AWS SES, etc.)
        // Store in singleton for development inspection — tokens never logged
        DevEmailStore.Instance.Add(email, resetUrl);
        _logger.LogInformation("Password reset email queued for {Email}. Access via DevEmailStore.Instance.", email);
        return Task.CompletedTask;
    }
}

/// <summary>In-memory email store for development: captures emails thread-safe with bounded retention.
/// Tokens are stored but NEVER logged. Use singleton instance for debugging.</summary>
public sealed class DevEmailStore
{
    private const int MaxCapacity = 100;
    private const int MaxAgeHours = 24;

    public static DevEmailStore Instance { get; } = new();

    private readonly ConcurrentQueue<DevEmailEntry> _emails = new();

    private DevEmailStore() { }

    public void Add(string email, string resetUrl)
    {
        PurgeExpiredAndOverflow();
        _emails.Enqueue(new DevEmailEntry(email, resetUrl, DateTime.UtcNow));
    }

    public IReadOnlyList<DevEmailEntry> GetAll()
    {
        PurgeExpiredAndOverflow();
        return _emails.ToArray();
    }

    public void Clear()
    {
        while (_emails.TryDequeue(out _)) { }
    }

    public int Count => _emails.Count;

    private void PurgeExpiredAndOverflow()
    {
        var cutoff = DateTime.UtcNow.AddHours(-MaxAgeHours);
        var entries = _emails.ToArray();
        var valid = entries
            .Where(e => e.CapturedAtUtc > cutoff)
            .TakeLast(MaxCapacity)
            .Reverse()
            .ToArray();

        Clear();
        foreach (var entry in valid.Reverse())
        {
            _emails.Enqueue(entry);
        }
    }
}

public sealed record DevEmailEntry(string Email, string ResetUrl, DateTime CapturedAtUtc);

/// <summary>In-memory sender for integration tests: captures emails in a thread-safe list.
/// Tokens are stored in CapturedEmails but NEVER logged.</summary>
public sealed class InMemoryCapturingEmailSender : IEmailSender
{
    private readonly object _lock = new();
    private readonly List<CapturedEmail> _capturedEmails = new();

    public IReadOnlyList<CapturedEmail> CapturedEmails
    {
        get
        {
            lock (_lock)
            {
                return _capturedEmails.ToList();
            }
        }
    }

    public Task SendPasswordResetEmailAsync(string email, string resetUrl, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _capturedEmails.Add(new CapturedEmail(email, resetUrl));
        }
        return Task.CompletedTask;
    }

    public void Clear() => _capturedEmails.Clear();
}

public sealed record CapturedEmail(string Email, string ResetUrl);

/// <summary>No-op email sender for production: does nothing. Tokens are NEVER logged.</summary>
public sealed class NoOpEmailSender : IEmailSender
{
    public Task SendPasswordResetEmailAsync(string email, string resetUrl, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
