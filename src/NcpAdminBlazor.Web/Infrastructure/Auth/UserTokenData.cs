namespace NcpAdminBlazor.Web.Infrastructure.Auth;

public record UserTokenData(
    string UserId,
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessTokenExpiresAt,
    DateTimeOffset RefreshTokenExpiresAt);