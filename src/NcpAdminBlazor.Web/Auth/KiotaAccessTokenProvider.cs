using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Kiota.Abstractions.Authentication;

namespace NcpAdminBlazor.Web.Auth;

/// <summary>
/// Kiota Access Token 提供器
/// 从 UserTokenStore 获取已刷新的 Access Token
/// </summary>
public class KiotaAccessTokenProvider(
    AuthenticationStateProvider authProvider,
    IUserTokenStore userTokenStore,
    ILogger<KiotaAccessTokenProvider> logger)
    : IAccessTokenProvider
{
    public AllowedHostsValidator AllowedHostsValidator { get; } = new();

    public async Task<string> GetAuthorizationTokenAsync(
        Uri uri,
        Dictionary<string, object>? additionalAuthenticationContext = null,
        CancellationToken cancellationToken = default)
    {
        var authState = await authProvider.GetAuthenticationStateAsync();
        var token = await userTokenStore.GetTokenAsync(authState.User);

        if (!string.IsNullOrEmpty(token))
        {
            return token;
        }

        logger.LogDebug("No valid access token available for Kiota request to {Uri}", uri);
        return string.Empty;
    }
}