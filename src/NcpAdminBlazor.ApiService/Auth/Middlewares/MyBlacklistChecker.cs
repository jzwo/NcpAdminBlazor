using FastEndpoints.Security;

namespace NcpAdminBlazor.ApiService.Auth.Middlewares;

// JWT 令牌注销
// 使用提供的抽象中间件类可以轻松实现令牌吊销。重写 JwtTokenIsValidAsync() 方法，
// 并在检查数据库或吊销令牌缓存后，如果提供的令牌不再有效，则返回 false。
public class MyBlacklistChecker(RequestDelegate next) : JwtRevocationMiddleware(next)
{
    protected override Task<bool> JwtTokenIsValidAsync(string jwtToken, CancellationToken ct)
    {
        // return true if the supplied token is still valid
        return Task.FromResult(true);
    }
}