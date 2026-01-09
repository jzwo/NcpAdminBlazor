using System.Net.Http.Headers;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;
using Microsoft.Kiota.Abstractions.Authentication;
using MudBlazor;
using MudBlazor.Services;
using NcpAdminBlazor.Client.Extensions;
using NcpAdminBlazor.Client.Infrastructure.ApiProxies;
using NcpAdminBlazor.Web.Components;
using NcpAdminBlazor.Web.Endpoints;
using NcpAdminBlazor.Web.Extensions;
using NcpAdminBlazor.Client.Services;
using NcpAdminBlazor.Client.Shared;
using NcpAdminBlazor.Web;
using NcpAdminBlazor.Web.Infrastructure.Http;
using NcpAdminBlazor.Web.Middleware;
using NcpAdminBlazor.Web.Options;
using NcpAdminBlazor.Web.Services;
using Yarp.ReverseProxy.Transforms;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// 配置选项
builder.Services.AddOptions<ApiServiceOptions>()
    .BindConfiguration(ApiServiceOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Add MudBlazor services
builder.Services.AddMudServices();
builder.Services.AddMudMarkdownServices();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents()
    .AddAuthenticationStateSerialization();

builder.Services.AddHybridCache();

// 添加认证服务
builder.Services.AddCookieAuthenticationWithRefresh();
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<ICookieAuthService, JsCookieAuthService>();

const string apiServiceAddress = "https+http://apiservice";

// 注册服务端 HTTP 消息处理器
builder.Services.AddScoped<ServerUnauthorizedHandler>();

// 添加 Kiota API 客户端
builder.Services.AddKiotaClient(
    apiServiceAddress,
    services =>
    {
        services.AddScoped<IAccessTokenProvider, KiotaAccessTokenProvider>();
        services.AddScoped<BaseBearerTokenAuthenticationProvider>();
        services.AddScoped<IAuthenticationProvider, BearerTokenAuthenticationProvider>();
    },
    clientBuilder =>
    {
        // 挂载服务端 401 未授权处理器
        // 用于在 Blazor Server 交互模式下处理 API 401 响应
        // 注意：YARP 转发的请求由 TokenCleanupMiddleware 处理
        clientBuilder.AddHttpMessageHandler<ServerUnauthorizedHandler>();
    });

builder.Services.AddHttpClient<AiChatService>(client => { client.BaseAddress = new Uri(apiServiceAddress); });

builder.Services.AddClientLocalization();
builder.Services.AddClientServices();

builder.Services.AddOutputCache();

builder.Services.AddHttpForwarder();
builder.Services.AddHttpForwarderWithServiceDiscovery();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.UseAuthentication();

app.UseAuthorization();

app.UseOutputCache();
app.MapStaticAssets();

string[] supportedCultures = ["zh-CN", "en-US"];
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture(supportedCultures[0])
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);

app.UseRequestLocalization(localizationOptions);

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(NcpAdminBlazor.Client._Imports).Assembly)
    .AllowAnonymous();

app.MapDefaultEndpoints();

// 映射认证端点
app.MapAuthEndpoints();

// 【Token 清理中间件】在认证之后，YARP之前
// 用于处理 401 响应，自动清除过期的 Token 和 Cookie
app.UseMiddleware<TokenCleanupMiddleware>();
// 映射 API 转发
app.MapForwarder("/api/{**catch-all}", apiServiceAddress, transformBuilder =>
        {
            transformBuilder.AddRequestTransform(transformContext =>
            {
                if (transformContext.HttpContext.Items.TryGetValue(WebConstants.HttpContextItems.AccessToken,
                        out var tokenObj)
                    && tokenObj is string token)
                {
                    transformContext.ProxyRequest.Headers.Authorization =
                        new AuthenticationHeaderValue("Bearer", token);
                }

                return ValueTask.CompletedTask;
            });
        }
    )
    .RequireAuthorization(policyBuilder =>
    {
        policyBuilder.RequireAssertion(context =>
        {
            if (context.Resource is not HttpContext httpContext)
                return context.User.Identity?.IsAuthenticated == true;

            var option = httpContext.RequestServices.GetRequiredService<IOptions<ApiServiceOptions>>().Value;
            // 认证端点允许匿名访问
            if (httpContext.Request.Path.StartsWithSegments(option.AuthPathPrefix))
                return true;

            return context.User.Identity?.IsAuthenticated == true;
        });
    });

app.MapGet("/Culture/Set", (string? culture, string redirectUri, HttpContext httpContext) =>
{
    if (!string.IsNullOrEmpty(culture))
    {
        httpContext.Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(
                new RequestCulture(culture, culture)));
    }

    return Results.LocalRedirect(redirectUri);
});

await app.RunAsync();