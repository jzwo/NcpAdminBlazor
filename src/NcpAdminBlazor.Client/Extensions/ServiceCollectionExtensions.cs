using Blazored.LocalStorage;
using MudBlazor.Translations;
using NcpAdminBlazor.Client.Infrastructure.Http;
using NcpAdminBlazor.Client.Stores;

namespace NcpAdminBlazor.Client.Extensions;

/// <summary>
/// 服务集合扩展方法
/// </summary>
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// 添加客户端基础服务
        /// </summary>
        public IServiceCollection AddClientServices()
        {
            services.AddBlazoredLocalStorage();
            services.AddScoped<MenuProvider>();
            services.AddScoped<LayoutStore>();
            services.AddScoped<BreadcrumbService>();
            return services;
        }

        /// <summary>
        /// 添加 Kiota API 客户端
        /// </summary>
        /// <param name="baseUrl">API 基础地址</param>
        /// <param name="configureAuthenticationProvider">配置认证提供程序的委托</param>
        /// <param name="configureClient">配置 HttpClient 管道的委托（可选），用于添加自定义 Handler</param>
        /// <returns>服务集合（用于链式调用）</returns>
        public IServiceCollection AddKiotaClient(
            string baseUrl,
            Action<IServiceCollection> configureAuthenticationProvider,
            Action<IHttpClientBuilder>? configureClient = null)
        {
            // 1. 注册认证提供程序
            configureAuthenticationProvider.Invoke(services);

            // 2. 注册 Kiota 核心服务
            services.AddKiotaHandlers();

            // 3. 注册 Factory 和 HttpClient
            var builder = services.AddHttpClient<ApiClientFactory>((sp, client) =>
                {
                    // 设置基础地址和其他 HttpClient 配置
                    client.BaseAddress = new Uri(baseUrl);
                })
                .AttachKiotaHandlers(); // 挂载 Kiota 必须的 Handler

            // 4. 【关键】执行外部传入的配置逻辑
            // 这里允许调用者挂载 1个、2个 或 N个 任意的 Handler
            // 例如: 401 拦截器、日志记录器、重试策略等
            configureClient?.Invoke(builder);

            // 5. 注册最终生成的 Client
            services.AddTransient(sp => sp.GetRequiredService<ApiClientFactory>().GetClient());
            // ----------- Add this part to register the generated client end -------

            services.AddScoped<ApiWrapper>();

            return services;
        }

        /// <summary>
        /// 添加客户端本地化服务
        /// </summary>
        public IServiceCollection AddClientLocalization()
        {
            services.AddLocalization(options => options.ResourcesPath = "Resources");
            services.AddMudTranslations();
            return services;
        }
    }
}