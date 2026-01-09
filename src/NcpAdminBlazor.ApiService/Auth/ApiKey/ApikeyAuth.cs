using System.Security.Claims;
using System.Security.Principal;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace NcpAdminBlazor.ApiService.Auth.ApiKey;

sealed class ApikeyAuth(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IConfiguration config)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    internal const string SchemeName = "ApiKey";
    internal const string HeaderName = "x-api-key";

    readonly string _apiKey = config["Auth:ApiKey"] ??
                              throw new InvalidOperationException("Api key not set in appsettings.json");

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // 从请求头获取apikey
        Request.Headers.TryGetValue(HeaderName, out var extractedApiKey);
        // 若请求头不存在则从查询参数获取
        if (string.IsNullOrWhiteSpace(extractedApiKey))
            Request.Query.TryGetValue(ApikeyAuth.HeaderName, out extractedApiKey);


        // 通过apikey初始化当前用户
        var user = await InitLoginUserAsync(extractedApiKey);

        if (!IsPublicEndpoint() && user.UserId == Guid.Empty)
            return AuthenticateResult.Fail("Invalid API credentials!");

        // 传递身份信息
        var ticket = CreateTicket(user);
        return AuthenticateResult.Success(ticket);
    }

    private Task<LoginUser> InitLoginUserAsync(StringValues extractedApiKey)
    {
        // 临时代码：后续改成通过apikey从缓存加载用户信息
        var loginUser = extractedApiKey.Equals(_apiKey)
            ? new LoginUser(Guid.NewGuid(), "登录用户")
            : new LoginUser(Guid.Empty, "匿名访客");

        return Task.FromResult(loginUser);
    }

    private AuthenticationTicket CreateTicket(LoginUser user)
    {
        var claims = user.UserId == Guid.Empty
            ? CreateGuestClaims()
            : CreateAuthenticatedUserClaims(user);

        var identity = new ClaimsIdentity(claims, authenticationType: Scheme.Name);
        var principal = new GenericPrincipal(identity, roles: null);
        return new AuthenticationTicket(principal, Scheme.Name);
    }

    private static Claim[] CreateGuestClaims() =>
    [
        new Claim(ClaimTypes.NameIdentifier, Guid.Empty.ToString()),
        new Claim(ClaimTypes.Role, "guest")
    ];

    private static Claim[] CreateAuthenticatedUserClaims(LoginUser user) =>
    [
        new Claim("ClientID", "Default"),
        new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
        new Claim(ClaimTypes.Name, user.UserName),
        new Claim(ClaimTypes.Role, "admin"), // TODO: 临时代码，后续从用户数据加载
        new Claim("permissions", "pms1"),
        new Claim("permissions", "pms2")
    ];

    private bool IsPublicEndpoint()
        => Context.GetEndpoint()?.Metadata.OfType<AllowAnonymousAttribute>().Any() is null or true;
}

/// <summary>
/// 表示已登录用户的不可变数据
/// </summary>
/// <param name="UserId">用户唯一标识</param>
/// <param name="UserName">用户名</param>
internal sealed record LoginUser(Guid UserId, string UserName);
