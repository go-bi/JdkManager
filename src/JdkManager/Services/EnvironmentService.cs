using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Microsoft.Win32;
using JdkManager.Models;

namespace JdkManager.Services;

/// <summary>
/// 环境变量服务
/// </summary>
public class EnvironmentService
{
    private const string JavaHomeVar = "JAVA_HOME";
    private const string PathVar = "PATH";

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SendMessageTimeout(
        IntPtr hWnd,
        uint Msg,
        IntPtr wParam,
        string lParam,
        uint fuFlags,
        uint uTimeout,
        out IntPtr lpdwResult);

    private static readonly IntPtr HwndBroadcast = new IntPtr(0xffff);
    private const uint WmSettingChange = 0x001A;
    private const uint SmtoAbortIfHung = 0x0002;

    /// <summary>
    /// 获取当前 JAVA_HOME
    /// </summary>
    public string? GetCurrentJavaHome()
    {
        return Environment.GetEnvironmentVariable(JavaHomeVar, EnvironmentVariableTarget.Machine) ??
               Environment.GetEnvironmentVariable(JavaHomeVar, EnvironmentVariableTarget.User);
    }

    /// <summary>
    /// 获取当前 Java 版本信息
    /// </summary>
    public string? GetCurrentJavaVersion()
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "java",
                Arguments = "-version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process != null)
            {
                var output = process.StandardError.ReadToEnd();
                process.WaitForExit();
                return output.Split('\n').FirstOrDefault()?.Trim();
            }
        }
        catch
        {
            // 忽略错误
        }
        return null;
    }

    /// <summary>
    /// 切换 JDK
    /// </summary>
    public SwitchResult SwitchJdk(JdkInfo jdkInfo)
    {
        try
        {
            // 1. 设置 JAVA_HOME (系统级)
            SetEnvironmentVariable(JavaHomeVar, jdkInfo.Path, EnvironmentVariableTarget.Machine);

            // 2. 更新 PATH - 将新的 JDK bin 目录添加到 PATH 前面
            UpdatePath(jdkInfo.Path);

            // 3. 广播环境变量变更消息
            BroadcastEnvironmentChange();

            // 4. 验证切换结果
            System.Threading.Thread.Sleep(500); // 等待环境变量生效
            var currentJavaHome = GetCurrentJavaHome();

            if (currentJavaHome != jdkInfo.Path)
            {
                return SwitchResult.Fail($"切换失败：JAVA_HOME 未正确设置为 {jdkInfo.Path}");
            }

            jdkInfo.IsActive = true;
            return SwitchResult.Ok(jdkInfo);
        }
        catch (Exception ex)
        {
            return SwitchResult.Fail($"切换失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 设置环境变量
    /// </summary>
    private void SetEnvironmentVariable(string name, string value, EnvironmentVariableTarget target)
    {
        // 使用注册表直接设置，确保立即生效
        var keyPath = target == EnvironmentVariableTarget.Machine
            ? @"SYSTEM\CurrentControlSet\Control\Session Manager\Environment"
            : @"Environment";

        using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
            target == EnvironmentVariableTarget.Machine
                ? @"SYSTEM\CurrentControlSet\Control\Session Manager\Environment"
                : @"Environment",
            true);

        if (key != null)
        {
            key.SetValue(name, value, Microsoft.Win32.RegistryValueKind.String);
        }
        else
        {
            // 回退到 .NET API
            Environment.SetEnvironmentVariable(name, value, target);
        }
    }

    /// <summary>
    /// 更新 PATH 环境变量
    /// </summary>
    private void UpdatePath(string jdkPath)
    {
        var newBinPath = Path.Combine(jdkPath, "bin");

        // 获取当前系统 PATH
        var currentPath = Environment.GetEnvironmentVariable(PathVar, EnvironmentVariableTarget.Machine) ?? "";

        // 移除旧的 JDK bin 路径
        var paths = currentPath.Split(';')
            .Where(p => !p.EndsWith("\\bin", StringComparison.OrdinalIgnoreCase) ||
                        !File.Exists(Path.Combine(p, "java.exe")))
            .ToList();

        // 添加新的 JDK bin 路径到开头
        paths.Insert(0, newBinPath);

        // 去重
        paths = paths.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        var newPath = string.Join(";", paths);

        // 设置新的 PATH
        SetEnvironmentVariable(PathVar, newPath, EnvironmentVariableTarget.Machine);
    }

    /// <summary>
    /// 广播环境变量变更消息
    /// </summary>
    private void BroadcastEnvironmentChange()
    {
        try
        {
            SendMessageTimeout(
                HwndBroadcast,
                WmSettingChange,
                IntPtr.Zero,
                "Environment",
                SmtoAbortIfHung,
                1000,
                out _);
        }
        catch
        {
            // 忽略广播失败
        }
    }

    /// <summary>
    /// 检查是否有管理员权限
    /// </summary>
    public bool IsAdministrator()
    {
        using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
        var principal = new System.Security.Principal.WindowsPrincipal(identity);
        return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
    }
}
