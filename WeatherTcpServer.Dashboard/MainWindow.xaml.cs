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

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        var settingsWindow = new SettingsWindow(_vm)
        {
            Owner = this
        };
        settingsWindow.ShowDialog();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        _ = _vm.ShutdownAsync();
        base.OnClosing(e);
    }
}