using OnlineSupermarket.Api.Contracts;
using OnlineSupermarket.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddInfrastructure(builder.Configuration);
var app = builder.Build();

app.MapGet("/api/health", () => Results.Ok(new HealthResponse("ok")))
    .WithName("GetHealth")
    .WithTags("System");

app.Run();

public partial class Program;
