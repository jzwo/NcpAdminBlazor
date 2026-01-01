using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor;
using MudBlazor.Services;
using NcpAdminBlazor.Client.Extensions;
using NcpAdminBlazor.Client.HttpClientServices;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddMudServices();
builder.Services.AddMudMarkdownServices();

builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<AiChatService>(sp => new AiChatService(sp.GetRequiredService<HttpClient>()));
builder.Services.AddKiotaClient();

builder.Services.AddAuthenticationAndLocalization();
builder.Services.AddClientServices();

var host = builder.Build();

await host.SetCulture();

await host.RunAsync();