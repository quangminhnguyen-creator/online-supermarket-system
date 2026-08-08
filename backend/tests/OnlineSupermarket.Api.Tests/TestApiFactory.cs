using Microsoft.AspNetCore.Mvc.Testing;

namespace OnlineSupermarket.Api.Tests;

public sealed class TestApiFactory : WebApplicationFactory<Program>
{
    private const string VariableName = "ConnectionStrings__DefaultConnection";
    private readonly string? _originalConnectionString;

    public TestApiFactory()
    {
        _originalConnectionString = Environment.GetEnvironmentVariable(VariableName);
        Environment.SetEnvironmentVariable(
            VariableName,
            "Server=localhost;Port=3306;Database=test;User=test;Password=test");
    }

    protected override void Dispose(bool disposing)
    {
        try
        {
            base.Dispose(disposing);
        }
        finally
        {
            Environment.SetEnvironmentVariable(VariableName, _originalConnectionString);
        }
    }
}

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class ApiConfigurationCollection
{
    public const string Name = "API configuration";
}
