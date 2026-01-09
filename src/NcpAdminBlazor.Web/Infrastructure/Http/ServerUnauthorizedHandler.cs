using System.Net;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using NcpAdminBlazor.Web.Infrastructure.Auth;

namespace NcpAdminBlazor.Web.Infrastructure.Http;

/// <summary>
/// 服务端 HTTP 消息处理器，用于处理 401 未授权响应
/// 负责清除服务端 Token 存储并重定向到登录页面
/// </summary>
public class ServerUnauthorizedHandler(
    NavigationManager navigationManager,
    IUserTokenStore tokenStore,
    AuthenticationStateProvider authStateProvider)
    : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // 1. 发送请求给后端 API
        var response = await base.SendAsync(request, cancellationToken);

        // 2. 如果后端不是返回 401 Unauthorized ，则直接返回响应
        if (response.StatusCode != HttpStatusCode.Unauthorized) return response;
        // 3. 获取当前用户
        // 注意：在 Server 模式下，使用 AuthenticationStateProvider 获取用户状态是最稳健的
        var authState = await authStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        // 4. 【关键步骤】清除服务端存储的 Token
        if (user.Identity?.IsAuthenticated == true)
        {
            await tokenStore.ClearTokenAsync(user);
        }

        // 5. 强制跳转到登录页
        // forceLoad: true 是必须的。
        // 它会强制浏览器发起一个新的 HTTP GET 请求访问 /login，
        // 从而触发 CookieAuthenticationMiddleware 的 ValidatePrincipal 逻辑，完成最终的"拒绝"过程。
        var returnUrl = Uri.EscapeDataString(navigationManager.Uri);
        navigationManager.NavigateTo($"/login?returnUrl={returnUrl}", forceLoad: true);

        return response;
    }
}