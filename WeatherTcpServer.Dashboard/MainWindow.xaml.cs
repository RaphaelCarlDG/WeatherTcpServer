using System.ComponentModel;
using System.Windows;
using WeatherTcpServer.Dashboard.ViewModels;

namespace WeatherTcpServer.Dashboard;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;

    public MainWindow()
    {
        InitializeComponent();

        _vm = new MainViewModel();
        DataContext = _vm;
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        _ = _vm.ShutdownAsync();
        base.OnClosing(e);
    }
}