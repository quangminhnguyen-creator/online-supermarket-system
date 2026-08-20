using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using OnlineSupermarket.Api.Contracts.Auth;

namespace OnlineSupermarket.Api.Tests.Auth;

public sealed class AuthEndpointTests(AuthTestApiFactory factory) : IClassFixture<AuthTestApiFactory>
{
    [Fact]
    public async Task Register_WithValidData_ReturnsCreated()
    {
        using var client = factory.CreateClient();
        var email = $"newuser_{Guid.NewGuid()}@example.com";
        var request = new RegisterRequest(email, "Password@123", "Nguyen Van B", "0912345678");

        var response = await client.PostAsJsonAsync("/api/auth/register", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<RegisterResponse>();
        Assert.NotNull(result);
        Assert.Equal(email.ToLowerInvariant(), result.Email);
        Assert.Equal("Nguyen Van B", result.FullName);
        Assert.Equal("Customer", result.Role);
    }

    [Fact]
    public async Task Register_WithExistingEmail_ReturnsConflict()
    {
        using var client = factory.CreateClient();
        var email = $"duplicate_{Guid.NewGuid()}@example.com";
        var request = new RegisterRequest(email, "Password@123", "User One", null);

        var firstResponse = await client.PostAsJsonAsync("/api/auth/register", request);
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        var secondResponse = await client.PostAsJsonAsync("/api/auth/register", request);
        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
    }

    [Fact]
    public async Task Register_WithShortPassword_ReturnsBadRequest()
    {
        using var client = factory.CreateClient();
        var request = new RegisterRequest("user@example.com", "123", "User Short", null);

        var response = await client.PostAsJsonAsync("/api/auth/register", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsTokensAndUserInfo()
    {
        using var client = factory.CreateClient();
        var email = $"login_{Guid.NewGuid()}@example.com";
        const string password = "ValidPassword@123";

        await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, password, "Test Login User", null));

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var authResult = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(authResult);
        Assert.NotEmpty(authResult.AccessToken);
        Assert.NotEmpty(authResult.RefreshToken);
        Assert.Equal(email.ToLowerInvariant(), authResult.User.Email);
        Assert.Equal("Test Login User", authResult.User.FullName);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        using var client = factory.CreateClient();
        var email = $"wrong_pwd_{Guid.NewGuid()}@example.com";

        await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "CorrectPass@123", "User", null));

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "WrongPass@123"));

        Assert.Equal(HttpStatusCode.Unauthorized, loginResponse.StatusCode);
    }

    [Fact]
    public async Task RefreshToken_WithValidToken_RotatesTokens()
    {
        using var client = factory.CreateClient();
        var email = $"refresh_{Guid.NewGuid()}@example.com";
        const string password = "ValidPassword@123";

        await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, password, "Refresh User", null));
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        var authResult = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();

        var refreshResponse = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest(authResult!.RefreshToken));

        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
        var refreshedAuth = await refreshResponse.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(refreshedAuth);
        Assert.NotEmpty(refreshedAuth.AccessToken);
        Assert.NotEmpty(refreshedAuth.RefreshToken);
        Assert.NotEqual(authResult.RefreshToken, refreshedAuth.RefreshToken);

        // Old refresh token must be revoked and fail on second use
        var reuseResponse = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest(authResult.RefreshToken));
        Assert.Equal(HttpStatusCode.Unauthorized, reuseResponse.StatusCode);
    }

    [Fact]
    public async Task GetMe_WithValidBearerToken_ReturnsUserProfile()
    {
        using var client = factory.CreateClient();
        var email = $"me_{Guid.NewGuid()}@example.com";
        const string password = "ValidPassword@123";

        await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, password, "Me User", "0909090909"));
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        var authResult = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResult!.AccessToken);
        var meResponse = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);
        var user = await meResponse.Content.ReadFromJsonAsync<UserDto>();
        Assert.NotNull(user);
        Assert.Equal(email.ToLowerInvariant(), user.Email);
        Assert.Equal("Me User", user.FullName);
        Assert.Equal("0909090909", user.Phone);
    }

    [Fact]
    public async Task GetMe_WithoutToken_ReturnsUnauthorized()
    {
        using var client = factory.CreateClient();
        var meResponse = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, meResponse.StatusCode);
    }

    [Fact]
    public async Task Logout_RevokesRefreshToken()
    {
        using var client = factory.CreateClient();
        var email = $"logout_{Guid.NewGuid()}@example.com";
        const string password = "ValidPassword@123";

        await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, password, "Logout User", null));
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        var authResult = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();

        var logoutResponse = await client.PostAsJsonAsync("/api/auth/logout", new LogoutRequest(authResult!.RefreshToken));
        Assert.Equal(HttpStatusCode.OK, logoutResponse.StatusCode);

        // Try using the refresh token after logout
        var refreshResponse = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest(authResult.RefreshToken));
        Assert.Equal(HttpStatusCode.Unauthorized, refreshResponse.StatusCode);
    }
}
