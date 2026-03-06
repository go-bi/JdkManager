namespace JdkManager.Models;

/// <summary>
/// JDK 信息模型
/// </summary>
public class JdkInfo
{
    /// <summary>
    /// JDK 安装路径
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// JDK 版本号 (如：17.0.8, 11.0.20, 1.8.0_381)
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// JDK 版本简称 (如：17, 11, 8)
    /// </summary>
    public string MajorVersion { get; set; } = string.Empty;

    /// <summary>
    /// 供应商 (如：Oracle, OpenJDK, Eclipse Adoptium)
    /// </summary>
    public string Vendor { get; set; } = string.Empty;

    /// <summary>
    /// 是否为当前激活的 JDK
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// 来源类型
    /// </summary>
    public JdkSourceType SourceType { get; set; }

    /// <summary>
    /// 显示名称
    /// </summary>
    public string DisplayName => $"JDK {Version} ({MajorVersion})";

    /// <summary>
    /// 完整显示信息
    /// </summary>
    public string FullInfo => $"{DisplayName} - {Path}";
}

/// <summary>
/// JDK 来源类型
/// </summary>
public enum JdkSourceType
{
    Registry,      // 注册表发现
    PathScan,      // 路径扫描发现
    Manual         // 手动添加
}
