using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using OnlineSupermarket.Api.Contracts.Branch;
using OnlineSupermarket.Domain.Identity;
using OnlineSupermarket.Infrastructure.Identity;
using OnlineSupermarket.Infrastructure.Persistence;

namespace OnlineSupermarket.Api.Tests.Endpoints;

public sealed class AdminBranchEndpointsTests
{
    private static async Task<HttpClient> CreateClientAsync(TestApiFactory factory, UserRole role)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();

        var user = User.Create($"{role}_{Guid.NewGuid():N}@test.com", "hash", role.ToString(), null, role);
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", tokenService.GenerateAccessToken(user));
        return client;
    }

    [Fact]
    public async Task List_AsAdmin_ReturnsSeededBranches()
    {
        using var factory = new TestApiFactory();
        var client = await CreateClientAsync(factory, UserRole.Admin);

        var response = await client.GetAsync("/api/admin/branches");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var branches = await response.Content.ReadFromJsonAsync<List<BranchDto>>();
        Assert.NotNull(branches);
        Assert.Contains(branches, b => b.Name == "AptechMart Quận 1");
    }

    [Fact]
    public async Task List_AsCustomer_ReturnsForbidden()
    {
        using var factory = new TestApiFactory();
        var client = await CreateClientAsync(factory, UserRole.Customer);

        var response = await client.GetAsync("/api/admin/branches");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithValidData_ReturnsCreated()
    {
        using var factory = new TestApiFactory();
        var client = await CreateClientAsync(factory, UserRole.Admin);

        var response = await client.PostAsJsonAsync("/api/admin/branches", new
        {
            name = "AptechMart Thủ Đức",
            address = "1 Võ Văn Ngân, Thủ Đức",
            phone = "028 1111 2222",
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<BranchDto>();
        Assert.NotNull(dto);
        Assert.Equal("AptechMart Thủ Đức", dto.Name);
        Assert.True(dto.IsActive);
    }

    [Fact]
    public async Task Create_WithMissingName_ReturnsBadRequest()
    {
        using var factory = new TestApiFactory();
        var client = await CreateClientAsync(factory, UserRole.Admin);

        var response = await client.PostAsJsonAsync("/api/admin/branches", new
        {
            name = "",
            address = "Somewhere",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_ChangesFieldsAndDeactivates_ThenStillListed()
    {
        using var factory = new TestApiFactory();
        var client = await CreateClientAsync(factory, UserRole.Admin);

        var created = await (await client.PostAsJsonAsync("/api/admin/branches", new
        {
            name = "Temp Branch",
            address = "Temp Address",
        })).Content.ReadFromJsonAsync<BranchDto>();
        Assert.NotNull(created);

        var updateResponse = await client.PutAsJsonAsync($"/api/admin/branches/{created.Id}", new
        {
            name = "Renamed Branch",
            address = "New Address",
            phone = (string?)null,
            isActive = false,
        });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<BranchDto>();
        Assert.NotNull(updated);
        Assert.Equal("Renamed Branch", updated.Name);
        Assert.False(updated.IsActive);

        // Admin list includes inactive branches (unlike the public GET /api/branches)
        var list = await (await client.GetAsync("/api/admin/branches")).Content.ReadFromJsonAsync<List<BranchDto>>();
        Assert.NotNull(list);
        Assert.Contains(list, b => b.Id == created.Id && !b.IsActive);
    }

    [Fact]
    public async Task Update_UnknownId_ReturnsNotFound()
    {
        using var factory = new TestApiFactory();
        var client = await CreateClientAsync(factory, UserRole.Admin);

        var response = await client.PutAsJsonAsync($"/api/admin/branches/{Guid.NewGuid()}", new
        {
            name = "X",
            address = "Y",
            phone = (string?)null,
            isActive = true,
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
