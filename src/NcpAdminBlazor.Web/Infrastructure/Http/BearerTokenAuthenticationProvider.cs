using Microsoft.Extensions.Options;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;
using NcpAdminBlazor.Web.Options;

namespace NcpAdminBlazor.Web.Infrastructure.Http;

/// <summary>
/// Bearer Token 认证提供器
/// 对认证端点跳过 Token 验证
/// </summary>
public class BearerTokenAuthenticationProvider(
    BaseBearerTokenAuthenticationProvider baseBearerTokenAuthenticationProvider,
    IOptions<ApiServiceOptions> options)
    : IAuthenticationProvider
{
    private readonly ApiServiceOptions _options = options.Value;

    public async Task AuthenticateRequestAsync(
        RequestInformation request,
        Dictionary<string, object>? additionalAuthenticationContext = null,
        CancellationToken cancellationToken = default)
    {
        // 认证端点跳过 Token 验证
        if (request.URI.AbsolutePath.StartsWith(_options.AuthPathPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        await baseBearerTokenAuthenticationProvider.AuthenticateRequestAsync(
            request,
            additionalAuthenticationContext,
            cancellationToken);
    }
}