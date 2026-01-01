using Microsoft.AspNetCore.Localization;
using MudBlazor;
using MudBlazor.Services;
using NcpAdminBlazor.Client.Extensions;
using NcpAdminBlazor.Web.Components;
using NcpAdminBlazor.Web.MockApi;
using NcpAdminBlazor.Client.HttpClientServices;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add MudBlazor services
builder.Services.AddMudServices();
builder.Services.AddMudMarkdownServices();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

// 配置 Demo 模式
var demoModeEnabled = builder.Configuration.GetValue<bool>("DemoMode:Enabled");
if (!demoModeEnabled)
{
    // 生产模式：使用真实的 API 服务
    builder.Services.AddHttpClient("ApiService", client => { client.BaseAddress = new("https+http://apiservice"); });
}

builder.Services.AddScoped<HttpClient>(sp =>
{
    if (demoModeEnabled)
    {
        // Demo 模式：使用本地 HttpClient
        var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
        var request = httpContextAccessor.HttpContext?.Request;
        var baseAddress = $"{request?.Scheme}://{request?.Host}";
        return new HttpClient { BaseAddress = new Uri(baseAddress) };
    }

    // 生产模式：使用 API 服务
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    return factory.CreateClient("ApiService");
});

builder.Services.AddScoped<AiChatService>(sp => new AiChatService(sp.GetRequiredService<HttpClient>()));

// 注册 Mock 数据存储（仅在 Demo 模式下使用）
if (demoModeEnabled)
{
    builder.Services.AddSingleton<MockDataStore>();
}

builder.Services.AddKiotaClient();

builder.Services.AddAuthenticationAndLocalization();
builder.Services.AddClientServices();

builder.Services.AddOutputCache();

builder.Services.AddHttpForwarder();
builder.Services.AddHttpContextAccessor();

// // OpenAI Configuration
// if (builder.Configuration["OpenAI:Key"] is { } openAiKey) 
// {
//     var openAIClient = new OpenAI.OpenAIClient(
//         new System.ClientModel.ApiKeyCredential(openAiKey), 
//         new OpenAI.OpenAIClientOptions
//         {
//             Endpoint = builder.Configuration["OpenAI:Endpoint"] is string endpoint ? new Uri(endpoint) : null
//         });
//
// #pragma warning disable OPENAI001 // GetOpenAIResponseClient(string) is experimental and subject to change or removal in future updates.
//     var chatClient = openAIClient.GetOpenAIResponseClient("ep-20251230124203-vccgb").AsIChatClient();
// #pragma warning restore OPENAI001
//
//     var embeddingGenerator = openAIClient.GetEmbeddingClient("ep-20251230122250-8gbc7").AsIEmbeddingGenerator();
//
//     var vectorStorePath = Path.Combine(AppContext.BaseDirectory, "vector-store.db");
//     var vectorStoreConnectionString = $"Data Source={vectorStorePath}";
//     builder.Services.AddSqliteVectorStore(_ => vectorStoreConnectionString);
//     builder.Services.AddSqliteCollection<string, IngestedChunk>(IngestedChunk.CollectionName, vectorStoreConnectionString);
//
//     builder.Services.AddSingleton<DataIngestor>();
//     builder.Services.AddSingleton<SemanticSearch>();
//     builder.Services.AddKeyedSingleton("ingestion_directory",
//         new DirectoryInfo(Path.Combine(builder.Environment.WebRootPath, "Data")));
//     builder.Services.AddChatClient(chatClient).UseFunctionInvocation().UseLogging();
//     builder.Services.AddEmbeddingGenerator(embeddingGenerator);
// }


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

// 根据 Demo 模式配置 API 路由
if (demoModeEnabled)
{
    // Demo 模式：使用 Mock APIs
    var mockDataStore = app.Services.GetRequiredService<MockDataStore>();
    var apiGroup = app.MapGroup("");
    apiGroup.MapUsersManagementMockApis(mockDataStore);
    apiGroup.MapRolesManagementMockApis(mockDataStore);
}
else
{
    // 生产模式：转发请求到 API 服务
    app.MapForwarder("/api/{**catch-all}", "http+https://apiservice/");
}

app.UseHttpsRedirection();

app.UseAntiforgery();

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