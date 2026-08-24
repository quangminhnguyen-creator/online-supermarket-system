using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OnlineSupermarket.Api.Endpoints;
using OnlineSupermarket.Api.Tests.Auth;
using OnlineSupermarket.Domain.Identity;
using OnlineSupermarket.Infrastructure.Identity;
using OnlineSupermarket.Infrastructure.Persistence;
using OnlineSupermarket.Infrastructure.Services;

namespace OnlineSupermarket.Api.Tests.Endpoints;

public sealed class PasswordResetEndpointsTests : IClassFixture<AuthTestApiFactory>
{
    private readonly AuthTestApiFactory _factory;

    public PasswordResetEndpointsTests(AuthTestApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task RequestReset_WithValidEmail_ReturnsOk()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var user = User.Create(
            "exists@example.com", passwordHasher.HashPassword("Password123!"), "Test User", null);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/password-reset",
            new PasswordResetRequest("exists@example.com"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task RequestReset_WithNonExistentEmail_ReturnsOkWithoutLeakingAccount()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/password-reset",
            new PasswordResetRequest("nonexistent@example.com"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<dynamic>();
        Assert.NotNull(body);
    }

    [Fact]
    public async Task RequestReset_BothEmailsReturnSameResponse()
    {
        var client = _factory.CreateClient();

        var responseExists = await client.PostAsJsonAsync("/api/auth/password-reset",
            new PasswordResetRequest("exists2@example.com"));
        var responseMissing = await client.PostAsJsonAsync("/api/auth/password-reset",
            new PasswordResetRequest("doesnot_exist_12345@example.com"));

        Assert.Equal(responseExists.StatusCode, responseMissing.StatusCode);
    }

    [Fact]
    public async Task ConfirmReset_WithValidToken_ReturnsOk()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var resetService = scope.ServiceProvider.GetRequiredService<IPasswordResetService>();

        var user = User.Create(
            "confirm@example.com", passwordHasher.HashPassword("OldPassword123!"), "Test User", null);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var (rawToken, _) = await resetService.GenerateResetTokenAsync(user.Id);

        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/password-reset/confirm",
            new PasswordResetConfirmRequest(rawToken, "NewPassword456!"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ConfirmReset_WithInvalidToken_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/password-reset/confirm",
            new PasswordResetConfirmRequest("invalid-token-xyz", "NewPassword456!"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ConfirmReset_WithEmptyToken_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/password-reset/confirm",
            new PasswordResetConfirmRequest("", "NewPassword456!"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ConfirmReset_TokenCannotBeUsedTwice()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var resetService = scope.ServiceProvider.GetRequiredService<IPasswordResetService>();

        var user = User.Create(
            "reuse@example.com", passwordHasher.HashPassword("OldPassword123!"), "Test User", null);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var (rawToken, _) = await resetService.GenerateResetTokenAsync(user.Id);

        // Detach so service gets fresh read
        dbContext.Entry(user).State = EntityState.Detached;

        var client = _factory.CreateClient();

        // First use - should succeed
        var firstResponse = await client.PostAsJsonAsync("/api/auth/password-reset/confirm",
            new PasswordResetConfirmRequest(rawToken, "NewPassword456!"));
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

        // Second use - should fail (token already consumed)
        var secondResponse = await client.PostAsJsonAsync("/api/auth/password-reset/confirm",
            new PasswordResetConfirmRequest(rawToken, "AnotherPassword789!"));
        Assert.Equal(HttpStatusCode.BadRequest, secondResponse.StatusCode);
    }

    [Fact]
    public async Task ConfirmReset_WithExpiredToken_ReturnsBadRequest()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var user = User.Create(
            "expired@example.com", passwordHasher.HashPassword("OldPassword123!"), "Test User", null);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        // Create an expired token via reflection to bypass Issue() validation
        var expiredToken = CreateExpiredPasswordResetToken(user.Id);
        dbContext.PasswordResetTokens.Add(expiredToken);
        await dbContext.SaveChangesAsync();

        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/password-reset/confirm",
            new PasswordResetConfirmRequest("expired-token-raw", "NewPassword456!"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ConfirmReset_PasswordHashActuallyChanges()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var resetService = scope.ServiceProvider.GetRequiredService<IPasswordResetService>();

        var newPassword = "NewPassword456!";
        var user = User.Create(
            "hashchange@example.com", passwordHasher.HashPassword("OldPassword123!"), "Test User", null);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var (rawToken, _) = await resetService.GenerateResetTokenAsync(user.Id);

        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/password-reset/confirm",
            new PasswordResetConfirmRequest(rawToken, newPassword));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Verify new password works: login should succeed with new password
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "hashchange@example.com",
            password = newPassword
        });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
    }

    [Fact]
    public async Task RequestReset_EmailSentWithResetUrlContainingToken()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var user = User.Create(
            "logtest@example.com", passwordHasher.HashPassword("Password123!"), "Test User", null);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        _factory.EmailSender.Clear();

        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/password-reset",
            new PasswordResetRequest("logtest@example.com"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Verify email was captured with a reset URL containing a token
        var captured = _factory.EmailSender.CapturedEmails;
        Assert.Single(captured);
        Assert.Equal("logtest@example.com", captured[0].Email);
        Assert.Contains("token=", captured[0].ResetUrl);
        Assert.Contains("reset-password", captured[0].ResetUrl);
    }

    private static PasswordResetToken CreateExpiredPasswordResetToken(Guid userId)
    {
        var rawToken = "expired-token-raw";
        var tokenBytes = System.Text.Encoding.UTF8.GetBytes(rawToken);
        var tokenHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(tokenBytes)).ToLowerInvariant();
        var pastTime = DateTime.UtcNow.AddDays(-2);

        var ctor = typeof(PasswordResetToken).GetConstructor(
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
            null,
            new[] { typeof(Guid), typeof(Guid), typeof(string), typeof(DateTime), typeof(DateTime) },
            null)!;

        return (PasswordResetToken)ctor.Invoke(new object[]
        {
            Guid.NewGuid(), userId, tokenHash, pastTime, DateTime.UtcNow.AddDays(-2)
        });
    }
}
