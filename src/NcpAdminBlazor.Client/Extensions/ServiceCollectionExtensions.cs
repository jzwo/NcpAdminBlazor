using Blazored.LocalStorage;
using MudBlazor.Translations;
using NcpAdminBlazor.Client.Kiota;
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
        public IServiceCollection AddKiotaClient(string baseUrl,
            Action<IServiceCollection> configureAuthenticationProvider)
        {
            // Register the authentication provider
            configureAuthenticationProvider.Invoke(services);

            // Add Kiota handlers to the service collection
            services.AddKiotaHandlers();

            // Register the factory for the GitHub client
            services.AddHttpClient<ApiClientFactory>((sp, client) =>
                {
                    // Set the base address and accept header
                    // or other settings on the http client
                    client.BaseAddress = new Uri(baseUrl);
                })
                .AttachKiotaHandlers(); // Attach the Kiota handlers to the http client, this is to enable all the Kiota features.

            // Register the GitHub client
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