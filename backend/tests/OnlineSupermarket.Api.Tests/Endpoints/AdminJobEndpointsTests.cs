using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using OnlineSupermarket.Api.Contracts.Jobs;
using OnlineSupermarket.Api.Tests.Auth;
using OnlineSupermarket.Domain.Identity;
using OnlineSupermarket.Domain.Jobs;
using OnlineSupermarket.Infrastructure.Persistence;
using Xunit;

namespace OnlineSupermarket.Api.Tests.Endpoints;

public class AdminJobEndpointsTests : IClassFixture<AuthTestApiFactory>
{
    private readonly AuthTestApiFactory _factory;

    public AdminJobEndpointsTests(AuthTestApiFactory factory)
    {
        _factory = factory;
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync(UserRole role)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<OnlineSupermarket.Infrastructure.Identity.IPasswordHasher>();

        var email = $"{Guid.NewGuid()}@example.com";
        var user = User.Create(email, passwordHasher.HashPassword("Password123!"), "Test User", null, role);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var tokenService = scope.ServiceProvider.GetRequiredService<OnlineSupermarket.Infrastructure.Identity.ITokenService>();
        var token = tokenService.GenerateAccessToken(user);

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    [Fact]
    public async Task GetJobs_WhenUnauthorized_ShouldReturn401()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/admin/jobs");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetJobs_WhenNotAdmin_ShouldReturn403()
    {
        var client = await CreateAuthenticatedClientAsync(UserRole.Customer);
        var response = await client.GetAsync("/api/admin/jobs");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetJobs_WhenAdmin_ShouldReturnPaginatedJobs()
    {
        var client = await CreateAuthenticatedClientAsync(UserRole.Admin);
        var response = await client.GetAsync("/api/admin/jobs?page=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetJob_WhenNotFound_ShouldReturn404()
    {
        var client = await CreateAuthenticatedClientAsync(UserRole.Admin);
        var response = await client.GetAsync($"/api/admin/jobs/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
