using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using OnlineSupermarket.Api.Contracts.Auth;
using OnlineSupermarket.Api.Tests.Auth;

namespace OnlineSupermarket.Api.Tests.Endpoints;

public sealed class UserEndpointsTests : IClassFixture<AuthTestApiFactory>
{
    private readonly AuthTestApiFactory _factory;

    public UserEndpointsTests(AuthTestApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task UpdateProfile_WithValidData_ReturnsOk()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OnlineSupermarket.Infrastructure.Persistence.AppDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<OnlineSupermarket.Infrastructure.Identity.IPasswordHasher>();

        var user = OnlineSupermarket.Domain.Identity.User.Create(
            "test@example.com", passwordHasher.HashPassword("Password123!"), "Old Name", null);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var tokenService = scope.ServiceProvider.GetRequiredService<OnlineSupermarket.Infrastructure.Identity.ITokenService>();
        var token = tokenService.GenerateAccessToken(user);
        
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PutAsJsonAsync("/api/users/me", new { fullName = "New Name", phone = "0987654321" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateProfile_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PutAsJsonAsync("/api/users/me", new { fullName = "New Name" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_WithValidData_ReturnsOk()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OnlineSupermarket.Infrastructure.Persistence.AppDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<OnlineSupermarket.Infrastructure.Identity.IPasswordHasher>();

        var passwordHash = passwordHasher.HashPassword("OldPassword123!");
        var user = OnlineSupermarket.Domain.Identity.User.Create(
            "changepwd@example.com", passwordHash, "Test User", null);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var tokenService = scope.ServiceProvider.GetRequiredService<OnlineSupermarket.Infrastructure.Identity.ITokenService>();
        var token = tokenService.GenerateAccessToken(user);
        
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PutAsJsonAsync("/api/users/me/password", new
        {
            currentPassword = "OldPassword123!",
            newPassword = "NewPassword456!"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_WithWrongCurrentPassword_ReturnsBadRequest()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OnlineSupermarket.Infrastructure.Persistence.AppDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<OnlineSupermarket.Infrastructure.Identity.IPasswordHasher>();

        var user = OnlineSupermarket.Domain.Identity.User.Create(
            "wrongpwd@example.com", passwordHasher.HashPassword("CorrectPassword123!"), "Test User", null);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var tokenService = scope.ServiceProvider.GetRequiredService<OnlineSupermarket.Infrastructure.Identity.ITokenService>();
        var token = tokenService.GenerateAccessToken(user);
        
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PutAsJsonAsync("/api/users/me/password", new
        {
            currentPassword = "WrongPassword123!",
            newPassword = "NewPassword456!"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ListUsers_AsAdmin_ReturnsPaginatedUsers()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OnlineSupermarket.Infrastructure.Persistence.AppDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<OnlineSupermarket.Infrastructure.Identity.IPasswordHasher>();

        var admin = OnlineSupermarket.Domain.Identity.User.Create(
            "admin@example.com", passwordHasher.HashPassword("Admin123!"), "Admin User", null,
            OnlineSupermarket.Domain.Identity.UserRole.Admin);
        dbContext.Users.Add(admin);
        await dbContext.SaveChangesAsync();

        var tokenService = scope.ServiceProvider.GetRequiredService<OnlineSupermarket.Infrastructure.Identity.ITokenService>();
        var token = tokenService.GenerateAccessToken(admin);
        
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/admin/users?page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<OnlineSupermarket.Api.Endpoints.PaginatedUsersDto>();
        Assert.NotNull(result);
        Assert.True(result.TotalCount >= 1);
    }

    [Fact]
    public async Task ListUsers_AsCustomer_ReturnsForbidden()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OnlineSupermarket.Infrastructure.Persistence.AppDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<OnlineSupermarket.Infrastructure.Identity.IPasswordHasher>();

        var user = OnlineSupermarket.Domain.Identity.User.Create(
            "customer_forbidden@example.com", passwordHasher.HashPassword("Customer123!"), "Customer", null);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var tokenService = scope.ServiceProvider.GetRequiredService<OnlineSupermarket.Infrastructure.Identity.ITokenService>();
        var token = tokenService.GenerateAccessToken(user);
        
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/admin/users");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateUserStatus_AsAdmin_UpdatesStatus()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OnlineSupermarket.Infrastructure.Persistence.AppDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<OnlineSupermarket.Infrastructure.Identity.IPasswordHasher>();

        var admin = OnlineSupermarket.Domain.Identity.User.Create(
            "admin2@example.com", passwordHasher.HashPassword("Admin123!"), "Admin", null,
            OnlineSupermarket.Domain.Identity.UserRole.Admin);
        var targetUser = OnlineSupermarket.Domain.Identity.User.Create(
            "target@example.com", passwordHasher.HashPassword("User123!"), "Target User", null);
        dbContext.Users.AddRange(admin, targetUser);
        await dbContext.SaveChangesAsync();

        var tokenService = scope.ServiceProvider.GetRequiredService<OnlineSupermarket.Infrastructure.Identity.ITokenService>();
        var token = tokenService.GenerateAccessToken(admin);
        
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PutAsJsonAsync($"/api/admin/users/{targetUser.Id}/status", new { status = "Locked" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateUserStatus_AsCustomer_ReturnsForbidden()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OnlineSupermarket.Infrastructure.Persistence.AppDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<OnlineSupermarket.Infrastructure.Identity.IPasswordHasher>();

        var user = OnlineSupermarket.Domain.Identity.User.Create(
            "customer2@example.com", passwordHasher.HashPassword("Customer123!"), "Customer", null);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var tokenService = scope.ServiceProvider.GetRequiredService<OnlineSupermarket.Infrastructure.Identity.ITokenService>();
        var token = tokenService.GenerateAccessToken(user);

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PutAsJsonAsync($"/api/admin/users/{Guid.NewGuid()}/status", new { status = "Locked" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateUserStatus_WithInvalidStatusValue_ReturnsBadRequest()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OnlineSupermarket.Infrastructure.Persistence.AppDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<OnlineSupermarket.Infrastructure.Identity.IPasswordHasher>();

        var admin = OnlineSupermarket.Domain.Identity.User.Create(
            "admin_invalid_status@example.com", passwordHasher.HashPassword("Admin123!"), "Admin", null,
            OnlineSupermarket.Domain.Identity.UserRole.Admin);
        var targetUser = OnlineSupermarket.Domain.Identity.User.Create(
            "target_invalid@example.com", passwordHasher.HashPassword("User123!"), "Target", null);
        dbContext.Users.AddRange(admin, targetUser);
        await dbContext.SaveChangesAsync();

        var tokenService = scope.ServiceProvider.GetRequiredService<OnlineSupermarket.Infrastructure.Identity.ITokenService>();
        var token = tokenService.GenerateAccessToken(admin);

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Enum.TryParse("999") succeeds but Enum.IsDefined rejects it
        var response = await client.PutAsJsonAsync($"/api/admin/users/{targetUser.Id}/status", new { status = "999" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        // Verify user status was NOT changed
        var unchangedUser = await dbContext.Users.FindAsync(targetUser.Id);
        Assert.NotNull(unchangedUser);
        Assert.Equal(OnlineSupermarket.Domain.Identity.UserStatus.Active, unchangedUser.Status);
    }

    [Fact]
    public async Task UpdateProfile_WithEmptyRequiredField_ReturnsBadRequest()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OnlineSupermarket.Infrastructure.Persistence.AppDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<OnlineSupermarket.Infrastructure.Identity.IPasswordHasher>();

        var user = OnlineSupermarket.Domain.Identity.User.Create(
            "empty_profile@example.com", passwordHasher.HashPassword("Password123!"), "Old Name", null);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var tokenService = scope.ServiceProvider.GetRequiredService<OnlineSupermarket.Infrastructure.Identity.ITokenService>();
        var token = tokenService.GenerateAccessToken(user);

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Empty string for required FullName field
        var response = await client.PutAsJsonAsync("/api/users/me", new { fullName = "", phone = "0987654321" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
