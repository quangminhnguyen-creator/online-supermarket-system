using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OnlineSupermarket.Infrastructure;

namespace OnlineSupermarket.Api.Tests.Configuration;

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
}
