using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Kiota.Abstractions.Authentication;
using MudBlazor;
using MudBlazor.Services;
using NcpAdminBlazor.Client.Extensions;
using NcpAdminBlazor.Client.HttpClientServices;
using NcpAdminBlazor.Client.Services.Abstract;

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
builder.Services.AddKiotaClient(builder.HostEnvironment.BaseAddress,
    services => { services.AddScoped<IAuthenticationProvider, AnonymousAuthenticationProvider>(); });


builder.Services.AddClientLocalization();
builder.Services.AddClientServices();

var host = builder.Build();

await host.SetCulture();

await host.RunAsync();