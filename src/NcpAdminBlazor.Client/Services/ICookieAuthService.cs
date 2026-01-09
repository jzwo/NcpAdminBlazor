namespace NcpAdminBlazor.Client.Services.Abstract;

/// <summary>
/// Cookie 认证服务，用于在客户端调用服务端的 Cookie 设置端点
/// </summary>
public interface ICookieAuthService
{
    /// <summary>
    /// 将 Token 设置到服务端 Cookie
    /// </summary>
    Task<bool> SetAuthCookieAsync(SetAuthCookieRequest snapshot, CancellationToken cancellationToken = default);

    /// <summary>
    /// 清除服务端的认证 Cookie
    /// </summary>
    Task<bool> ClearAuthCookieAsync(CancellationToken cancellationToken = default);
}