using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using NcpAdminBlazor.Web.Infrastructure.Auth;

namespace NcpAdminBlazor.Web.Middleware;

/// <summary>
/// Token 清理中间件
/// 当后端 API 返回 401 未授权时，自动清除 Redis 中的脏 Token 和浏览器的 Cookie
/// 确保下次请求能够正确触发认证流程
/// </summary>
public class TokenCleanupMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IUserTokenStore tokenStore)
    {
        // 1. 先执行后续管道（也就是去转发请求调用后端 API）
        await next(context);

        // 2. 请求回来后，检查状态码
        // 如果后端 API 返回了 401 Unauthorized
        if (context.Response.StatusCode == StatusCodes.Status401Unauthorized)
        {
            // 获取当前用户（可能已经过期或无效）
            var user = context.User;

            if (user.Identity?.IsAuthenticated == true)
            {
                // 【动作 A】清除 Redis 中的脏 Token
                // 这样下次即便有 Cookie，BFF 查 Redis 也会返回 null，直接拦截，不再骚扰后端
                await tokenStore.ClearTokenAsync(user);

                // 【动作 B】清除浏览器的 Cookie
                // 这一步非常关键！它会向响应头添加 Set-Cookie: ...; Expires=Thu, 01 Jan 1970
                // 浏览器收到这个响应后，会立即丢弃 Cookie
                await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
        }
    }
}