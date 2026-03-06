using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Win32;
using JdkManager.Models;

namespace JdkManager.Services;

/// <summary>
/// 注册表服务
/// </summary>
public class RegistryService
{
    private const string JavaSoftKey = @"SOFTWARE\JavaSoft";
    private const string JdkKey = @"SOFTWARE\JavaSoft\JDK";
    private const string JavaDevelopmentKitKey = @"SOFTWARE\JavaSoft\Java Development Kit";

    /// <summary>
    /// 从注册表获取已安装的 JDK 列表
    /// </summary>
    public List<JdkInfo> GetInstalledJdksFromRegistry()
    {
        var jdks = new List<JdkInfo>();

        try
        {
            // 尝试读取 HKLM\SOFTWARE\JavaSoft\JDK (新格式)
            var jdkKey = Registry.LocalMachine.OpenSubKey(JdkKey);
            if (jdkKey != null)
            {
                foreach (var versionName in jdkKey.GetSubKeyNames())
                {
                    var versionKey = jdkKey.OpenSubKey(versionName);
                    if (versionKey != null)
                    {
                        var javaHome = versionKey.GetValue("JavaHome")?.ToString();
                        if (!string.IsNullOrEmpty(javaHome) && Directory.Exists(javaHome))
                        {
                            jdks.Add(new JdkInfo
                            {
                                Path = javaHome,
                                Version = versionName,
                                MajorVersion = ExtractMajorVersion(versionName),
                                Vendor = GetVendor(javaHome),
                                SourceType = JdkSourceType.Registry
                            });
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"读取注册表 JDK 失败：{ex.Message}");
        }

        try
        {
            // 尝试读取 HKLM\SOFTWARE\JavaSoft\Java Development Kit (旧格式)
            var jdkOldKey = Registry.LocalMachine.OpenSubKey(JavaDevelopmentKitKey);
            if (jdkOldKey != null)
            {
                foreach (var versionName in jdkOldKey.GetSubKeyNames())
                {
                    var versionKey = jdkOldKey.OpenSubKey(versionName);
                    if (versionKey != null)
                    {
                        var javaHome = versionKey.GetValue("JavaHome")?.ToString();
                        if (!string.IsNullOrEmpty(javaHome) && Directory.Exists(javaHome))
                        {
                            // 避免重复
                            if (jdks.All(j => j.Path != javaHome))
                            {
                                jdks.Add(new JdkInfo
                                {
                                    Path = javaHome,
                                    Version = versionName,
                                    MajorVersion = ExtractMajorVersion(versionName),
                                    Vendor = GetVendor(javaHome),
                                    SourceType = JdkSourceType.Registry
                                });
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"读取注册表 Java Development Kit 失败：{ex.Message}");
        }

        return jdks;
    }

    /// <summary>
    /// 提取主版本号
    /// </summary>
    private static string ExtractMajorVersion(string version)
    {
        // 处理如 "1.8.0_381" -> "8", "17.0.8" -> "17"
        if (version.StartsWith("1."))
        {
            var parts = version.Split('.');
            if (parts.Length > 1)
            {
                return parts[1].Split('_')[0];
            }
        }
        return version.Split('.')[0];
    }

    /// <summary>
    /// 获取 JDK 供应商
    /// </summary>
    private string GetVendor(string javaHome)
    {
        try
        {
            var releaseFile = Path.Combine(javaHome, "release");
            if (File.Exists(releaseFile))
            {
                foreach (var line in File.ReadAllLines(releaseFile))
                {
                    if (line.StartsWith("IMPLEMENTOR="))
                    {
                        return line.Split('=')[1].Trim('"');
                    }
                }
            }
        }
        catch
        {
            // 忽略错误
        }

        return "Unknown";
    }
}
