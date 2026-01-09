using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Kiota.Abstractions.Authentication;
using MudBlazor;
using MudBlazor.Services;
using NcpAdminBlazor.Client.Extensions;
using NcpAdminBlazor.Client.Infrastructure.ApiProxies;
using NcpAdminBlazor.Client.Infrastructure.Http;
using NcpAdminBlazor.Client.Shared;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddMudServices();
builder.Services.AddMudMarkdownServices();

builder.Services.AddAuthorizationCore();
builder.Services.AddAuthenticationStateDeserialization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<ICookieAuthService, CookieAuthService>();

builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddHttpClient<AiChatService>(client =>
    client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress));

// 注册自定义 HTTP 消息处理器
builder.Services.AddScoped<ClientUnauthorizedHandler>();

// 添加 Kiota API 客户端
builder.Services.AddKiotaClient(
    builder.HostEnvironment.BaseAddress,
    services => { services.AddScoped<IAuthenticationProvider, AnonymousAuthenticationProvider>(); },
    clientBuilder =>
    {
        // 挂载 401 未授权处理器
        clientBuilder.AddHttpMessageHandler<ClientUnauthorizedHandler>();
    });


builder.Services.AddClientLocalization();
builder.Services.AddClientServices();

var host = builder.Build();

await host.SetCulture();

await host.RunAsync();