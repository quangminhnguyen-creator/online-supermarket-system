using System.Net;
using System.Net.Http.Json;

namespace OnlineSupermarket.Api.Tests;

[Collection(ApiConfigurationCollection.Name)]
public sealed class HealthEndpointTests(TestApiFactory factory)
    : IClassFixture<TestApiFactory>
{
    [Fact]
    public async Task GetHealth_ReturnsOkPayload()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<HealthPayload>();
        Assert.Equal("ok", payload?.Status);
    }

    private sealed record HealthPayload(string Status);
}
