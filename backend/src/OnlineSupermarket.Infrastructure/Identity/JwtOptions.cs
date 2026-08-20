namespace OnlineSupermarket.Infrastructure.Identity;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string SecretKey { get; set; } = "online-supermarket-system-default-secret-key-at-least-32-chars-long!";
    public string Issuer { get; set; } = "OnlineSupermarket";
    public string Audience { get; set; } = "OnlineSupermarket.Client";
    public int AccessTokenExpirationMinutes { get; set; } = 15;
    public int RefreshTokenExpirationDays { get; set; } = 7;
}
