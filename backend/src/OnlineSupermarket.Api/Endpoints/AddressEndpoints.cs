using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineSupermarket.Domain.Entities;
using OnlineSupermarket.Infrastructure.Persistence;

namespace OnlineSupermarket.Api.Endpoints;

public static class AddressEndpoints
{
    public static IEndpointRouteBuilder MapAddressEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/users/me/addresses").WithTags("Addresses");

        group.MapGet(string.Empty, GetAddressesAsync)
            .WithName("GetAddresses")
            .RequireAuthorization()
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost(string.Empty, CreateAddressAsync)
            .WithName("CreateAddress")
            .RequireAuthorization()
            .Produces<AddressDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPut("/{id}", UpdateAddressAsync)
            .WithName("UpdateAddress")
            .RequireAuthorization()
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/{id}", DeleteAddressAsync)
            .WithName("DeleteAddress")
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/{id}/default", SetAddressAsDefaultAsync)
            .WithName("SetAddressAsDefault")
            .RequireAuthorization()
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return routes;
    }

    private static async Task<IResult> GetAddressesAsync(
        ClaimsPrincipal claimsPrincipal,
        [FromServices] AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(claimsPrincipal);
        if (userId == null) return Results.Unauthorized();

        var addresses = await dbContext.Addresses
            .Where(a => a.UserId == userId.Value)
            .OrderByDescending(a => a.IsDefault)
            .ThenBy(a => a.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var dtos = addresses.Select(a => new AddressDto(
            a.Id,
            a.RecipientName,
            a.Phone,
            a.Street,
            a.Ward,
            a.District,
            a.City,
            a.PostalCode,
            a.IsDefault,
            a.CreatedAtUtc,
            a.UpdatedAtUtc));

        return Results.Ok(dtos);
    }

    private static async Task<IResult> CreateAddressAsync(
        [FromBody] CreateAddressRequest request,
        ClaimsPrincipal claimsPrincipal,
        [FromServices] AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(claimsPrincipal);
        if (userId == null) return Results.Unauthorized();

        // Validate first so we can return 400 without entering transaction
        Address address;
        try
        {
            address = Address.Create(
                userId.Value,
                request.RecipientName,
                request.Phone,
                request.Street,
                request.Ward,
                request.District,
                request.City,
                request.PostalCode,
                isDefault: false); // determined inside transaction
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }

        var supportsTransactions = dbContext.Database.ProviderName != "Microsoft.EntityFrameworkCore.InMemory";

        if (supportsTransactions)
        {
            var strategy = dbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await dbContext.Database
                    .BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);

                // Re-check inside transaction to avoid race condition
                var hasExisting = await dbContext.Addresses
                    .AnyAsync(a => a.UserId == userId.Value, cancellationToken);

                if (!hasExisting)
                {
                    address.SetAsDefault();
                }

                dbContext.Addresses.Add(address);
                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            });
        }
        else
        {
            // In-memory fallback for tests: sequential execution, no concurrency
            var hasExisting = await dbContext.Addresses
                .AnyAsync(a => a.UserId == userId.Value, cancellationToken);

            if (!hasExisting)
            {
                address.SetAsDefault();
            }

            dbContext.Addresses.Add(address);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return Results.Created($"/api/users/me/addresses/{address.Id}",
            new AddressDto(address.Id, address.RecipientName, address.Phone, address.Street,
                address.Ward, address.District, address.City, address.PostalCode,
                address.IsDefault, address.CreatedAtUtc, address.UpdatedAtUtc));
    }

    private static async Task<IResult> UpdateAddressAsync(
        [FromRoute] Guid id,
        [FromBody] UpdateAddressRequest request,
        ClaimsPrincipal claimsPrincipal,
        [FromServices] AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(claimsPrincipal);
        if (userId == null) return Results.Unauthorized();

        var address = await dbContext.Addresses
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId.Value, cancellationToken);

        if (address == null)
        {
            return Results.NotFound();
        }

        try
        {
            address.Update(
                request.RecipientName,
                request.Phone,
                request.Street,
                request.Ward,
                request.District,
                request.City,
                request.PostalCode);
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(new { message = "Address updated successfully." });
    }

    private static async Task<IResult> DeleteAddressAsync(
        [FromRoute] Guid id,
        ClaimsPrincipal claimsPrincipal,
        [FromServices] AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(claimsPrincipal);
        if (userId == null) return Results.Unauthorized();

        var address = await dbContext.Addresses
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId.Value, cancellationToken);

        if (address == null)
        {
            return Results.NotFound();
        }

        dbContext.Addresses.Remove(address);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.NoContent();
    }

    private static async Task<IResult> SetAddressAsDefaultAsync(
        [FromRoute] Guid id,
        ClaimsPrincipal claimsPrincipal,
        [FromServices] AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(claimsPrincipal);
        if (userId == null) return Results.Unauthorized();

        var address = await dbContext.Addresses
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId.Value, cancellationToken);

        if (address == null)
        {
            return Results.NotFound();
        }

        var supportsTransactions = dbContext.Database.ProviderName != "Microsoft.EntityFrameworkCore.InMemory";

        if (supportsTransactions)
        {
            var strategy = dbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await dbContext.Database
                    .BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);

                // Clear defaults for all OTHER addresses (exclude target to avoid tracker issue)
                await dbContext.Addresses
                    .Where(a => a.UserId == userId.Value && a.IsDefault && a.Id != id)
                    .ExecuteUpdateAsync(
                        s => s.SetProperty(a => a.IsDefault, false)
                              .SetProperty(a => a.UpdatedAtUtc, DateTime.UtcNow),
                        cancellationToken);

                // Reload target to sync with DB state (may have been set to false by bulk update)
                dbContext.Entry(address).Reload();
                address.SetAsDefault();
                await dbContext.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);
            });
        }
        else
        {
            // In-memory: sequential execution, exclude target from clear
            var existingDefault = await dbContext.Addresses
                .Where(a => a.UserId == userId.Value && a.IsDefault && a.Id != id)
                .ToListAsync(cancellationToken);

            foreach (var a in existingDefault)
            {
                a.ClearDefault();
            }

            address.SetAsDefault();
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return Results.Ok(new { message = "Address set as default." });
    }

    private static Guid? GetUserId(ClaimsPrincipal claimsPrincipal)
    {
        var userIdClaim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? claimsPrincipal.FindFirst("sub")?.Value;

        if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return null;
        }

        return userId;
    }
}

public sealed record CreateAddressRequest(
    string RecipientName,
    string Phone,
    string Street,
    string Ward,
    string District,
    string City,
    string? PostalCode);

public sealed record UpdateAddressRequest(
    string RecipientName,
    string Phone,
    string Street,
    string Ward,
    string District,
    string City,
    string? PostalCode);

public sealed record AddressDto(
    Guid Id,
    string RecipientName,
    string Phone,
    string Street,
    string Ward,
    string District,
    string City,
    string? PostalCode,
    bool IsDefault,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
