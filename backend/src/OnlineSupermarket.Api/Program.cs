using OnlineSupermarket.Api.Contracts;
using OnlineSupermarket.Api.Endpoints;
using OnlineSupermarket.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddOpenApi();
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/api/health", () => Results.Ok(new HealthResponse("ok")))
    .WithName("GetHealth")
    .WithTags("System");

// Auth
app.MapAuthEndpoints();

// Catalog (A-1, A-2)
app.MapCatalogEndpoints();

// Branch (A-3)
app.MapBranchEndpoints();

// Cart (A-4)
app.MapCartEndpoints();

// Checkout + Payment (A-5, C-1, C-2, C-3)
app.MapCheckoutEndpoints();

// Orders (C-4, C-5)
app.MapOrderEndpoints();

// Users (FR-114)
app.MapUserEndpoints();

// Addresses (FR-115)
app.MapAddressEndpoints();

app.Run();

public partial class Program;
