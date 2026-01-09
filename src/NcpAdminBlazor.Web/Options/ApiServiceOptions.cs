namespace NcpAdminBlazor.Web.Options;

/// <summary>
/// API 转发配置选项
/// </summary>
public class ApiServiceOptions
{
    /// <summary>
    /// 配置节名称
    /// </summary>
    public const string SectionName = "ApiService";

    /// <summary>
    /// 认证 API 路径前缀（不带尾部斜杠）
    /// </summary>
    public string AuthPathPrefix { get; set; } = "/api/auth";

}
