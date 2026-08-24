using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineSupermarket.Infrastructure.Identity;
using OnlineSupermarket.Infrastructure.Persistence;
using OnlineSupermarket.Infrastructure.Services;

namespace OnlineSupermarket.Api.Endpoints;

public static class PasswordResetEndpoints
{
    public static IEndpointRouteBuilder MapPasswordResetEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/auth/password-reset").WithTags("PasswordReset");

        group.MapPost(string.Empty, RequestPasswordResetAsync)
            .WithName("RequestPasswordReset")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPost("/confirm", ConfirmPasswordResetAsync)
            .WithName("ConfirmPasswordReset")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return routes;
    }

    private static async Task<IResult> RequestPasswordResetAsync(
        [FromBody] PasswordResetRequest request,
        [FromServices] IPasswordResetService passwordResetService,
        [FromServices] IEmailSender emailSender,
        [FromServices] AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request?.Email))
        {
            return Results.BadRequest(new { message = "Email is required." });
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await dbContext.Users
            .SingleOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        // Always return 200 OK to prevent email enumeration
        if (user == null)
        {
            return Results.Ok(new { message = "If the email exists, a reset link has been sent." });
        }

        var (rawToken, _) = await passwordResetService.GenerateResetTokenAsync(user.Id, cancellationToken);

        // Build reset URL and send via email abstraction — raw token never logged
        var resetUrl = $"/reset-password?token={Uri.EscapeDataString(rawToken)}";
        await emailSender.SendPasswordResetEmailAsync(user.Email, resetUrl, cancellationToken);

        return Results.Ok(new { message = "If the email exists, a reset link has been sent." });
    }

    private static async Task<IResult> ConfirmPasswordResetAsync(
        [FromBody] PasswordResetConfirmRequest request,
        [FromServices] IPasswordResetService passwordResetService,
        [FromServices] IPasswordHasher passwordHasher,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request?.Token))
        {
            return Results.BadRequest(new { message = "Token is required." });
        }

        if (string.IsNullOrWhiteSpace(request?.NewPassword))
        {
            return Results.BadRequest(new { message = "New password is required." });
        }

        var newPasswordHash = passwordHasher.HashPassword(request.NewPassword);
        var (success, error) = await passwordResetService.ConfirmResetAsync(
            request.Token,
            newPasswordHash,
            cancellationToken);

        if (!success)
        {
            return Results.BadRequest(new { message = error ?? "Invalid or expired token." });
        }

        return Results.Ok(new { message = "Password has been reset successfully." });
    }
}

public sealed record PasswordResetRequest(string Email);

public sealed record PasswordResetConfirmRequest(
    string Token,
    string NewPassword);
