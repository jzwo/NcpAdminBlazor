using NcpAdminBlazor.Client;
using NcpAdminBlazor.Client.Models;

namespace NcpAdminBlazor.Web.Infrastructure.Auth;

/// <summary>
/// 用户令牌刷新服务接口
/// </summary>
public interface IUserTokenRefresher
{
    /// <summary>
    /// 异步刷新用户访问令牌
    /// </summary>
    /// <param name="userId">用户标识</param>
    /// <param name="refreshToken">刷新令牌</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>刷新后的令牌数据，刷新失败时返回 null</returns>
    Task<UserTokenData?> RefreshTokenAsync(
        string userId,
        string refreshToken,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 用户令牌刷新服务实现
/// </summary>
public class UserTokenRefresher(
    IServiceProvider serviceProvider,
    ILogger<UserTokenRefresher> logger) : IUserTokenRefresher
{
    /// <summary>
    /// 异步刷新用户访问令牌
    /// </summary>
    public async Task<UserTokenData?> RefreshTokenAsync(
        string userId,
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken);
        //这里为了防止循环依赖，使用 IServiceProvider 获取 ApiClient 实例
        var apiClient = serviceProvider.GetRequiredService<ApiClient>();
        try
        {
            logger.LogDebug("开始刷新令牌，用户ID: {UserId}", userId);

            var response = await apiClient.Api.Auth.RefreshToken.PostAsync(new FastEndpointsSecurityTokenRequest
            {
                UserId = userId,
                RefreshToken = refreshToken
            }, cancellationToken: cancellationToken);

            if (response == null)
            {
                logger.LogWarning("令牌刷新响应为空，用户ID: {UserId}", userId);
                return null;
            }

            if (response.Success == false)
            {
                logger.LogWarning("令牌刷新请求失败，用户ID: {UserId}，错误信息: {ErrorMessage}",
                    userId, response.Message);
                return null;
            }

            if (response.Data == null)
            {
                logger.LogWarning("令牌刷新响应数据为空，用户ID: {UserId}", userId);
                return null;
            }

            // 验证响应数据的必需字段
            if (string.IsNullOrWhiteSpace(response.Data.UserId) ||
                string.IsNullOrWhiteSpace(response.Data.AccessToken) ||
                string.IsNullOrWhiteSpace(response.Data.RefreshToken) ||
                !response.Data.AccessTokenExpiry.HasValue ||
                !response.Data.RefreshTokenExpiry.HasValue)
            {
                logger.LogWarning("令牌刷新响应数据不完整，用户ID: {UserId}", userId);
                return null;
            }

            logger.LogInformation("令牌刷新成功，用户ID: {UserId}", userId);

            return new UserTokenData(
                UserId: response.Data.UserId,
                AccessToken: response.Data.AccessToken,
                RefreshToken: response.Data.RefreshToken,
                AccessTokenExpiresAt: response.Data.AccessTokenExpiry.Value,
                RefreshTokenExpiresAt: response.Data.RefreshTokenExpiry.Value
            );
        }
        catch (OperationCanceledException ex)
        {
            logger.LogInformation(ex, "令牌刷新操作已取消，用户ID: {UserId}", userId);
            return null;
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "令牌刷新网络请求失败，用户ID: {UserId}", userId);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "令牌刷新发生未预期错误，用户ID: {UserId}", userId);
            return null;
        }
    }
}