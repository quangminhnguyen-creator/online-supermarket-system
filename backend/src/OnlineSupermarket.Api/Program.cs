using OnlineSupermarket.Api.Contracts;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/api/health", () => Results.Ok(new HealthResponse("ok")))
    .WithName("GetHealth")
    .WithTags("System");

app.Run();

public partial class Program;
