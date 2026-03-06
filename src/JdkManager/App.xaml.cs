using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using JdkManager.Services;
using JdkManager.ViewModels;

namespace JdkManager;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        // 全局异常处理
        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            LogException("UnhandledException", args.ExceptionObject as Exception);
        };

        DispatcherUnhandledException += (s, args) =>
        {
            LogException("DispatcherUnhandledException", args.Exception);
            args.Handled = true;
        };

        Dispatcher.UnhandledException += (s, args) =>
        {
            LogException("Dispatcher.UnhandledException", args.Exception);
            args.Handled = true;
        };

        TaskScheduler.UnobservedTaskException += (s, args) =>
        {
            LogException("UnobservedTaskException", args.Exception);
            args.SetObserved();
        };

        try
        {
            base.OnStartup(e);

            // 注册服务
            var configService = new ConfigService();
            var registryService = new RegistryService();
            var jdkScanner = new JdkScanner(registryService, configService);
            var environmentService = new EnvironmentService();

            // 创建主窗口
            var mainWindow = new Views.MainWindow(
                new MainViewModel(jdkScanner, environmentService, configService)
            );
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            LogException("OnStartup", ex);
            MessageBox.Show($"启动失败：{ex.Message}\n\n{ex.StackTrace}", "JDK Manager 错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LogException(string location, Exception? ex)
    {
        try
        {
            var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "JdkManager", "error.log");
            Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
            File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {location}:{Environment.NewLine}{ex}{Environment.NewLine}---{Environment.NewLine}");
        }
        catch { }
    }
}
