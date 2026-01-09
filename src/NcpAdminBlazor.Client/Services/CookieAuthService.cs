using System.Net.Http.Json;
using NcpAdminBlazor.Client.Shared;

namespace NcpAdminBlazor.Client.Services;

/// <summary>
/// Cookie 认证服务的 WASM 客户端实现
/// 通过 HTTP 请求调用 BFF 端点来设置和清除认证 Cookie
/// </summary>
public sealed class CookieAuthService(HttpClient httpClient) : ICookieAuthService
{
    private const string LoginEndpoint = "/bff-api/auth/login";
    private const string LogoutEndpoint = "/bff-api/auth/logout";

    public async Task<bool> SetAuthCookieAsync(
        SetAuthCookieRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync(LoginEndpoint, request, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException)
        {
            return false;
        }
    }

    public async Task<bool> ClearAuthCookieAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.PostAsync(LogoutEndpoint, null, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException)
        {
            return false;
        }
    }
}

