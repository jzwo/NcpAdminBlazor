using Microsoft.AspNetCore.Authentication.Cookies;
using NcpAdminBlazor.Web.Infrastructure.Auth;

namespace NcpAdminBlazor.Web.Extensions;

/// <summary>
/// Cookie 认证服务扩展方法
/// 配置基于 Cookie 的认证，并支持自动刷新 Access Token
/// 基于 ASP.NET Core OpenID Connect cookie refresh 模式实现
/// </summary>
public static class CookieAuthenticationExtensions
{
    /// <summary>
    /// 添加 Cookie 认证服务，支持自动刷新 Token
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合（用于链式调用）</returns>
    public static IServiceCollection AddCookieAuthenticationWithRefresh(this IServiceCollection services)
    {
        services.AddScoped<IUserTokenStore, UserTokenStore>();
        services.AddScoped<IUserTokenRefresher, UserTokenRefresher>();

        // 注册 Cookie 刷新服务
        services.AddTransient<CookieEvents>();

        // 添加 Cookie 认证方案
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.EventsType = typeof(CookieEvents);

                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Strict;

                // 路径配置
                options.LoginPath = "/account/login"; // 未登录时跳转地址
                // options.AccessDeniedPath = "/access-denied"; // 403 禁止访问地址
                
                //【关键】API 请求不跳转 (AJAX/Fetch 优化)
                // 默认情况下，未登录访问接口会返回 302 跳转到 /login。
                // 对于 Blazor/API 调用，这会导致 fetch 报错或重定向循环。
                // 我们需要让它返回 401，让前端拦截器去处理跳转。
                options.Events.OnRedirectToLogin = context =>
                {
                    // 判断是否是 API 请求 (根据路径或请求头)
                    if (IsApiRequest(context.Request))
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        return Task.CompletedTask;
                    }

                    // 普通页面请求，执行默认跳转
                    context.Response.Redirect(context.RedirectUri);
                    return Task.CompletedTask;
                };

                // 同理处理 403 禁止访问
                options.Events.OnRedirectToAccessDenied = context =>
                {
                    if (IsApiRequest(context.Request))
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        return Task.CompletedTask;
                    }

                    context.Response.Redirect(context.RedirectUri);
                    return Task.CompletedTask;
                };
            });
        return services;
    }

    // 辅助方法：判断是否 API 请求
    private static bool IsApiRequest(HttpRequest request)
    {
        return request.Path.StartsWithSegments("/api");
    }
}