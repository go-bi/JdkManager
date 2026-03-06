namespace JdkManager.Models;

/// <summary>
/// JDK 切换结果
/// </summary>
public class SwitchResult
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 错误消息（失败时）
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 切换后的 JDK 信息
    /// </summary>
    public JdkInfo? JdkInfo { get; set; }

    /// <summary>
    /// 创建成功结果
    /// </summary>
    public static SwitchResult Ok(JdkInfo jdkInfo) => new()
    {
        Success = true,
        JdkInfo = jdkInfo
    };

    /// <summary>
    /// 创建失败结果
    /// </summary>
    public static SwitchResult Fail(string errorMessage) => new()
    {
        Success = false,
        ErrorMessage = errorMessage
    };
}
