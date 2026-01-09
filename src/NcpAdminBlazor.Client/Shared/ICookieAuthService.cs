namespace NcpAdminBlazor.Client.Shared;

/// <summary>
/// Cookie 认证服务，用于在客户端调用服务端的 Cookie 设置端点
/// </summary>
public interface ICookieAuthService
{
    /// <summary>
    /// 将 Token 设置到服务端 Cookie
    /// </summary>
    Task<bool> SetAuthCookieAsync(SetAuthCookieRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 清除服务端的认证 Cookie
    /// </summary>
    Task<bool> ClearAuthCookieAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// 设置认证 Cookie 的请求模型
/// </summary>
public record SetAuthCookieRequest(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessTokenExpiry,
    DateTimeOffset RefreshTokenExpiry,
    string UserId);