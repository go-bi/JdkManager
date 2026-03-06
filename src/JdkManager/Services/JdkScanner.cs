using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using JdkManager.Models;

namespace JdkManager.Services;

/// <summary>
/// JDK 扫描服务
/// </summary>
public class JdkScanner
{
    private readonly RegistryService _registryService;
    private readonly ConfigService _configService;

    // 常见 JDK 安装根目录
    private static readonly string[] CommonJdkRoots = new[]
    {
        @"C:\Program Files\Java",
        @"C:\Program Files (x86)\Java",
        @"D:\Java",
        @"C:\Java",
        @"%PROGRAMFILES%\Java",
        @"%PROGRAMFILES(X86)%\Java"
    };

    // JDK 目录命名模式
    private static readonly Regex JdkDirPattern = new(@"^(jdk|java)-?\d*", RegexOptions.IgnoreCase);

    public JdkScanner(RegistryService registryService, ConfigService configService)
    {
        _registryService = registryService;
        _configService = configService;
    }

    /// <summary>
    /// 初始化配置文件（如果不存在则创建）
    /// </summary>
    public void InitializeConfig()
    {
        var config = _configService.LoadConfig();

        // 如果配置文件为空，扫描系统 JDK 并保存
        if (config.JdkPaths.Count == 0)
        {
            var jdks = ScanAllJdks();
            foreach (var jdk in jdks)
            {
                if (!config.JdkPaths.Contains(jdk.Path))
                {
                    config.JdkPaths.Add(jdk.Path);
                }
            }

            // 保存当前 JAVA_HOME
            var currentJavaHome = GetCurrentJavaHome();
            if (currentJavaHome != null)
            {
                config.CurrentJavaHome = currentJavaHome;
            }

            _configService.SaveConfig(config);
        }
    }

    /// <summary>
    /// 获取当前 JAVA_HOME
    /// </summary>
    private string? GetCurrentJavaHome()
    {
        return Environment.GetEnvironmentVariable("JAVA_HOME", EnvironmentVariableTarget.Machine) ??
               Environment.GetEnvironmentVariable("JAVA_HOME", EnvironmentVariableTarget.User);
    }

    /// <summary>
    /// 扫描所有可用的 JDK
    /// </summary>
    public List<JdkInfo> ScanAllJdks()
    {
        var jdks = new Dictionary<string, JdkInfo>();
        var config = _configService.LoadConfig();

        // 1. 从配置文件加载用户手动添加的 JDK（优先）
        foreach (var path in config.JdkPaths)
        {
            if (!jdks.ContainsKey(path) && IsValidJdk(path))
            {
                var jdkInfo = CreateJdkInfo(path, JdkSourceType.Manual);
                if (jdkInfo != null)
                {
                    jdks[path] = jdkInfo;
                }
            }
        }

        // 2. 从注册表获取
        var registryJdks = _registryService.GetInstalledJdksFromRegistry();
        foreach (var jdk in registryJdks)
        {
            if (!jdks.ContainsKey(jdk.Path))
            {
                jdks[jdk.Path] = jdk;
            }
        }

        // 3. 从常见路径扫描
        foreach (var root in CommonJdkRoots)
        {
            var expandedRoot = Environment.ExpandEnvironmentVariables(root);
            if (Directory.Exists(expandedRoot))
            {
                foreach (var dir in Directory.GetDirectories(expandedRoot))
                {
                    var dirName = Path.GetFileName(dir);
                    if (JdkDirPattern.IsMatch(dirName) && IsValidJdk(dir))
                    {
                        if (!jdks.ContainsKey(dir))
                        {
                            var jdkInfo = CreateJdkInfo(dir, JdkSourceType.PathScan);
                            if (jdkInfo != null)
                            {
                                jdks[dir] = jdkInfo;
                            }
                        }
                    }
                }
            }
        }

        // 4. 验证每个 JDK 并获取详细版本信息
        var result = new List<JdkInfo>();
        foreach (var jdk in jdks.Values)
        {
            var versionInfo = GetJavaVersion(jdk.Path);
            if (versionInfo != null)
            {
                jdk.Version = versionInfo.Version;
                jdk.MajorVersion = versionInfo.MajorVersion;
                jdk.Vendor = versionInfo.Vendor;
                result.Add(jdk);
            }
        }

        return result.OrderByDescending(j => j.MajorVersion).ThenBy(j => j.Version).ToList();
    }

    /// <summary>
    /// 验证指定目录是否是有效的 JDK
    /// </summary>
    public bool IsValidJdk(string path)
    {
        return Directory.Exists(path) &&
               File.Exists(Path.Combine(path, "bin", "java.exe"));
    }

    /// <summary>
    /// 手动添加 JDK
    /// </summary>
    public JdkInfo? AddJdkManually(string path)
    {
        if (!IsValidJdk(path))
        {
            return null;
        }

        // 保存到配置文件
        _configService.AddJdkPath(path);

        // 避免重复
        return CreateJdkInfo(path, JdkSourceType.Manual);
    }

    /// <summary>
    /// 删除 JDK
    /// </summary>
    public void DeleteJdkManually(string path)
    {
        _configService.RemoveJdkPath(path);
    }

    /// <summary>
    /// 同步配置文件（刷新所有 JDK 到配置文件）
    /// </summary>
    public void SyncConfig(List<JdkInfo> jdks)
    {
        var config = _configService.LoadConfig();

        // 保留用户手动添加的路径，添加新扫描到的 JDK
        foreach (var jdk in jdks)
        {
            if (!config.JdkPaths.Contains(jdk.Path))
            {
                config.JdkPaths.Add(jdk.Path);
            }
        }

        _configService.SaveConfig(config);
    }

    /// <summary>
    /// 更新当前激活的 JAVA_HOME
    /// </summary>
    public void UpdateCurrentJavaHome(string path)
    {
        _configService.SetCurrentJavaHome(path);
    }

    /// <summary>
    /// 创建 JDK 信息
    /// </summary>
    private JdkInfo? CreateJdkInfo(string path, JdkSourceType sourceType)
    {
        var versionInfo = GetJavaVersion(path);
        if (versionInfo == null)
        {
            return null;
        }

        return new JdkInfo
        {
            Path = path,
            Version = versionInfo.Version,
            MajorVersion = versionInfo.MajorVersion,
            Vendor = versionInfo.Vendor,
            SourceType = sourceType
        };
    }

    /// <summary>
    /// 执行 java -version 获取版本信息
    /// </summary>
    private JavaVersionInfo? GetJavaVersion(string jdkPath)
    {
        try
        {
            var javaExe = Path.Combine(jdkPath, "bin", "java.exe");
            if (!File.Exists(javaExe))
            {
                return null;
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = javaExe,
                Arguments = "-version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                return null;
            }

            // java -version 输出到 stderr
            var output = process.StandardError.ReadToEnd();
            process.WaitForExit();

            return ParseJavaVersion(output);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"获取 Java 版本失败 {jdkPath}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 解析 java -version 输出
    /// </summary>
    private JavaVersionInfo? ParseJavaVersion(string output)
    {
        // 示例输出:
        // java version "17.0.8" 2023-07-18 LTS
        // Java(TM) SE Runtime Environment (build 17.0.8+9-LTS-211)
        // Java HotSpot(TM) 64-Bit Server VM (build 17.0.8+9-LTS-211, mixed mode, sharing)

        // 或者:
        // openjdk version "11.0.20" 2023-07-18
        // OpenJDK Runtime Environment (build 11.0.20+8-post-Ubuntu-1ubuntu2204)
        // OpenJDK 64-Bit Server VM (build 11.0.20+8-post-Ubuntu-1ubuntu2204, mixed mode, sharing)

        var versionMatch = Regex.Match(output, @"(?:java|openjdk)\s+version\s+""([^""]+)""");
        if (!versionMatch.Success)
        {
            return null;
        }

        var fullVersion = versionMatch.Groups[1].Value;
        var majorVersion = ExtractMajorVersion(fullVersion);

        // 提取供应商
        string vendor = "Unknown";
        if (output.Contains("Java(TM)"))
        {
            vendor = "Oracle";
        }
        else if (output.Contains("OpenJDK"))
        {
            vendor = "OpenJDK";
        }
        else if (output.Contains("Adoptium"))
        {
            vendor = "Eclipse Adoptium";
        }
        else if (output.Contains("Amazon"))
        {
            vendor = "Amazon";
        }

        return new JavaVersionInfo
        {
            Version = fullVersion,
            MajorVersion = majorVersion,
            Vendor = vendor
        };
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
}

/// <summary>
/// Java 版本信息
/// </summary>
public class JavaVersionInfo
{
    public string Version { get; set; } = string.Empty;
    public string MajorVersion { get; set; } = string.Empty;
    public string Vendor { get; set; } = string.Empty;
}
