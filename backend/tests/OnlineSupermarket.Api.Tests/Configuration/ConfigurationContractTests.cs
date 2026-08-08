using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.Testing;
using OnlineSupermarket.Infrastructure;

namespace OnlineSupermarket.Api.Tests.Configuration;

[Collection(OnlineSupermarket.Api.Tests.ApiConfigurationCollection.Name)]
public sealed class ConfigurationContractTests
{
    [Fact]
    public void MissingDefaultConnection_StopsStartup()
    {
        var configuration = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddInfrastructure(configuration));

        Assert.Equal("DefaultConnection is required.", exception.Message);
    }

    [Fact]
    public void MissingDefaultConnection_StopsApiCompositionRoot()
    {
        const string variableName = "ConnectionStrings__DefaultConnection";
        var originalValue = Environment.GetEnvironmentVariable(variableName);
        Environment.SetEnvironmentVariable(variableName, null);

        try
        {
            using var factory = new WebApplicationFactory<Program>();

            var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());

            Assert.Contains("DefaultConnection is required.", exception.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            Environment.SetEnvironmentVariable(variableName, originalValue);
        }
    }
}
