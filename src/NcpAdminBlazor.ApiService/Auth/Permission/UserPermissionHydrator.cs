using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace NcpAdminBlazor.ApiService.Auth.Permission;

// 动态权限验证
// https://github.com/FastEndpoints/FastEndpoints/issues/594
// 任何动态（按请求）的声明加载都可以通过一个 IClaimTransformation 来实现。这是标准的 ASP.NET 管道内容，并不特定于 FE。
// FE 授权执行依赖于当前用户主体的声明。
// 因此，当实际声明没有嵌入 JWT 令牌本身时，可以使用 IClaimTransformation 来按你希望的方式为用户主体声明赋值。
// 示例：https://gist.github.com/dj-nitehawk/220363f14e649a2cb850d61f9bd793b5
//
// IClaimsTransformation接口作用：
// 在ASP.NET Core中，IClaimsTransformation 接口提供了一种在用户认证之后、授权策略执行之前修改或添加用户声明（Claims）的机制。
// 简单来说，它允许你在用户身份被验证后，但在系统根据这些身份信息判断用户是否有权限执行某个操作之前，对用户的身份信息进行“加工”。
sealed class UserPermissionHydrator(UserPermissionService userPermissionService) : IClaimsTransformation
{
    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        var userId = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        ArgumentNullException.ThrowIfNull(userId);

        // 从缓存读取所有权限代码加载到权限声明列表
        var userPermissions = await userPermissionService.GetPermissionsForUserAsync(userId);
        if (userPermissions.Any())
            principal.AddIdentity(new(userPermissions.Select(p => new Claim("permissions", p))));

        return principal;
    }
}

sealed class UserPermissionService
{
    private readonly string[] _defaultPermissions =
    [
        "pms1",
        "pms2",
        "pms3",
        "user.create",
        "System.Users.Delete"
    ];

    public Task<string[]> GetPermissionsForUserAsync(string userId)
    {
        // fetch the user's permissions from a db or cache here
        var permissions = userId == "123" ? _defaultPermissions : Array.Empty<string>();
        return Task.FromResult(permissions);
    }
}