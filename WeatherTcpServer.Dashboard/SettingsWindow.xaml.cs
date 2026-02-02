using System.Windows;
using WeatherTcpServer.Dashboard.Services;
using WeatherTcpServer.Dashboard.ViewModels;

namespace WeatherTcpServer.Dashboard;

public partial class SettingsWindow : Window
{
    private readonly MainViewModel _viewModel;

    public SettingsWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        
        // Listen for theme changes
        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.IsDarkMode))
            {
                ThemeManager.ApplyTheme(_viewModel.IsDarkMode);
            }
        };
        
        // Apply current theme
        ThemeManager.ApplyTheme(_viewModel.IsDarkMode);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}