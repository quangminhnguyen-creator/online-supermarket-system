using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineSupermarket.Api.Contracts.Auth;
using OnlineSupermarket.Domain.Identity;
using OnlineSupermarket.Infrastructure.Identity;
using OnlineSupermarket.Infrastructure.Persistence;

namespace OnlineSupermarket.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/register", RegisterAsync)
            .WithName("Register")
            .Produces<RegisterResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/login", LoginAsync)
            .WithName("Login")
            .Produces<AuthResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/refresh", RefreshTokenAsync)
            .WithName("RefreshToken")
            .Produces<AuthResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/logout", LogoutAsync)
            .WithName("Logout")
            .Produces(StatusCodes.Status200OK);

        group.MapGet("/me", GetMeAsync)
            .WithName("GetMe")
            .RequireAuthorization()
            .Produces<UserDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return routes;
    }

    private static async Task<IResult> RegisterAsync(
        [FromBody] RegisterRequest request,
        [FromServices] AppDbContext dbContext,
        [FromServices] IPasswordHasher passwordHasher,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password) ||
            string.IsNullOrWhiteSpace(request.FullName))
        {
            return Results.BadRequest(new { message = "Email, password, and full name are required." });
        }

        if (request.Password.Length < 6)
        {
            return Results.BadRequest(new { message = "Password must be at least 6 characters long." });
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var emailExists = await dbContext.Users
            .AnyAsync(u => u.Email == normalizedEmail, cancellationToken);

        if (emailExists)
        {
            return Results.Conflict(new { message = "Email is already registered." });
        }

        var passwordHash = passwordHasher.HashPassword(request.Password);
        User user;
        try
        {
            user = User.Create(normalizedEmail, passwordHash, request.FullName, request.Phone, UserRole.Customer);
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new RegisterResponse(
            user.Id,
            user.Email,
            user.FullName,
            user.Phone,
            user.Role.ToString());

        return Results.Created($"/api/auth/me", response);
    }

    private static async Task<IResult> LoginAsync(
        [FromBody] LoginRequest request,
        [FromServices] AppDbContext dbContext,
        [FromServices] IPasswordHasher passwordHasher,
        [FromServices] ITokenService tokenService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Results.BadRequest(new { message = "Email and password are required." });
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await dbContext.Users
            .SingleOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        if (user == null || user.Status != UserStatus.Active)
        {
            return Results.Unauthorized();
        }

        var isPasswordValid = passwordHasher.VerifyPassword(user.PasswordHash, request.Password);
        if (!isPasswordValid)
        {
            return Results.Unauthorized();
        }

        var accessToken = tokenService.GenerateAccessToken(user);
        var (rawRefreshToken, tokenHash, expiresAtUtc) = tokenService.GenerateRefreshToken();

        var refreshTokenEntity = RefreshToken.Issue(user.Id, tokenHash, expiresAtUtc);
        dbContext.RefreshTokens.Add(refreshTokenEntity);
        await dbContext.SaveChangesAsync(cancellationToken);

        var userDto = new UserDto(user.Id, user.Email, user.FullName, user.Phone, user.Role.ToString());
        var response = new AuthResponse(
            accessToken,
            rawRefreshToken,
            tokenService.AccessTokenExpirationMinutes * 60,
            userDto);

        return Results.Ok(response);
    }

    private static async Task<IResult> RefreshTokenAsync(
        [FromBody] RefreshTokenRequest request,
        [FromServices] AppDbContext dbContext,
        [FromServices] ITokenService tokenService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return Results.BadRequest(new { message = "RefreshToken is required." });
        }

        var tokenHash = tokenService.HashRefreshToken(request.RefreshToken);
        var token = await dbContext.RefreshTokens
            .SingleOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (token == null)
        {
            return Results.Unauthorized();
        }

        if (token.IsRevoked)
        {
            // Reuse detection: revoke all active refresh tokens of this user
            var activeTokens = await dbContext.RefreshTokens
                .Where(t => t.UserId == token.UserId && t.RevokedAtUtc == null)
                .ToListAsync(cancellationToken);

            var now = DateTime.UtcNow;
            foreach (var t in activeTokens)
            {
                t.Revoke(now);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            return Results.Unauthorized();
        }

        if (token.IsExpired)
        {
            return Results.Unauthorized();
        }

        var user = await dbContext.Users
            .FindAsync([token.UserId], cancellationToken);

        if (user == null || user.Status != UserStatus.Active)
        {
            return Results.Unauthorized();
        }

        var newAccessToken = tokenService.GenerateAccessToken(user);
        var (newRawRefreshToken, newTokenHash, newExpiresAtUtc) = tokenService.GenerateRefreshToken();

        var newToken = RefreshToken.Issue(user.Id, newTokenHash, newExpiresAtUtc);
        token.Revoke(DateTime.UtcNow, newToken.Id);

        dbContext.RefreshTokens.Add(newToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var userDto = new UserDto(user.Id, user.Email, user.FullName, user.Phone, user.Role.ToString());
        var response = new AuthResponse(
            newAccessToken,
            newRawRefreshToken,
            tokenService.AccessTokenExpirationMinutes * 60,
            userDto);

        return Results.Ok(response);
    }

    private static async Task<IResult> LogoutAsync(
        [FromBody] LogoutRequest? request,
        [FromServices] AppDbContext dbContext,
        [FromServices] ITokenService tokenService,
        CancellationToken cancellationToken)
    {
        if (request != null && !string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            var tokenHash = tokenService.HashRefreshToken(request.RefreshToken);
            var token = await dbContext.RefreshTokens
                .SingleOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

            if (token != null && !token.IsRevoked)
            {
                token.Revoke(DateTime.UtcNow);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        return Results.Ok(new { message = "Logged out successfully." });
    }

    private static async Task<IResult> GetMeAsync(
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

        var user = await dbContext.Users
            .FindAsync([userId], cancellationToken);

        if (user == null)
        {
            return Results.NotFound();
        }

        var userDto = new UserDto(user.Id, user.Email, user.FullName, user.Phone, user.Role.ToString());
        return Results.Ok(userDto);
    }
}
