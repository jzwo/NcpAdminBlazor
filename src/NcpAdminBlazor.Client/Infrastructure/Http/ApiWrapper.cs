using MudBlazor;

namespace NcpAdminBlazor.Client.Infrastructure.Http;

public class ApiWrapper(ApiClient apiClient, ILogger<ApiWrapper> logger, ISnackbar snackbar)
{
    public async Task<bool> HandleCallAsync<TResponse>(
        Func<ApiClient, Task<TResponse?>> apiCall,
        Action<TResponse>? onSuccess = null) where TResponse : NetCorePalExtensionsDtoResponseData
    {
        TResponse? response;
        try
        {
            response = await apiCall(apiClient);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "API call failed with exception.");
            snackbar.Add(ex.Message, Severity.Error);
            return false;
        }

        if (response?.Success == true)
        {
            onSuccess?.Invoke(response);
            return true;
        }

        // 5. 业务失败 (API返回了 Success = false)
        logger.LogWarning("API call reported failure: {Message}", response?.Message);
        snackbar.Add(response?.Message ?? "操作失败！", Severity.Error);
        return false;
    }
}