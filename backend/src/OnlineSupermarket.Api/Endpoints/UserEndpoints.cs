using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineSupermarket.Domain.Identity;
using OnlineSupermarket.Infrastructure.Identity;
using OnlineSupermarket.Infrastructure.Persistence;

namespace OnlineSupermarket.Api.Endpoints;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/users").WithTags("Users");

        group.MapPut("/me", UpdateProfileAsync)
            .WithName("UpdateProfile")
            .RequireAuthorization()
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPut("/me/password", ChangePasswordAsync)
            .WithName("ChangePassword")
            .RequireAuthorization()
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        var adminGroup = routes.MapGroup("/api/admin/users").WithTags("Admin/Users");

        adminGroup.MapGet(string.Empty, ListUsersAsync)
            .WithName("ListUsers")
            .RequireAuthorization("AdminOnly")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        adminGroup.MapPut("/{id}/status", UpdateUserStatusAsync)
            .WithName("UpdateUserStatus")
            .RequireAuthorization("AdminOnly")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        return routes;
    }

    private static async Task<IResult> UpdateProfileAsync(
        [FromBody] UpdateProfileRequest request,
        ClaimsPrincipal claimsPrincipal,
        [FromServices] AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var userIdClaim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? claimsPrincipal.FindFirst("sub")?.Value;

        if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Results.Unauthorized();
        }

        var user = await dbContext.Users.FindAsync([userId], cancellationToken);
        if (user == null)
        {
            return Results.NotFound();
        }

        try
        {
            user.UpdateProfile(request.FullName, request.Phone);
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(new { message = "Profile updated successfully." });
    }

    private static async Task<IResult> ChangePasswordAsync(
        [FromBody] ChangePasswordRequest request,
        ClaimsPrincipal claimsPrincipal,
        [FromServices] AppDbContext dbContext,
        [FromServices] IPasswordHasher passwordHasher,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(claimsPrincipal);
        if (userId == null) return Results.Unauthorized();

        var user = await dbContext.Users.FindAsync([userId.Value], cancellationToken);
        if (user == null) return Results.NotFound();

        if (!passwordHasher.VerifyPassword(user.PasswordHash, request.CurrentPassword))
        {
            return Results.BadRequest(new { message = "Current password is incorrect." });
        }

        var newPasswordHash = passwordHasher.HashPassword(request.NewPassword);
        user.UpdatePassword(newPasswordHash);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(new { message = "Password changed successfully." });
    }

    private static async Task<IResult> ListUsersAsync(
        [FromQuery] int page,
        [FromQuery] int pageSize,
        [FromServices] AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 20 : Math.Min(pageSize, 100);

        var totalCount = await dbContext.Users.CountAsync(cancellationToken);
        var users = await dbContext.Users
            .OrderBy(u => u.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var dtos = users.Select(u => new UserSummaryDto(
            u.Id,
            u.Email,
            u.FullName,
            u.Phone,
            u.Role.ToString(),
            u.Status.ToString(),
            u.CreatedAtUtc,
            u.UpdatedAtUtc)).ToList();

        return Results.Ok(new PaginatedUsersDto(page, pageSize, totalCount, dtos));
    }

    private static async Task<IResult> UpdateUserStatusAsync(
        [FromRoute] Guid id,
        [FromBody] UpdateUserStatusRequest request,
        [FromServices] AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<UserStatus>(request.Status, ignoreCase: true, out var newStatus)
            || !Enum.IsDefined(typeof(UserStatus), newStatus))
        {
            return Results.BadRequest(new { message = "Invalid status. Valid values: Active, Locked, Disabled." });
        }

        var user = await dbContext.Users.FindAsync([id], cancellationToken);
        if (user == null) return Results.NotFound();

        user.ChangeStatus(newStatus);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(new { message = $"User status updated to {newStatus}." });
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

public sealed record UpdateProfileRequest(
    string FullName,
    string? Phone);

public sealed record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword);

public sealed record UpdateUserStatusRequest(
    string Status);

public sealed record UserSummaryDto(
    Guid Id,
    string Email,
    string FullName,
    string? Phone,
    string Role,
    string Status,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record PaginatedUsersDto(
    int Page,
    int PageSize,
    int TotalCount,
    List<UserSummaryDto> Users);
