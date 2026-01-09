using Microsoft.JSInterop;
using NcpAdminBlazor.Client.Services;
using NcpAdminBlazor.Client.Services.Abstract;

namespace NcpAdminBlazor.Web.Services;

/// <summary>
/// 基于 JavaScript Interop 的 Cookie 认证服务
/// 用于在 SSR 场景下通过浏览器 JavaScript 发起 HTTP 请求来设置和清除认证 Cookie
/// </summary>
public sealed class JsCookieAuthService(
    IJSRuntime jsRuntime,
    ILogger<JsCookieAuthService> logger)
    : ICookieAuthService, IAsyncDisposable
{
    private const string JsModulePath = "./js/cookieAuth.js";
    private const string SetAuthCookieFunction = "setAuthCookie";
    private const string ClearAuthCookieFunction = "clearAuthCookie";

    private readonly Lazy<Task<IJSObjectReference>> _moduleTask = new(
        () => jsRuntime.InvokeAsync<IJSObjectReference>("import", JsModulePath).AsTask());

    /// <inheritdoc />
    public async Task<bool> SetAuthCookieAsync(
        SetAuthCookieRequest snapshot,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var module = await _moduleTask.Value;
            return await module.InvokeAsync<bool>(
                SetAuthCookieFunction,
                cancellationToken,
                snapshot.AccessToken,
                snapshot.RefreshToken,
                snapshot.AccessTokenExpiry.ToString("O"),
                snapshot.RefreshTokenExpiry.ToString("O"),
                snapshot.UserId);
        }
        catch (JSException ex)
        {
            logger.LogError(ex, "Error setting authentication cookie via JavaScript");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> ClearAuthCookieAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var module = await _moduleTask.Value;
            return await module.InvokeAsync<bool>(ClearAuthCookieFunction, cancellationToken);
        }
        catch (JSException ex)
        {
            logger.LogError(ex, "Error clearing authentication cookie via JavaScript");
            return false;
        }
    }

    /// <summary>
    /// 释放 JavaScript 模块资源
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_moduleTask.IsValueCreated)
        {
            var module = await _moduleTask.Value;
            await module.DisposeAsync();
        }
    }
}