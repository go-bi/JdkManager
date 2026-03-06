using System.Windows;
using JdkManager.ViewModels;

namespace JdkManager.Views;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
