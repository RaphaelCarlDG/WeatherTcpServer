using System;
using System.Collections.ObjectModel;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using WeatherTcpServer.Dashboard.Infrastructure;
using WeatherTcpServer.Dashboard.Models;
using WeatherTcpServer.Dashboard.Services;

namespace WeatherTcpServer.Dashboard.ViewModels;

public sealed class MainViewModel : ObservableObject, IDisposable
{
    private readonly TcpJsonServer _server = new();
    private readonly DbService _dbService;
    private readonly SignalRService _signalRService;
    private string _ipAddress = "0.0.0.0";
    private int _port = 46800;
    private bool _isRunning;
    private Weather? _weather;
    private PerceivedWeather? _heatIndex;
    private Hydro? _hydro;
    private Gas? _gas;
    private bool _disposed;

    public MainViewModel()
    {
        // Read connection string from appsettings.json
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        string connectionString = configuration.GetConnectionString("AppDb")
            ?? throw new InvalidOperationException("Connection string 'AppDb' not found in appsettings.json");

        string signalRUrl = configuration["SignalR:HubUrl"]
            ?? "http://localhost:44363/signalr";

        _dbService = new DbService(connectionString);
        _signalRService = new SignalRService(signalRUrl);

        Logs = new ObservableCollection<string>();
        StartCommand = new RelayCommand(StartServer, () => !IsRunning);
        StopCommand = new RelayCommand(() => _ = StopServerAsync(), () => IsRunning);

        _server.Log += msg => AddLog(msg);
        _signalRService.Log += msg => AddLog(msg);

        _server.WeatherReceived += async model =>
        {
            Application.Current.Dispatcher.Invoke(() => Weather = model);
            await SaveWeatherReadingAsync(model);
            await BroadcastWeatherAsync(model);
        };

        _server.HeatIndexReceived += async model =>
        {
            Application.Current.Dispatcher.Invoke(() => HeatIndex = model);
            await SaveHeatIndexReadingAsync(model);
            await BroadcastHeatIndexAsync(model);
        };

        _server.HydroReceived += async model =>
        {
            Application.Current.Dispatcher.Invoke(() => Hydro = model);
            await SaveHydroReadingAsync(model);
            await BroadcastHydroAsync(model);
        };

        _server.GasReceived += async model =>
        {
            Application.Current.Dispatcher.Invoke(() => Gas = model);
            await SaveGasReadingAsync(model);
            await BroadcastGasAsync(model);
        };

        AddLog("ViewModel initialized");
        AddLog($"Database service created - using direct DB connection");

        // Start SignalR connection
        _ = InitializeSignalRAsync();
    }

    private async Task InitializeSignalRAsync()
    {
        try
        {
            await _signalRService.StartAsync();
        }
        catch (Exception ex)
        {
            AddLog($"[WARNING] SignalR connection failed: {ex.Message}");
            AddLog("[INFO] Application will continue without real-time broadcasting");
        }
    }

    public string IpAddress
    {
        get => _ipAddress;
        set => SetProperty(ref _ipAddress, value);
    }

    public int Port
    {
        get => _port;
        set => SetProperty(ref _port, value);
    }

    public bool IsRunning
    {
        get => _isRunning;
        private set
        {
            if (SetProperty(ref _isRunning, value))
            {
                StartCommand.RaiseCanExecuteChanged();
                StopCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public Weather? Weather
    {
        get => _weather;
        private set => SetProperty(ref _weather, value);
    }

    public PerceivedWeather? HeatIndex
    {
        get => _heatIndex;
        private set => SetProperty(ref _heatIndex, value);
    }

    public Hydro? Hydro
    {
        get => _hydro;
        private set => SetProperty(ref _hydro, value);
    }

    public Gas? Gas
    {
        get => _gas;
        private set => SetProperty(ref _gas, value);
    }

    public ObservableCollection<string> Logs { get; }

    public RelayCommand StartCommand { get; }
    public RelayCommand StopCommand { get; }

    private void AddLog(string message)
    {
        Application.Current.Dispatcher.Invoke(() =>
            Logs.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {message}"));
    }

    private async Task SaveWeatherReadingAsync(Weather reading)
    {
        AddLog($"[DEBUG] Attempting to save weather reading to database...");
        AddLog($"[DEBUG] Weather data - Temp: {reading.Temperature}, Humidity: {reading.Humidity}, DeviceId: {reading.DeviceId}");

        try
        {
            var success = await _dbService.SaveWeatherReadingAsync(reading);
            if (success)
            {
                AddLog($"[OK] Weather reading saved (Temp: {reading.Temperature}°C, Humidity: {reading.Humidity}%)");
            }
            else
            {
                AddLog("[FAIL] Failed to save weather reading - no rows affected");
            }
        }
        catch (SqlException sqlEx)
        {
            AddLog($"[ERROR] SQL error saving weather: {sqlEx.Message}");
            AddLog($"[DEBUG] SQL Error Number: {sqlEx.Number}");
        }
        catch (TaskCanceledException)
        {
            AddLog($"[ERROR] Request timeout saving weather");
        }
        catch (Exception ex)
        {
            AddLog($"[ERROR] Error saving weather: {ex.GetType().Name} - {ex.Message}");
            if (ex.InnerException != null)
            {
                AddLog($"[DEBUG] Inner exception: {ex.InnerException.Message}");
            }
        }
    }

    private async Task BroadcastWeatherAsync(Weather reading)
    {
        try
        {
            if (_signalRService.IsConnected)
            {
                await _signalRService.BroadcastWeatherAsync(reading);
                AddLog($"[SignalR] Weather data broadcasted");
            }
        }
        catch (Exception ex)
        {
            AddLog($"[SignalR] Broadcast error: {ex.Message}");
        }
    }

    private async Task SaveHeatIndexReadingAsync(PerceivedWeather reading)
    {
        AddLog($"[DEBUG] Attempting to save heat index reading to database...");
        AddLog($"[DEBUG] Heat index data - Value: {reading.HeatIndex}, DeviceId: {reading.DeviceId}");

        try
        {
            var success = await _dbService.SaveHeatIndexReadingAsync(reading);
            if (success)
            {
                AddLog($"[OK] Heat index reading saved (Value: {reading.HeatIndex}°C)");
            }
            else
            {
                AddLog("[FAIL] Failed to save heat index reading - no rows affected");
            }
        }
        catch (SqlException sqlEx)
        {
            AddLog($"[ERROR] SQL error saving heat index: {sqlEx.Message}");
            AddLog($"[DEBUG] SQL Error Number: {sqlEx.Number}");
        }
        catch (TaskCanceledException)
        {
            AddLog($"[ERROR] Request timeout saving heat index");
        }
        catch (Exception ex)
        {
            AddLog($"[ERROR] Error saving heat index: {ex.GetType().Name} - {ex.Message}");
            if (ex.InnerException != null)
            {
                AddLog($"[DEBUG] Inner exception: {ex.InnerException.Message}");
            }
        }
    }

    private async Task BroadcastHeatIndexAsync(PerceivedWeather reading)
    {
        try
        {
            if (_signalRService.IsConnected)
            {
                await _signalRService.BroadcastHeatIndexAsync(reading);
                AddLog($"[SignalR] Heat index data broadcasted");
            }
        }
        catch (Exception ex)
        {
            AddLog($"[SignalR] Broadcast error: {ex.Message}");
        }
    }

    private async Task SaveHydroReadingAsync(Hydro reading)
    {
        AddLog($"[DEBUG] Attempting to save hydro reading to database...");
        AddLog($"[DEBUG] Hydro data - WaterLevel: {reading.WaterLevel}, DeviceId: {reading.DeviceId}");

        try
        {
            var success = await _dbService.SaveHydroReadingAsync(reading);
            if (success)
            {
                AddLog($"[OK] Hydro reading saved (WaterLevel: {reading.WaterLevel} hPa)");
            }
            else
            {
                AddLog("[FAIL] Failed to save hydro reading - no rows affected");
            }
        }
        catch (SqlException sqlEx)
        {
            AddLog($"[ERROR] SQL error saving hydro: {sqlEx.Message}");
            AddLog($"[DEBUG] SQL Error Number: {sqlEx.Number}");
        }
        catch (TaskCanceledException)
        {
            AddLog($"[ERROR] Request timeout saving hydro");
        }
        catch (Exception ex)
        {
            AddLog($"[ERROR] Error saving hydro: {ex.GetType().Name} - {ex.Message}");
            if (ex.InnerException != null)
            {
                AddLog($"[DEBUG] Inner exception: {ex.InnerException.Message}");
            }
        }
    }

    private async Task BroadcastHydroAsync(Hydro reading)
    {
        try
        {
            if (_signalRService.IsConnected)
            {
                await _signalRService.BroadcastHydroAsync(reading);
                AddLog($"[SignalR] Hydro data broadcasted");
            }
        }
        catch (Exception ex)
        {
            AddLog($"[SignalR] Broadcast error: {ex.Message}");
        }
    }

    private async Task SaveGasReadingAsync(Gas reading)
    {
        AddLog($"[DEBUG] Attempting to save gas reading to database...");
        AddLog($"[DEBUG] Gas data - GasDetected: {reading.GasDetected}, DeviceId: {reading.DeviceId}");

        try
        {
            var success = await _dbService.SaveGasReadingAsync(reading);
            if (success)
            {
                AddLog($"[OK] Gas reading saved (GasDetected: {reading.GasDetected})");
            }
            else
            {
                AddLog("[FAIL] Failed to save gas reading - no rows affected");
            }
        }
        catch (SqlException sqlEx)
        {
            AddLog($"[ERROR] SQL error saving gas: {sqlEx.Message}");
            AddLog($"[DEBUG] SQL Error Number: {sqlEx.Number}");
        }
        catch (TaskCanceledException)
        {
            AddLog($"[ERROR] Request timeout saving gas");
        }
        catch (Exception ex)
        {
            AddLog($"[ERROR] Error saving gas: {ex.GetType().Name} - {ex.Message}");
            if (ex.InnerException != null)
            {
                AddLog($"[DEBUG] Inner exception: {ex.InnerException.Message}");
            }
        }
    }

    private async Task BroadcastGasAsync(Gas reading)
    {
        try
        {
            if (_signalRService.IsConnected)
            {
                await _signalRService.BroadcastGasAsync(reading);
                AddLog($"[SignalR] Gas data broadcasted");
            }
        }
        catch (Exception ex)
        {
            AddLog($"[SignalR] Broadcast error: {ex.Message}");
        }
    }

    public async Task ShutdownAsync()
    {
        await StopServerAsync().ConfigureAwait(false);
        await _signalRService.StopAsync().ConfigureAwait(false);
    }

    private void StartServer()
    {
        if (!IPAddress.TryParse(IpAddress, out IPAddress? ip))
        {
            AddLog("[ERROR] Invalid IP address");
            return;
        }

        if (Port is < 1 or > 65535)
        {
            AddLog("[ERROR] Invalid port");
            return;
        }

        try
        {
            _server.Start(ip, Port);
            IsRunning = true;
            AddLog($"[OK] Server started on {IpAddress}:{Port}");
        }
        catch (Exception ex)
        {
            AddLog($"[ERROR] Start error: {ex.Message}");
            IsRunning = false;
        }
    }

    private async Task StopServerAsync()
    {
        try
        {
            await _server.StopAsync().ConfigureAwait(false);
            AddLog("[OK] Server stopped");
        }
        catch (Exception ex)
        {
            AddLog($"[ERROR] Stop error: {ex.Message}");
        }
        finally
        {
            Application.Current.Dispatcher.Invoke(() => IsRunning = false);
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _dbService?.Dispose();
            _signalRService?.Dispose();
            _disposed = true;
            AddLog("[DEBUG] ViewModel disposed");
        }
    }
}