using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;

namespace NcpAdminBlazor.Client.Infrastructure.Http;

/// <summary>
/// API 客户端工厂，负责创建 ApiClient 实例
/// </summary>
public class ApiClientFactory(HttpClient httpClient, IAuthenticationProvider authenticationProvider)
{
    /// <summary>
    /// 获取 ApiClient 实例
    /// </summary>
    public ApiClient GetClient()
    {
        return new ApiClient(new HttpClientRequestAdapter(
            authenticationProvider,
            httpClient: httpClient));
    }
}