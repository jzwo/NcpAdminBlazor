using System.Collections.Concurrent;
using System.Security.Claims;
using Microsoft.Extensions.Caching.Hybrid;

namespace NcpAdminBlazor.Web.Auth;

/// <summary>
/// 用户令牌存储接口
/// </summary>
public interface IUserTokenStore
{
    Task<string?> GetTokenAsync(ClaimsPrincipal user);
    Task StoreTokenAsync(UserTokenData tokenData);
    Task ClearTokenAsync(ClaimsPrincipal user);
}

/// <summary>
/// 用户令牌存储实现
/// 使用 HybridCache 存储令牌，支持自动刷新
/// </summary>
public class UserTokenStore(HybridCache cache, IUserTokenRefresher userTokenRefresher) : IUserTokenStore
{
    private const string CacheKeyPrefix = "auth:token:";
    private static readonly TimeSpan ExpirationBuffer = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan LocalCacheExpiration = TimeSpan.FromMinutes(5);

    // 用于防止同一用户并发刷新 Token
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> RefreshLocks = new();

    public async Task<string?> GetTokenAsync(ClaimsPrincipal user)
    {
        var userId = GetUserId(user);
        if (userId is null)
        {
            return null;
        }

        var key = GetCacheKey(userId);
        var tokenData = await GetCachedTokenDataAsync(key);

        if (tokenData is null)
        {
            return null;
        }

        // AccessToken 未过期，直接返回
        if (DateTimeOffset.UtcNow.Add(ExpirationBuffer) <= tokenData.AccessTokenExpiresAt)
        {
            return tokenData.AccessToken;
        }

        // 尝试刷新 Token（带并发控制）
        return await RefreshTokenWithLockAsync(userId, tokenData, user);
    }

    public async Task StoreTokenAsync(UserTokenData tokenData)
    {
        var key = GetCacheKey(tokenData.UserId);
        var entryOptions = new HybridCacheEntryOptions
        {
            Expiration = tokenData.RefreshTokenExpiresAt - DateTimeOffset.UtcNow,
            LocalCacheExpiration = LocalCacheExpiration
        };

        await cache.SetAsync(key, tokenData, entryOptions);
    }

    public async Task ClearTokenAsync(ClaimsPrincipal user)
    {
        var userId = GetUserId(user);
        if (userId is null)
        {
            return;
        }

        var key = GetCacheKey(userId);
        await cache.RemoveAsync(key);
    }

    private static string GetCacheKey(string userId) => $"{CacheKeyPrefix}{userId}";

    private static string? GetUserId(ClaimsPrincipal user) =>
        user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    private async Task<UserTokenData?> GetCachedTokenDataAsync(string key)
    {
        return await cache.GetOrCreateAsync<UserTokenData?>(
            key,
            factory: _ => ValueTask.FromResult<UserTokenData?>(null));
    }

    /// <summary>
    /// 带锁的 Token 刷新，防止并发刷新
    /// </summary>
    private async Task<string?> RefreshTokenWithLockAsync(
        string userId,
        UserTokenData tokenData,
        ClaimsPrincipal user)
    {
        var refreshLock = RefreshLocks.GetOrAdd(userId, _ => new SemaphoreSlim(1, 1));

        await refreshLock.WaitAsync();
        try
        {
            // 双重检查：可能在等待锁期间已被其他请求刷新
            var key = GetCacheKey(userId);
            var currentTokenData = await GetCachedTokenDataAsync(key);

            if (currentTokenData is not null &&
                DateTimeOffset.UtcNow.Add(ExpirationBuffer) <= currentTokenData.AccessTokenExpiresAt)
            {
                return currentTokenData.AccessToken;
            }

            // 执行刷新
            var newTokenData = await userTokenRefresher.RefreshTokenAsync(userId, tokenData.RefreshToken);

            if (newTokenData is not null)
            {
                await StoreTokenAsync(newTokenData);
                return newTokenData.AccessToken;
            }

            // 刷新失败，清除本地状态
            await ClearTokenAsync(user);
            return null;
        }
        finally
        {
            refreshLock.Release();
        }
    }
}