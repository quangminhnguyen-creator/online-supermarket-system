using System.Net;
using Microsoft.AspNetCore.Hosting;

namespace OnlineSupermarket.Api.Tests;

[Collection(ApiConfigurationCollection.Name)]
public sealed class OpenApiContractTests(TestApiFactory factory)
    : IClassFixture<TestApiFactory>
{
    [Fact]
    public async Task GetOpenApi_ContainsHealthOperation()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");
        var document = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("/api/health", document, StringComparison.Ordinal);
        Assert.Contains("GetHealth", document, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetOpenApi_InProduction_ReturnsNotFound()
    {
        using var productionFactory = factory.WithWebHostBuilder(builder =>
            builder.UseEnvironment("Production"));
        using var client = productionFactory.CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
