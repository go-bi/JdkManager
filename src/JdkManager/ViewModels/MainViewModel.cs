using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JdkManager.Models;
using JdkManager.Services;

namespace JdkManager.ViewModels;

/// <summary>
/// 主窗口 ViewModel
/// </summary>
public partial class MainViewModel : ObservableRecipient
{
    private readonly JdkScanner _jdkScanner;
    private readonly EnvironmentService _environmentService;
    private readonly ConfigService _configService;

    [ObservableProperty]
    private ObservableCollection<JdkInfoViewModel> _jdks = new();

    [ObservableProperty]
    private JdkInfoViewModel? _selectedJdk;

    [ObservableProperty]
    private string _statusMessage = "就绪";

    [ObservableProperty]
    private string _currentVersion = "";

    [ObservableProperty]
    private bool _isAdministrator;

    [ObservableProperty]
    private bool _isLoading;

    public MainViewModel(JdkScanner jdkScanner, EnvironmentService environmentService, ConfigService configService)
    {
        _jdkScanner = jdkScanner;
        _environmentService = environmentService;
        _configService = configService;

        IsAdministrator = _environmentService.IsAdministrator();
        CurrentVersion = _environmentService.GetCurrentJavaVersion() ?? "未知";

        // 初始化配置文件
        _jdkScanner.InitializeConfig();

        // 异步加载 JDK 列表，避免阻塞 UI
        _ = RefreshAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "正在扫描 JDK...";

            var currentJavaHome = _environmentService.GetCurrentJavaHome();

            // 在后台线程扫描 JDK
            var jdks = await Task.Run(() => _jdkScanner.ScanAllJdks());

            Application.Current.Dispatcher.Invoke(() =>
            {
                // 如果当前 JAVA_HOME 不在列表中，添加到列表
                if (!string.IsNullOrEmpty(currentJavaHome) && !jdks.Any(j => j.Path == currentJavaHome))
                {
                    var jdkInfo = _jdkScanner.AddJdkManually(currentJavaHome);
                    if (jdkInfo != null)
                    {
                        jdks.Add(jdkInfo);
                    }
                }

                Jdks.Clear();
                foreach (var jdk in jdks)
                {
                    var isActive = jdk.Path == currentJavaHome;
                    Jdks.Add(new JdkInfoViewModel(jdk) { IsActive = isActive });
                }

                // 选中当前激活的 JDK
                SelectedJdk = Jdks.FirstOrDefault(j => j.IsActive) ?? Jdks.FirstOrDefault();

                // 使用列表中 JDK 的 DisplayName 显示当前激活版本
                if (SelectedJdk != null)
                {
                    CurrentVersion = SelectedJdk.DisplayName;
                }

                StatusMessage = $"已扫描到 {Jdks.Count} 个 JDK";

                // 同步配置文件
                Task.Run(() => _jdkScanner.SyncConfig(jdks));
            });
        }
        catch (Exception ex)
        {
            StatusMessage = $"扫描失败：{ex.Message}";
        }

        IsLoading = false;
    }

    [RelayCommand]
    private async Task SwitchAsync()
    {
        if (SelectedJdk == null)
        {
            StatusMessage = "请先选择一个 JDK";
            return;
        }

        if (!IsAdministrator)
        {
            StatusMessage = "错误：需要管理员权限才能切换 JDK";
            return;
        }

        var targetVersion = SelectedJdk.Version;
        var targetPath = SelectedJdk.Path;

        IsLoading = true;
        StatusMessage = $"正在切换 JDK...";

        try
        {
            var result = await Task.Run(() => _environmentService.SwitchJdk(SelectedJdk.JdkInfo));

            Application.Current.Dispatcher.Invoke(() =>
            {
                if (result.Success)
                {
                    // 更新所有 JDK 的激活状态
                    foreach (var jdk in Jdks)
                    {
                        jdk.IsActive = (jdk == SelectedJdk);
                    }
                    StatusMessage = $"已成功切换到 JDK {targetVersion}";

                    // 更新配置文件中的当前 JAVA_HOME
                    _jdkScanner.UpdateCurrentJavaHome(targetPath);

                    // 使用列表中 JDK 的 DisplayName 更新当前版本显示
                    CurrentVersion = SelectedJdk.DisplayName;
                }
                else
                {
                    StatusMessage = result.ErrorMessage ?? "切换失败";
                }
            });
        }
        catch (Exception ex)
        {
            StatusMessage = $"切换失败：{ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task AddJdkAsync()
    {
        // 使用 OpenFileDialog 选择目录
        var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "请选择 JDK 安装目录",
            ShowNewFolderButton = false
        };

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            var selectedPath = dialog.SelectedPath;

            // 在后台验证 JDK
            var jdkInfo = await Task.Run(() => _jdkScanner.AddJdkManually(selectedPath));

            if (jdkInfo != null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    // 检查是否已存在
                    if (Jdks.Any(j => j.JdkInfo.Path == jdkInfo.Path))
                    {
                        StatusMessage = $"该 JDK 已存在于列表中";
                        return;
                    }

                    var jdkViewModel = new JdkInfoViewModel(jdkInfo);
                    Jdks.Add(jdkViewModel);
                    SelectedJdk = jdkViewModel;
                    StatusMessage = $"已添加 JDK {jdkInfo.Version}";
                });
            }
            else
            {
                StatusMessage = "无效的 JDK 目录：未找到 bin/java.exe";
            }
        }
    }

    [RelayCommand]
    private void DeleteJdk()
    {
        if (SelectedJdk == null)
        {
            StatusMessage = "请先选择一个 JDK";
            return;
        }

        // 不允许删除当前激活的 JDK
        if (SelectedJdk.JdkInfo.IsActive)
        {
            StatusMessage = "不能删除当前激活的 JDK";
            return;
        }

        // 从配置文件中移除
        _jdkScanner.DeleteJdkManually(SelectedJdk.JdkInfo.Path);

        Jdks.Remove(SelectedJdk);
        SelectedJdk = Jdks.FirstOrDefault();
        StatusMessage = "已删除选中的 JDK";
    }

    [RelayCommand]
    private void OpenInExplorer()
    {
        if (SelectedJdk == null)
        {
            StatusMessage = "请先选择一个 JDK";
            return;
        }

        try
        {
            Process.Start("explorer.exe", SelectedJdk.JdkInfo.Path);
        }
        catch (Exception ex)
        {
            StatusMessage = $"打开目录失败：{ex.Message}";
        }
    }
}

/// <summary>
/// JDK 信息 ViewModel（包装器）
/// </summary>
public partial class JdkInfoViewModel : ObservableObject
{
    public JdkInfo JdkInfo { get; }

    public JdkInfoViewModel(JdkInfo jdkInfo)
    {
        JdkInfo = jdkInfo;
    }

    public string DisplayName => JdkInfo.DisplayName;
    public string Path => JdkInfo.Path;
    public string Version => JdkInfo.Version;
    public string MajorVersion => JdkInfo.MajorVersion;
    public string Vendor => JdkInfo.Vendor;
    public JdkSourceType SourceType => JdkInfo.SourceType;

    [ObservableProperty]
    private bool _isActive;
}
