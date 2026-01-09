using Microsoft.AspNetCore.Authentication.Cookies;

namespace NcpAdminBlazor.Web.Infrastructure.Auth;

public class CookieEvents(IUserTokenStore store) : CookieAuthenticationEvents
{
    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        var path = context.Request.Path;
        if (IsStaticResource(path))
        {
            return;
        }

        if (context.Principal?.Identity?.IsAuthenticated != true)
        {
            return;
        }

        var token = await store.GetTokenAsync(context.Principal);
        if (token is null)
        {
            context.RejectPrincipal();
            return;
        }

        context.HttpContext.Items[WebConstants.HttpContextItems.AccessToken] = token;

        await base.ValidatePrincipal(context);
    }

    public override async Task SigningOut(CookieSigningOutContext context)
    {
        await store.ClearTokenAsync(context.HttpContext.User);
        await base.SigningOut(context);
    }


    // 辅助方法：判断是否是静态资源
    private static bool IsStaticResource(PathString path)
    {
        // 1. Blazor 框架文件
        if (path.StartsWithSegments("/_framework") ||
            path.StartsWithSegments("/_content"))
            return true;

        // 2. 常见静态扩展名
        // 也可以检查 context.Request.Headers["Accept"] 是否包含 text/html
        var value = path.Value ?? string.Empty;
        return value.EndsWith(".css", StringComparison.OrdinalIgnoreCase) ||
               value.EndsWith(".js", StringComparison.OrdinalIgnoreCase);
    }
}