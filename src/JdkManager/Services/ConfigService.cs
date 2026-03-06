using System;
using System.IO;
using System.Xml.Serialization;
using JdkManager.Models;

namespace JdkManager.Services;

/// <summary>
/// 配置服务（XML）
/// </summary>
public class ConfigService
{
    private readonly string _configPath;

    public ConfigService()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var configDir = Path.Combine(appDataPath, "JdkManager");
        Directory.CreateDirectory(configDir);
        _configPath = Path.Combine(configDir, "jdk-config.xml");
    }

    /// <summary>
    /// 加载配置
    /// </summary>
    public JdkConfig LoadConfig()
    {
        try
        {
            if (File.Exists(_configPath))
            {
                var xml = File.ReadAllText(_configPath);
                var serializer = new XmlSerializer(typeof(JdkConfig));
                using var reader = new StringReader(xml);
                var config = serializer.Deserialize(reader) as JdkConfig;
                return config ?? new JdkConfig();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"加载配置失败：{ex.Message}");
        }

        return new JdkConfig();
    }

    /// <summary>
    /// 保存配置
    /// </summary>
    public void SaveConfig(JdkConfig config)
    {
        try
        {
            config.LastModified = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var serializer = new XmlSerializer(typeof(JdkConfig));
            using var writer = new StringWriter();
            serializer.Serialize(writer, config);
            File.WriteAllText(_configPath, writer.ToString());
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"保存配置失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 获取配置文件路径
    /// </summary>
    public string GetConfigPath() => _configPath;

    /// <summary>
    /// 添加 JDK 路径
    /// </summary>
    public void AddJdkPath(string path)
    {
        var config = LoadConfig();
        if (!config.JdkPaths.Contains(path))
        {
            config.JdkPaths.Add(path);
            SaveConfig(config);
        }
    }

    /// <summary>
    /// 移除 JDK 路径
    /// </summary>
    public void RemoveJdkPath(string path)
    {
        var config = LoadConfig();
        if (config.JdkPaths.Contains(path))
        {
            config.JdkPaths.Remove(path);
            SaveConfig(config);
        }
    }

    /// <summary>
    /// 设置当前激活的 JavaHome
    /// </summary>
    public void SetCurrentJavaHome(string path)
    {
        var config = LoadConfig();
        config.CurrentJavaHome = path;
        SaveConfig(config);
    }

    /// <summary>
    /// 获取配置中的 JDK 路径列表
    /// </summary>
    public System.Collections.Generic.List<string> GetJdkPaths()
    {
        var config = LoadConfig();
        return config.JdkPaths;
    }
}
