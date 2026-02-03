using System;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using WeatherTcpServer.Dashboard.Infrastructure;
using WeatherTcpServer.Dashboard.Models;
using WeatherTcpServer.Dashboard.Services;

namespace WeatherTcpServer.Dashboard.ViewModels;

public sealed class MainViewModel : ObservableObject, IDisposable
{
    private readonly TcpJsonServer _server = new();
    private readonly WeatherApiService _apiService;
    private string _ipAddress = "0.0.0.0";
    private int _port = 46800;
    private bool _isRunning;
    private WeatherReading? _weather;
    private HeatIndexReading? _heatIndex;
    private HydroReading? _hydro;
    private GasReading? _gas;
    private int? _selectedDeviceId;
    private bool _disposed;

    public MainViewModel()
    {
        _apiService = new WeatherApiService();
        Logs = new ObservableCollection<string>();
        ConnectedClients = new ObservableCollection<ClientDisplay>();
        DeviceIds = new ObservableCollection<int?> { null }; // null means "All Devices"
        StartCommand = new RelayCommand(StartServer, () => !IsRunning);
        StopCommand = new RelayCommand(() => _ = StopServerAsync(), () => IsRunning);

        _server.Log += msg => AddLog(msg);
        _server.ClientsChanged += () => UpdateClientsList();

        _server.WeatherReceived += async model =>
        {
            var app = Application.Current;
            if (app?.Dispatcher != null)
            {
                app.Dispatcher.Invoke(() =>
                {
                    AddDeviceIdIfNew(model.DeviceId);
                    if (_selectedDeviceId == null || _selectedDeviceId == model.DeviceId)
                    {
                        Weather = model;
                    }
                });
            }
            await PostWeatherReadingAsync(model);
        };

        _server.HeatIndexReceived += async model =>
        {
            var app = Application.Current;
            if (app?.Dispatcher != null)
            {
                app.Dispatcher.Invoke(() =>
                {
                    AddDeviceIdIfNew(model.DeviceId);
                    if (_selectedDeviceId == null || _selectedDeviceId == model.DeviceId)
                    {
                        HeatIndex = model;
                    }
                });
            }
            await PostHeatIndexReadingAsync(model);
        };

        _server.HydroReceived += async model =>
        {
            var app = Application.Current;
            if (app?.Dispatcher != null)
            {
                app.Dispatcher.Invoke(() =>
                {
                    AddDeviceIdIfNew(model.DeviceId);
                    if (_selectedDeviceId == null || _selectedDeviceId == model.DeviceId)
                    {
                        Hydro = model;
                    }
                });
            }
            await PostHydroReadingAsync(model);
        };

        _server.GasReceived += async model =>
        {
            var app = Application.Current;
            if (app?.Dispatcher != null)
            {
                app.Dispatcher.Invoke(() =>
                {
                    AddDeviceIdIfNew(model.DeviceId);
                    if (_selectedDeviceId == null || _selectedDeviceId == model.DeviceId)
                    {
                        Gas = model;
                    }
                });
            }
            await PostGasReadingAsync(model);
        };

        AddLog("ViewModel initialized");
        AddLog($"API Base URL: {_apiService.GetType().Name} created");
    }

    public class ClientDisplay
    {
        public string Address { get; set; } = string.Empty;
        public DateTime ConnectedAt { get; set; }
        public int? DeviceId { get; set; }
        public string ConnectedTime => (DateTime.Now - ConnectedAt).ToString(@"hh\:mm\:ss");
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

    public WeatherReading? Weather
    {
        get => _weather;
        private set => SetProperty(ref _weather, value);
    }

    public HeatIndexReading? HeatIndex
    {
        get => _heatIndex;
        private set => SetProperty(ref _heatIndex, value);
    }

    public HydroReading? Hydro
    {
        get => _hydro;
        private set => SetProperty(ref _hydro, value);
    }

    public GasReading? Gas
    {
        get => _gas;
        private set => SetProperty(ref _gas, value);
    }

    public int? SelectedDeviceId
    {
        get => _selectedDeviceId;
        set => SetProperty(ref _selectedDeviceId, value);
    }

    public ObservableCollection<string> Logs { get; }
    public ObservableCollection<ClientDisplay> ConnectedClients { get; }
    public ObservableCollection<int?> DeviceIds { get; }

    public RelayCommand StartCommand { get; }
    public RelayCommand StopCommand { get; }

    private void UpdateClientsList()
    {
        var app = Application.Current;
        if (app?.Dispatcher != null)
        {
            app.Dispatcher.Invoke(() =>
            {
                var clients = _server.GetConnectedClients();
                ConnectedClients.Clear();
                foreach (var kvp in clients)
                {
                    ConnectedClients.Add(new ClientDisplay
                    {
                        Address = kvp.Value.Address,
                        ConnectedAt = kvp.Value.ConnectedAt,
                        DeviceId = kvp.Value.LastDeviceId
                    });
                }
            });
        }
    }

    private void AddDeviceIdIfNew(int deviceId)
    {
        if (!DeviceIds.Contains(deviceId))
        {
            DeviceIds.Add(deviceId);
        }
    }

    private void AddLog(string message)
    {
        var app = Application.Current;
        if (app?.Dispatcher != null)
        {
            app.Dispatcher.Invoke(() =>
                Logs.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {message}"));
        }
    }

    private async Task PostWeatherReadingAsync(WeatherReading reading)
    {
        AddLog($"[DEBUG] Attempting to post weather reading to API...");
        AddLog($"[DEBUG] Weather data - Temp: {reading.Temperature}, Humidity: {reading.Humidity}, DeviceId: {reading.DeviceId}");

        try
        {
            var success = await _apiService.PostWeatherReadingAsync(reading);
            if (success)
            {
                AddLog($"[OK] Weather reading posted (Temp: {reading.Temperature}�C, Humidity: {reading.Humidity}%)");
            }
            else
            {
                AddLog("[FAIL] Failed to post weather reading - API returned non-success status");
            }
        }
        catch (HttpRequestException httpEx)
        {
            AddLog($"[ERROR] HTTP error posting weather: {httpEx.Message}");
            AddLog($"[DEBUG] Status Code: {httpEx.StatusCode}");
        }
        catch (TaskCanceledException)
        {
            AddLog($"[ERROR] Request timeout posting weather (>10s)");
        }
        catch (Exception ex)
        {
            AddLog($"[ERROR] Error posting weather: {ex.GetType().Name} - {ex.Message}");
            if (ex.InnerException != null)
            {
                AddLog($"[DEBUG] Inner exception: {ex.InnerException.Message}");
            }
        }
    }

    private async Task PostHeatIndexReadingAsync(HeatIndexReading reading)
    {
        AddLog($"[DEBUG] Attempting to post heat index reading to API...");
        AddLog($"[DEBUG] Heat index data - Value: {reading.HeatIndex}, DeviceId: {reading.DeviceId}");

        try
        {
            var success = await _apiService.PostHeatIndexReadingAsync(reading);
            if (success)
            {
                AddLog($"[OK] Heat index reading posted (Value: {reading.HeatIndex}�C)");
            }
            else
            {
                AddLog("[FAIL] Failed to post heat index reading - API returned non-success status");
            }
        }
        catch (HttpRequestException httpEx)
        {
            AddLog($"[ERROR] HTTP error posting heat index: {httpEx.Message}");
            AddLog($"[DEBUG] Status Code: {httpEx.StatusCode}");
        }
        catch (TaskCanceledException)
        {
            AddLog($"[ERROR] Request timeout posting heat index (>10s)");
        }
        catch (Exception ex)
        {
            AddLog($"[ERROR] Error posting heat index: {ex.GetType().Name} - {ex.Message}");
            if (ex.InnerException != null)
            {
                AddLog($"[DEBUG] Inner exception: {ex.InnerException.Message}");
            }
        }
    }

    private async Task PostHydroReadingAsync(HydroReading reading)
    {
        AddLog($"[DEBUG] Attempting to post hydro reading to API...");
        AddLog($"[DEBUG] Hydro data - WaterLevel: {reading.WaterLevel}, DeviceId: {reading.DeviceId}");

        try
        {
            var success = await _apiService.PostHydroReadingAsync(reading);
            if (success)
            {
                AddLog($"[OK] Hydro reading posted (WaterLevel: {reading.WaterLevel} hPa)");
            }
            else
            {
                AddLog("[FAIL] Failed to post hydro reading - API returned non-success status");
            }
        }
        catch (HttpRequestException httpEx)
        {
            AddLog($"[ERROR] HTTP error posting hydro: {httpEx.Message}");
            AddLog($"[DEBUG] Status Code: {httpEx.StatusCode}");
        }
        catch (TaskCanceledException)
        {
            AddLog($"[ERROR] Request timeout posting hydro (>10s)");
        }
        catch (Exception ex)
        {
            AddLog($"[ERROR] Error posting hydro: {ex.GetType().Name} - {ex.Message}");
            if (ex.InnerException != null)
            {
                AddLog($"[DEBUG] Inner exception: {ex.InnerException.Message}");
            }
        }
    }

    private async Task PostGasReadingAsync(GasReading reading)
    {
        AddLog($"[DEBUG] Attempting to post gas reading to API...");
        AddLog($"[DEBUG] Gas data - GasDetected: {reading.GasDetected}, DeviceId: {reading.DeviceId}");

        try
        {
            var success = await _apiService.PostGasReadingAsync(reading);
            if (success)
            {
                AddLog($"[OK] Gas reading posted (GasDetected: {reading.GasDetected})");
            }
            else
            {
                AddLog("[FAIL] Failed to post gas reading - API returned non-success status");
            }
        }
        catch (HttpRequestException httpEx)
        {
            AddLog($"[ERROR] HTTP error posting gas: {httpEx.Message}");
            AddLog($"[DEBUG] Status Code: {httpEx.StatusCode}");
        }
        catch (TaskCanceledException)
        {
            AddLog($"[ERROR] Request timeout posting gas (>10s)");
        }
        catch (Exception ex)
        {
            AddLog($"[ERROR] Error posting gas: {ex.GetType().Name} - {ex.Message}");
            if (ex.InnerException != null)
            {
                AddLog($"[DEBUG] Inner exception: {ex.InnerException.Message}");
            }
        }
    }

    public async Task ShutdownAsync()
    {
        await StopServerAsync().ConfigureAwait(false);
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
            var app = Application.Current;
            if (app?.Dispatcher != null)
            {
                app.Dispatcher.Invoke(() => IsRunning = false);
            }
            else
            {
                IsRunning = false;
            }
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _apiService?.Dispose();
            _disposed = true;
            AddLog("[DEBUG] ViewModel disposed");
        }
    }
}