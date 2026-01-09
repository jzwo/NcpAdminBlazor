using System.Net;
using Microsoft.AspNetCore.Components;

namespace NcpAdminBlazor.Client.Infrastructure.Http;

/// <summary>
/// HTTP 消息处理器，用于处理 401 未授权响应并重定向到登录页面
/// </summary>
public class ClientUnauthorizedHandler(NavigationManager navigationManager) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // 1. 发送请求
        var response = await base.SendAsync(request, cancellationToken);

        // 2. 检查响应状态码
        if (response.StatusCode != HttpStatusCode.Unauthorized) return response;
        // 3. 构建跳转 URL (带上当前页面作为 ReturnUrl)
        var returnUrl = Uri.EscapeDataString(navigationManager.Uri);
        var loginUrl = $"/login?returnUrl={returnUrl}";

        // 4. 执行跳转
        // 【关键点】forceLoad: true
        // 必须强制刷新，因为这不仅是页面跳转，还需要让浏览器清除可能残留的错误状态
        // 并在服务器端触发新的认证握手
        navigationManager.NavigateTo(loginUrl, forceLoad: true);

        return response;
    }
}