/**
 * Cookie 认证服务的 JavaScript 实现
 * 用于在 SSR 场景下通过浏览器发起 HTTP 请求来设置和清除认证 Cookie
 */

/**
 * 设置认证 Cookie
 * @param {string} accessToken - Access Token
 * @param {string} refreshToken - Refresh Token
 * @param {string} accessTokenExpiry - Access Token 过期时间 (ISO 8601)
 * @param {string} refreshTokenExpiry - Refresh Token 过期时间 (ISO 8601)
 * @param {string} userId - 用户 ID
 * @returns {Promise<boolean>} 是否成功设置 Cookie
 */
export async function setAuthCookie(accessToken, refreshToken, accessTokenExpiry, refreshTokenExpiry, userId) {
    try {
        const request = {
            accessToken,
            refreshToken,
            accessTokenExpiry,
            refreshTokenExpiry,
            userId
        };

        const response = await fetch('/bff-api/auth/login', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(request),
            credentials: 'same-origin' // 确保 Cookie 被正确发送和接收
        });

        if (response.ok) {
            console.log('Successfully set authentication cookie');
            return true;
        } else {
            console.warn(`Failed to set authentication cookie. Status: ${response.status}`);
            return false;
        }
    } catch (error) {
        console.error('Error setting authentication cookie:', error);
        return false;
    }
}

/**
 * 清除认证 Cookie
 * @returns {Promise<boolean>} 是否成功清除 Cookie
 */
export async function clearAuthCookie() {
    try {
        const response = await fetch('/bff-api/auth/logout', {
            method: 'POST',
            credentials: 'same-origin' // 确保 Cookie 被正确发送
        });

        if (response.ok) {
            console.log('Successfully cleared authentication cookie');
            return true;
        } else {
            console.warn(`Failed to clear authentication cookie. Status: ${response.status}`);
            return false;
        }
    } catch (error) {
        console.error('Error clearing authentication cookie:', error);
        return false;
    }
}
