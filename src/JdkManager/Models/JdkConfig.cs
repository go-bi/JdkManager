using System.Collections.Generic;
using System.Xml.Serialization;

namespace JdkManager.Models;

/// <summary>
/// JDK 配置模型（XML）
/// </summary>
[XmlRoot("JdkConfiguration")]
public class JdkConfig
{
    /// <summary>
    /// JDK 路径列表
    /// </summary>
    [XmlArray("JdkPaths")]
    [XmlArrayItem("Path")]
    public List<string> JdkPaths { get; set; } = new();

    /// <summary>
    /// 当前激活的 JAVA_HOME 路径
    /// </summary>
    [XmlElement("CurrentJavaHome")]
    public string? CurrentJavaHome { get; set; }

    /// <summary>
    /// 上次配置修改时间
    /// </summary>
    [XmlElement("LastModified")]
    public string? LastModified { get; set; }
}
