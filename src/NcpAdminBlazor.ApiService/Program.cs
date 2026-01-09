using System.ClientModel;
using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using FastEndpoints;
using FastEndpoints.ClientGen.Kiota;
using FastEndpoints.Security;
using FastEndpoints.Swagger;
using FluentValidation.AspNetCore;
using Hangfire;
using Hangfire.Redis.StackExchange;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.DevUI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Extensions.AI;
using OpenAI.Chat;
using Kiota.Builder;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using NcpAdminBlazor.ApiService.Auth;
using NcpAdminBlazor.ApiService.Auth.Middlewares;
using NcpAdminBlazor.ApiService.Auth.Permission;
using NcpAdminBlazor.ApiService.Auth.ApiKey;
using NcpAdminBlazor.ApiService.Clients;
using NcpAdminBlazor.ApiService.Extensions;
using NetCorePal.Extensions.CodeAnalysis;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using OpenAI;
using Prometheus;
using Refit;
using Serilog;
using Serilog.Formatting.Json;

// Create a minimal logger for startup
Log.Logger = new LoggerConfiguration()
    .Enrich.WithClientIp()
    .WriteTo.Console( /*new JsonFormatter()*/)
    .CreateLogger();
try
{
    var builder = WebApplication.CreateBuilder(args);

    // Add service defaults & Aspire client integrations.
    builder.AddServiceDefaults();

    // Configure Serilog to send logs to OpenTelemetry when Aspire is enabled
    builder.Host.UseSerilog((context, services, loggerConfiguration) =>
    {
        loggerConfiguration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.WithClientIp()
            .Enrich.FromLogContext();

        var otlpEndpoint = context.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
        if (!string.IsNullOrWhiteSpace(otlpEndpoint))
        {
            // Send logs to OpenTelemetry when OTLP endpoint is configured (Aspire Dashboard)
            loggerConfiguration.WriteTo.OpenTelemetry(options =>
            {
                options.Endpoint = otlpEndpoint;
                // Aspire uses HTTP/Protobuf for logs by default
                var protocol = context.Configuration["OTEL_EXPORTER_OTLP_PROTOCOL"];
                options.Protocol = protocol?.ToLowerInvariant() switch
                {
                    "grpc" => Serilog.Sinks.OpenTelemetry.OtlpProtocol.Grpc,
                    "http/protobuf" => Serilog.Sinks.OpenTelemetry.OtlpProtocol.HttpProtobuf,
                    _ => Serilog.Sinks.OpenTelemetry.OtlpProtocol.HttpProtobuf // Default to HTTP/Protobuf
                };
                options.ResourceAttributes = new Dictionary<string, object>
                {
                    ["service.name"] = context.Configuration["OTEL_SERVICE_NAME"] ??
                                       context.HostingEnvironment.ApplicationName
                };
            });
        }
        else
        {
            // Fallback to console logging when OTLP is not configured
            loggerConfiguration.WriteTo.Console(new JsonFormatter());
        }
    });

    #region SignalR

    builder.Services.AddHealthChecks();
    builder.Services.AddMvc()
        .AddNewtonsoftJson(options => { options.SerializerSettings.AddNetCorePalJsonConverters(); });
    builder.Services.AddSignalR();

    #endregion

    #region Prometheus监控

    builder.Services.AddHealthChecks().ForwardToPrometheus();
    builder.Services.AddHttpClient(Options.DefaultName)
        .UseHttpClientMetrics();

    #endregion

    // Add services to the container.

    #region 身份认证

    // When using Aspire, Redis connection is managed by Aspire and injected automatically
    builder.AddRedisClient("Redis");

    // DataProtection - use custom extension that resolves IConnectionMultiplexer from DI
    builder.Services.AddDataProtection()
        .PersistKeysToStackExchangeRedis("DataProtection-Keys");

    builder.Services.AddScoped<ICurrentUser, CurrentUser>();
    builder.Services.AddTransient<UserTokenService>();
    builder.Services.AddTransient<UserPermissionService>(); // 获取用户权限
    builder.Services.AddTransient<IClaimsTransformation, UserPermissionHydrator>(); // 用户权限验证

    builder.Services
        // 添加Jwt身份认证方案
        .AddAuthenticationJwtBearer(o => o.SigningKey = builder.Configuration["Auth:Jwt:TokenSigningKey"])
        .AddAuthentication(o =>
        {
            o.DefaultAuthenticateScheme = "Jwt_Or_ApiKey";
            o.DefaultChallengeScheme = "Jwt_Or_ApiKey";
        })
        // 添加 ApiKey 身份认证方案
        .AddScheme<AuthenticationSchemeOptions, ApikeyAuth>(ApikeyAuth.SchemeName, null)
        // 综合认证方案（使用jwt或apikey任意一个方案请求endpoint）
        // https://fast-endpoints.com/docs/security#combined-authentication-scheme
        .AddPolicyScheme("Jwt_Or_ApiKey", "Jwt_Or_ApiKey", o =>
        {
            o.ForwardDefaultSelector = ctx =>
            {
                if ((ctx.Request.Headers.TryGetValue(ApikeyAuth.HeaderName, out var apikeyHeader) &&
                     !string.IsNullOrWhiteSpace(apikeyHeader)) ||
                    (ctx.Request.Query.TryGetValue(ApikeyAuth.HeaderName, out apikeyHeader) &&
                     !string.IsNullOrWhiteSpace(apikeyHeader)))
                {
                    return ApikeyAuth.SchemeName;
                }

                if (ctx.Request.Headers.TryGetValue(HeaderNames.Authorization, out var authHeader) &&
                    authHeader.FirstOrDefault()?.StartsWith("Bearer ") is true)
                {
                    return JwtBearerDefaults.AuthenticationScheme;
                }

                return ApikeyAuth.SchemeName;
            };
        });

    builder.Services.AddAuthorization();

    #endregion

    #region Controller

    builder.Services.AddControllers().AddNetCorePalSystemTextJson();
    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();

    #endregion

    #region FastEndpoints

    builder.Services.AddFastEndpoints(o => o.IncludeAbstractValidators = true)
        .SwaggerDocument();
    builder.Services.Configure<JsonOptions>(o =>
        o.SerializerOptions.AddNetCorePalJsonConverters());

    #endregion

    #region 模型验证器

    builder.Services.AddFluentValidationAutoValidation();
    builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
    builder.Services.AddKnownExceptionErrorModelInterceptor();

    #endregion

    #region 基础设施

    builder.Services.AddRepositories(typeof(ApplicationDbContext).Assembly);

    // When using Aspire, database connection is managed by Aspire
    // Use AddDbContext instead of AddMySqlDbContext/AddSqlServerDbContext/AddNpgsqlDbContext
    // to avoid ExecutionStrategy issues with user-initiated transactions
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
    {
        options.UseNpgsql(builder.Configuration.GetConnectionString("PostgreSQL"));
        // 仅在开发环境启用敏感数据日志，防止生产环境泄露敏感信息
        if (builder.Environment.IsDevelopment())
        {
            options.EnableSensitiveDataLogging();
        }

        options.EnableDetailedErrors();
    });
    builder.Services.AddUnitOfWork<ApplicationDbContext>();
    // Redis locks use the Aspire-managed Redis connection
    builder.Services.AddRedisLocks();
    builder.Services.AddContext().AddEnvContext().AddCapContextProcessor();
    builder.Services.AddNetCorePalServiceDiscoveryClient();
    builder.Services.AddIntegrationEvents(typeof(Program))
        .UseCap<ApplicationDbContext>(b =>
        {
            b.RegisterServicesFromAssemblies(typeof(Program));
            b.AddContextIntegrationFilters();
        });


    builder.Services.AddCap(x =>
    {
        x.UseNetCorePalStorage<ApplicationDbContext>();
        x.JsonSerializerOptions.AddNetCorePalJsonConverters();
        // When using Aspire, RabbitMQ connection is managed by Aspire
        x.UseRabbitMQ(p =>
        {
            var connectionString = builder.Configuration.GetConnectionString("rabbitmq");
            if (!string.IsNullOrEmpty(connectionString))
            {
                // Parse Aspire-provided connection string
                var uri = new Uri(connectionString);
                p.HostName = uri.Host;
                p.Port = uri.Port;
                if (!string.IsNullOrEmpty(uri.UserInfo))
                {
                    var userInfo = uri.UserInfo.Split(':');
                    p.UserName = userInfo[0];
                    if (userInfo.Length > 1)
                    {
                        p.Password = userInfo[1];
                    }
                }

                if (!string.IsNullOrEmpty(uri.AbsolutePath) && uri.AbsolutePath != "/")
                {
                    p.VirtualHost = uri.AbsolutePath.TrimStart('/');
                }
            }
            else
            {
                builder.Configuration.GetSection("RabbitMQ").Bind(p);
            }
        });
        x.UseDashboard(); //CAP Dashboard  path：  /cap
    });

    builder.Services.AddUtilsInfrastructure();
    #endregion

    builder.Services.AddMediatR(cfg =>
        cfg.RegisterServicesFromAssemblies(Assembly.GetExecutingAssembly())
            .AddCommandLockBehavior()
            .AddKnownExceptionValidationBehavior()
            .AddUnitOfWorkBehaviors());

    #region 多环境支持与服务注册发现

    builder.Services.AddMultiEnv(envOption => envOption.ServiceName = "Abc.Template")
        .UseMicrosoftServiceDiscovery();
    builder.Services.AddConfigurationServiceEndpointProvider();

    #endregion

    #region 远程服务客户端配置

    var jsonSerializerSettings = new JsonSerializerSettings
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        NullValueHandling = NullValueHandling.Ignore,
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore
    };
    jsonSerializerSettings.AddNetCorePalJsonConverters();
    var ser = new NewtonsoftJsonContentSerializer(jsonSerializerSettings);
    var settings = new RefitSettings(ser);
    builder.Services.AddRefitClient<IUserServiceClient>(settings)
        .ConfigureHttpClient(client =>
            client.BaseAddress = new Uri(builder.Configuration.GetValue<string>("https+http://user:8080")!))
        .AddMultiEnvMicrosoftServiceDiscovery() //多环境服务发现支持
        .AddStandardResilienceHandler(); //添加标准的重试策略

    #endregion

    #region Jobs

    // this flag indicates whether we are generating API clients,
    // when true, we use InMemory database and skip some services that are not needed for client generation
    var isGenerateClients = args.Contains("--generateclients");
    // When using Aspire, Redis connection is managed by Aspire
    if (!isGenerateClients)
    {
        builder.Services.AddHangfire(x => { x.UseRedisStorage(builder.Configuration.GetConnectionString("Redis")); });
        builder.Services.AddHangfireServer(); //hangfire dashboard  path：  /hangfire
    }

    #endregion

    #region AI Agent

    var openAiApiKey = builder.Configuration["OpenAI:Key"]
                       ?? throw new InvalidOperationException("Missing configuration:OpenAI:Key");
    var openAiModel = builder.Configuration["OpenAI:Model"] ?? "gpt-4o-mini";
    var openAiEndpoint = builder.Configuration["OpenAI:Endpoint"];
    var openAiOptions = new OpenAIClientOptions();
    if (!string.IsNullOrWhiteSpace(openAiEndpoint)) openAiOptions.Endpoint = new Uri(openAiEndpoint);
    var chatClient = new ChatClient(openAiModel, new ApiKeyCredential(openAiApiKey), openAiOptions).AsIChatClient();
    builder.Services.AddChatClient(chatClient);

    builder.AddAIAgent("systemAssister", (sp, key) => new ChatClientAgent(
        chatClient,
        name: key,
        instructions:
        "You assist users with system-related inquiries and tasks, providing accurate and helpful information.",
        tools: [AIFunctionFactory.Create(FormatStory)]
    ));

    // Register services for OpenAI responses and conversations (also required for DevUI)
    builder.Services.AddOpenAIResponses();
    builder.Services.AddOpenAIConversations();

    #endregion

    var app = builder.Build();

    app.UseKnownExceptionHandler();
    // Configure the HTTP request pipeline.
    app.UseStaticFiles();
    app.UseHttpsRedirection();
    app.UseRouting();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseMiddleware<CurrentUserMiddleware>();

    app.MapControllers();
    app.UseFastEndpoints(c => c.Binding.UseDefaultValuesForNullableProps = false);
    if (app.Environment.IsDevelopment())
    {
        app.UseSwaggerGen(); //add this
        // Map DevUI endpoint to /devui
        app.MapDevUI();
    }

    // Map endpoints for OpenAI responses and conversations (also required for DevUI)
    app.MapOpenAIResponses();
    app.MapOpenAIConversations();

    await app.GenerateApiClientsAndExitAsync(c =>
    {
        c.SwaggerDocumentName = "v1"; //must match doc name above
        c.Language = GenerationLanguage.CSharp;
        c.OutputPath = "../NcpAdminBlazor.Client/HttpClient";
        c.ClientNamespaceName = "NcpAdminBlazor.Client";
        c.ClientClassName = "ApiClient";
        // c.CreateZipArchive = true; //if you'd like a zip file as well
    });

    // Code analysis endpoint
    app.MapGet("/code-analysis", () =>
    {
        var html = VisualizationHtmlBuilder.GenerateVisualizationHtml(
            CodeFlowAnalysisHelper.GetResultFromAssemblies(typeof(Program).Assembly,
                typeof(ApplicationDbContext).Assembly,
                typeof(NcpAdminBlazor.Domain.AggregatesModel.OrderAggregate.Order).Assembly)
        );
        return Results.Content(html, "text/html; charset=utf-8");
    });

    #region SignalR

    app.MapHub<NcpAdminBlazor.ApiService.Application.Hubs.ChatHub>("/chat");

    #endregion

    app.UseHttpMetrics();
    app.MapMetrics(); // 通过   /metrics  访问指标
    app.MapDefaultEndpoints();
    app.UseHangfireDashboard();

    // seed initial data
    await app.Services.SeedAsync();

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}

[Description("Formats the story for publication, revealing its title.")]
static string FormatStory(string title, string story) => $"""
                                                          **Title**: {title}

                                                          {story}
                                                          """;

#pragma warning disable S1118
namespace NcpAdminBlazor.ApiService
{
    // ReSharper disable once ClassNeverInstantiated.Global
    // ReSharper disable once PartialTypeWithSinglePart
    public partial class Program
#pragma warning restore S1118
    {
    }
}