using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using OnlineSupermarket.Api.Endpoints;
using OnlineSupermarket.Api.Tests.Auth;

namespace OnlineSupermarket.Api.Tests.Endpoints;

public sealed class AddressEndpointsTests : IClassFixture<AuthTestApiFactory>
{
    private readonly AuthTestApiFactory _factory;

    public AddressEndpointsTests(AuthTestApiFactory factory)
    {
        _factory = factory;
    }

    private static readonly CreateAddressRequest _validAddress = new(
        RecipientName: "John Doe",
        Phone: "0987654321",
        Street: "123 Main Street",
        Ward: "Ward 1",
        District: "District 1",
        City: "Ho Chi Minh City",
        PostalCode: "700000");

    [Fact]
    public async Task GetAddresses_ReturnsOk()
    {
        var (client, user) = await CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/users/me/addresses");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetAddresses_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/users/me/addresses");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateAddress_WithValidData_ReturnsCreated()
    {
        var (client, _) = await CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/users/me/addresses", _validAddress);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<AddressDto>();
        Assert.NotNull(dto);
        Assert.Equal(_validAddress.RecipientName, dto.RecipientName);
    }

    [Fact]
    public async Task CreateAddress_SetsFirstAsDefault()
    {
        var (client, _) = await CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/users/me/addresses", _validAddress);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<AddressDto>();
        Assert.NotNull(dto);
        Assert.True(dto.IsDefault, "First address should be set as default.");
    }

    [Fact]
    public async Task UpdateAddress_WithValidData_ReturnsOk()
    {
        var (client, _) = await CreateAuthenticatedClientAsync();
        var createResponse = await client.PostAsJsonAsync("/api/users/me/addresses", _validAddress);
        var created = await createResponse.Content.ReadFromJsonAsync<AddressDto>();
        Assert.NotNull(created);

        var updateRequest = new UpdateAddressRequest(
            RecipientName: "Jane Doe",
            Phone: "0123456789",
            Street: "456 New Street",
            Ward: "Ward 2",
            District: "District 2",
            City: "Hanoi",
            PostalCode: "100000");

        var response = await client.PutAsJsonAsync($"/api/users/me/addresses/{created.Id}", updateRequest);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateAddress_NotOwner_ReturnsNotFound()
    {
        var (client1, _) = await CreateAuthenticatedClientAsync();
        var (client2, user2) = await CreateAuthenticatedClientAsync();

        var createResponse = await client1.PostAsJsonAsync("/api/users/me/addresses", _validAddress);
        var created = await createResponse.Content.ReadFromJsonAsync<AddressDto>();
        Assert.NotNull(created);

        // Try to update with client2 (different user)
        var updateRequest = new UpdateAddressRequest(
            RecipientName: "Hacker",
            Phone: "0000000000",
            Street: "Hacked",
            Ward: "Ward",
            District: "District",
            City: "City",
            PostalCode: null);

        var response = await client2.PutAsJsonAsync($"/api/users/me/addresses/{created.Id}", updateRequest);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteAddress_ReturnsNoContent()
    {
        var (client, _) = await CreateAuthenticatedClientAsync();
        var createResponse = await client.PostAsJsonAsync("/api/users/me/addresses", _validAddress);
        var created = await createResponse.Content.ReadFromJsonAsync<AddressDto>();
        Assert.NotNull(created);

        var response = await client.DeleteAsync($"/api/users/me/addresses/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task SetAddressAsDefault_ClearsExistingDefault()
    {
        var (client, _) = await CreateAuthenticatedClientAsync();

        // Create first address (will be default)
        var firstResponse = await client.PostAsJsonAsync("/api/users/me/addresses", _validAddress);
        var first = await firstResponse.Content.ReadFromJsonAsync<AddressDto>();
        Assert.NotNull(first);
        Assert.True(first.IsDefault);

        // Create second address (not default)
        var secondAddress = new CreateAddressRequest(
            RecipientName: "Second Recipient",
            Phone: "0987654321",
            Street: "Second Street",
            Ward: "Ward 2",
            District: "District 2",
            City: "Da Nang",
            PostalCode: "550000");
        var secondResponse = await client.PostAsJsonAsync("/api/users/me/addresses", secondAddress);
        var second = await secondResponse.Content.ReadFromJsonAsync<AddressDto>();
        Assert.NotNull(second);
        Assert.False(second.IsDefault);

        // Set second as default
        var setResponse = await client.PutAsJsonAsync($"/api/users/me/addresses/{second.Id}/default", Array.Empty<object>());
        Assert.Equal(HttpStatusCode.OK, setResponse.StatusCode);

        // Verify first is no longer default
        var getResponse = await client.GetAsync("/api/users/me/addresses");
        var addresses = await getResponse.Content.ReadFromJsonAsync<List<AddressDto>>();
        Assert.NotNull(addresses);
        Assert.Equal(2, addresses.Count);

        var formerDefault = addresses.First(a => a.Id == first.Id);
        var newDefault = addresses.First(a => a.Id == second.Id);

        Assert.False(formerDefault.IsDefault, "Former default should be cleared.");
        Assert.True(newDefault.IsDefault, "New default should be set.");
    }

    [Fact]
    public async Task CreateAddress_WithEmptyRequiredField_ReturnsBadRequest()
    {
        var (client, _) = await CreateAuthenticatedClientAsync();

        var invalidRequest = new CreateAddressRequest(
            RecipientName: "",  // required
            Phone: "0987654321",
            Street: "123 Main Street",
            Ward: "Ward 1",
            District: "District 1",
            City: "Ho Chi Minh City",
            PostalCode: null);

        var response = await client.PostAsJsonAsync("/api/users/me/addresses", invalidRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateAddress_WithEmptyRequiredField_ReturnsBadRequest()
    {
        var (client, _) = await CreateAuthenticatedClientAsync();
        var createResponse = await client.PostAsJsonAsync("/api/users/me/addresses", _validAddress);
        var created = await createResponse.Content.ReadFromJsonAsync<AddressDto>();
        Assert.NotNull(created);

        var invalidRequest = new UpdateAddressRequest(
            RecipientName: "",  // required
            Phone: "0123456789",
            Street: "456 New Street",
            Ward: "Ward 2",
            District: "District 2",
            City: "Hanoi",
            PostalCode: null);

        var response = await client.PutAsJsonAsync($"/api/users/me/addresses/{created.Id}", invalidRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SetAddressAsDefault_OnAlreadyDefaultAddress_KeepsItDefault()
    {
        var (client, _) = await CreateAuthenticatedClientAsync();

        // Create first address (will be default)
        var firstResponse = await client.PostAsJsonAsync("/api/users/me/addresses", _validAddress);
        var first = await firstResponse.Content.ReadFromJsonAsync<AddressDto>();
        Assert.NotNull(first);
        Assert.True(first.IsDefault);

        // Set the same address as default again
        var setResponse = await client.PutAsJsonAsync($"/api/users/me/addresses/{first.Id}/default", Array.Empty<object>());
        Assert.Equal(HttpStatusCode.OK, setResponse.StatusCode);

        // Verify it's still default
        var getResponse = await client.GetAsync("/api/users/me/addresses");
        var addresses = await getResponse.Content.ReadFromJsonAsync<List<AddressDto>>();
        Assert.NotNull(addresses);
        var stillDefault = addresses.First(a => a.Id == first.Id);
        Assert.True(stillDefault.IsDefault, "Address should remain default after re-setting as default.");
    }

    [Fact]
    public async Task CreateAddress_FirstAddressGetsIsDefault()
    {
        var (client, _) = await CreateAuthenticatedClientAsync();

        // No addresses exist — first should be default
        var response = await client.PostAsJsonAsync("/api/users/me/addresses", _validAddress);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<AddressDto>();
        Assert.NotNull(created);
        Assert.True(created.IsDefault, "First address should be set as default.");
    }

    private async Task<(System.Net.Http.HttpClient Client, OnlineSupermarket.Domain.Identity.User User)> CreateAuthenticatedClientAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OnlineSupermarket.Infrastructure.Persistence.AppDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<OnlineSupermarket.Infrastructure.Identity.IPasswordHasher>();
        var tokenService = scope.ServiceProvider.GetRequiredService<OnlineSupermarket.Infrastructure.Identity.ITokenService>();

        var uniqueEmail = $"test_{Guid.NewGuid():N}@example.com";
        var user = OnlineSupermarket.Domain.Identity.User.Create(
            uniqueEmail,
            passwordHasher.HashPassword("TestPassword123!"),
            "Test User",
            "0987654321");

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var client = _factory.CreateClient();
        var token = tokenService.GenerateAccessToken(user);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return (client, user);
    }
}
