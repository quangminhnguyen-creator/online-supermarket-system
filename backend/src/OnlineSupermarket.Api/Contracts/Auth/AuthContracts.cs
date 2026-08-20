namespace OnlineSupermarket.Api.Contracts.Auth;

public sealed record RegisterRequest(
    string Email,
    string Password,
    string FullName,
    string? Phone);

public sealed record RegisterResponse(
    Guid Id,
    string Email,
    string FullName,
    string? Phone,
    string Role);

public sealed record LoginRequest(
    string Email,
    string Password);

public sealed record UserDto(
    Guid Id,
    string Email,
    string FullName,
    string? Phone,
    string Role);

public sealed record AuthResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresInSeconds,
    UserDto User);

public sealed record RefreshTokenRequest(
    string RefreshToken);

public sealed record LogoutRequest(
    string? RefreshToken);
