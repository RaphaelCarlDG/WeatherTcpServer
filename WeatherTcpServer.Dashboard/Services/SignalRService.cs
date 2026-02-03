using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using WeatherTcpServer.Dashboard.Models;

namespace WeatherTcpServer.Dashboard.Services
{
    public sealed class SignalRService : IDisposable
    {
        private HubConnection? _hubConnection;
        private readonly string _hubUrl;
        private bool _disposed;

        public event Action<string>? Log;

        public SignalRService(string hubUrl)
        {
            _hubUrl = hubUrl ?? throw new ArgumentNullException(nameof(hubUrl));
            Console.WriteLine($"[SignalRService] Initialized with hub URL: {hubUrl}");
        }

        public async Task StartAsync()
        {
            try
            {
                _hubConnection = new HubConnectionBuilder()
                    .WithUrl(_hubUrl)
                    .WithAutomaticReconnect()
                    .Build();

                _hubConnection.Reconnecting += error =>
                {
                    Log?.Invoke("[SignalR] Reconnecting...");
                    return Task.CompletedTask;
                };

                _hubConnection.Reconnected += connectionId =>
                {
                    Log?.Invoke($"[SignalR] Reconnected - Connection ID: {connectionId}");
                    return Task.CompletedTask;
                };

                _hubConnection.Closed += error =>
                {
                    Log?.Invoke($"[SignalR] Connection closed: {error?.Message}");
                    return Task.CompletedTask;
                };

                await _hubConnection.StartAsync();
                Log?.Invoke("[SignalR] Connected successfully");
            }
            catch (Exception ex)
            {
                Log?.Invoke($"[SignalR] Error connecting: {ex.Message}");
                throw;
            }
        }

        public async Task StopAsync()
        {
            if (_hubConnection != null)
            {
                try
                {
                    await _hubConnection.StopAsync();
                    Log?.Invoke("[SignalR] Disconnected");
                }
                catch (Exception ex)
                {
                    Log?.Invoke($"[SignalR] Error disconnecting: {ex.Message}");
                }
            }
        }

        public async Task BroadcastWeatherAsync(Weather data)
        {
            if (_hubConnection?.State == HubConnectionState.Connected)
            {
                try
                {
                    await _hubConnection.InvokeAsync("BroadcastBme", data);
                    Console.WriteLine($"[SignalR] Weather data broadcasted");
                }
                catch (Exception ex)
                {
                    Log?.Invoke($"[SignalR] Error broadcasting weather: {ex.Message}");
                }
            }
        }

        public async Task BroadcastHeatIndexAsync(PerceivedWeather data)
        {
            if (_hubConnection?.State == HubConnectionState.Connected)
            {
                try
                {
                    await _hubConnection.InvokeAsync("BroadcastHeatIndex", data);
                    Console.WriteLine($"[SignalR] Heat index data broadcasted");
                }
                catch (Exception ex)
                {
                    Log?.Invoke($"[SignalR] Error broadcasting heat index: {ex.Message}");
                }
            }
        }

        public async Task BroadcastHydroAsync(Hydro data)
        {
            if (_hubConnection?.State == HubConnectionState.Connected)
            {
                try
                {
                    await _hubConnection.InvokeAsync("BroadcastHydroFromController", data);
                    Console.WriteLine($"[SignalR] Hydro data broadcasted");
                }
                catch (Exception ex)
                {
                    Log?.Invoke($"[SignalR] Error broadcasting hydro: {ex.Message}");
                }
            }
        }

        public async Task BroadcastGasAsync(Gas data)
        {
            if (_hubConnection?.State == HubConnectionState.Connected)
            {
                try
                {
                    await _hubConnection.InvokeAsync("BroadcastMq2", data);
                    Console.WriteLine($"[SignalR] Gas data broadcasted");
                }
                catch (Exception ex)
                {
                    Log?.Invoke($"[SignalR] Error broadcasting gas: {ex.Message}");
                }
            }
        }

        public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;

        public void Dispose()
        {
            if (!_disposed)
            {
                _hubConnection?.DisposeAsync().AsTask().Wait();
                Console.WriteLine("[SignalRService] Disposed");
                _disposed = true;
            }
        }
    }
}