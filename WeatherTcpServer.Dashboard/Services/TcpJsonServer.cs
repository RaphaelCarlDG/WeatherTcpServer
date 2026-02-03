using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using WeatherTcpServer.Dashboard.Models;

namespace WeatherTcpServer.Dashboard.Services;

public sealed class TcpJsonServer : IDisposable
{
    private TcpListener? _listener;
    private CancellationTokenSource? _cts;
    private Task? _acceptLoop;
    private readonly HashSet<Task> _clientTasks = new HashSet<Task>();
    private readonly object _clientTasksLock = new object();

    public bool IsRunning { get; private set; }

    public event Action<string>? Log;
    public event Action<Weather>? WeatherReceived;
    public event Action<PerceivedWeather>? HeatIndexReceived;
    public event Action<Hydro>? HydroReceived;
    public event Action<Gas>? GasReceived;

    private const string PrefixWater = "water_level:";
    private const string PrefixMq2 = "mq2:";
    private const string PrefixBme = "bme280:";

    public void Start(IPAddress ip, int port)
    {
        if (IsRunning)
        {
            return;
        }

        _cts = new CancellationTokenSource();
        _listener = new TcpListener(ip, port);
        _listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        _listener.Start();
        IsRunning = true;

        Log?.Invoke($"Server started: {ip}:{port}");

        _acceptLoop = AcceptLoopAsync(_listener, _cts.Token);
    }

    public async Task StopAsync()
    {
        if (!IsRunning)
        {
            return;
        }

        try
        {
            _cts?.Cancel();
            _listener?.Stop();
        }
        catch { }

        if (_acceptLoop is not null)
        {
            try
            {
                await _acceptLoop.ConfigureAwait(false);
            }
            catch { }
        }

        IsRunning = false;
        Log?.Invoke("Server stopped");
    }

    private async Task AcceptLoopAsync(TcpListener listener, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            TcpClient? client = null;
            try
            {
                client = await listener.AcceptTcpClientAsync().ConfigureAwait(false);

                var clientTask = HandleClientAsync(client, ct);

                lock (_clientTasksLock)
                {
                    _clientTasks.Add(clientTask);
                }

                _ = clientTask.ContinueWith(t =>
                {
                    lock (_clientTasksLock)
                    {
                        _clientTasks.Remove(t);
                    }
                }, TaskScheduler.Default);
            }
            catch (OperationCanceledException)
            {
                client?.Dispose();
                break;
            }
            catch (ObjectDisposedException)
            {
                client?.Dispose();
                break;
            }
            catch (Exception ex)
            {
                client?.Dispose();
                Log?.Invoke($"Accept error: {ex.Message}");
                await Task.Delay(200, ct).ConfigureAwait(false);
            }
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken ct)
    {
        string remote = client.Client.RemoteEndPoint?.ToString() ?? "unknown";
        Log?.Invoke($"Client connected: {remote}");

        try
        {
            await using NetworkStream ns = client.GetStream();
            using var reader = new StreamReader(ns, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 4096, leaveOpen: true);

            while (!ct.IsCancellationRequested)
            {
                string? line = await reader.ReadLineAsync().ConfigureAwait(false);

                if (line is null)
                {
                    break;
                }

                line = line.Trim();
                if (line.Length == 0)
                {
                    continue;
                }

                Log?.Invoke($"Received from {remote}: {line.Substring(0, Math.Min(100, line.Length))}...");
                TryProcessLine(line, remote);
            }
        }
        catch (OperationCanceledException) { }
        catch (IOException) { }
        catch (Exception ex)
        {
            Log?.Invoke($"Client error ({remote}): {ex.Message}");
        }
        finally
        {
            try { client.Close(); } catch { }
            Log?.Invoke($"Client disconnected: {remote}");
        }
    }

    /// <summary>
    /// Strips the prefix from a line and returns the JSON portion.
    /// Returns null if the line does not start with the expected prefix.
    /// </summary>
    private static string? StripPrefix(string line, string prefix)
    {
        if (!line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return null;

        return line.Substring(prefix.Length);
    }

    /// <summary>
    /// Routes a prefixed line to the correct handler, mirroring the Python
    /// script's if/elif chain exactly:
    ///   water_level:{...}  →  HydroReceived
    ///   mq2:{...}          →  GasReceived
    ///   bme280:{...}       →  WeatherReceived  +  (optionally) HeatIndexReceived
    /// </summary>
    private void TryProcessLine(string line, string clientAddress)
    {
        try
        {
            string? json;

            // water_level
            if ((json = StripPrefix(line, PrefixWater)) is not null)
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var reading = new Hydro
                {
                    DeviceId = (int)root.GetProperty("DeviceId").GetInt64(),
                    WaterLevel = (int)root.GetProperty("WaterLevel").GetInt64()
                };

                Log?.Invoke($"Hydro reading parsed: Level={reading.WaterLevel}, DeviceId={reading.DeviceId}");
                HydroReceived?.Invoke(reading);
                return;
            }

            // mq2
            if ((json = StripPrefix(line, PrefixMq2)) is not null)
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var reading = new Gas
                {
                    DeviceId = (int)root.GetProperty("DeviceId").GetInt64(),
                    GasDetected = root.GetProperty("GasDetected").GetBoolean()
                };

                Log?.Invoke($"Gas reading parsed: Detected={reading.GasDetected}, DeviceId={reading.DeviceId}");
                GasReceived?.Invoke(reading);
                return;
            }

            // bme280
            // This single payload fans out to WeatherReceived AND, when the
            // optional HeatIndex field is present, also to HeatIndexReceived.
            // This matches exactly what the Python script does inside its
            // "bme280:" branch.
            if ((json = StripPrefix(line, PrefixBme)) is not null)
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                int deviceId = (int)root.GetProperty("DeviceId").GetInt64();

                // Always emit the weather reading
                var weather = new Weather
                {
                    DeviceId = deviceId,
                    Temperature = (decimal)root.GetProperty("Temperature").GetDouble(),
                    Pressure = (decimal)root.GetProperty("Pressure").GetDouble(),
                    Humidity = (decimal)root.GetProperty("Humidity").GetDouble(),
                    Altitude = (decimal)root.GetProperty("Altitude").GetDouble(),
                    DewPoint = (decimal)root.GetProperty("DewPoint").GetDouble()
                };

                Log?.Invoke($"Weather reading parsed: Temp={weather.Temperature}C, DeviceId={weather.DeviceId}");
                WeatherReceived?.Invoke(weather);

                // Conditionally emit the heat-index reading (same guard as Python)
                if (root.TryGetProperty("HeatIndex", out var heatIndexElement))
                {
                    var heatIndex = new PerceivedWeather
                    {
                        DeviceId = deviceId,
                        HeatIndex = heatIndexElement.GetDouble()
                    };

                    Log?.Invoke($"HeatIndex reading parsed: Value={heatIndex.HeatIndex}, DeviceId={heatIndex.DeviceId}");
                    HeatIndexReceived?.Invoke(heatIndex);
                }

                return;
            }

            Log?.Invoke($"Unknown message dropped from {clientAddress}: {line.Substring(0, Math.Min(80, line.Length))}");
        }
        catch (JsonException jsonEx)
        {
            Log?.Invoke($"Invalid JSON from {clientAddress}: {jsonEx.Message}");
        }
        catch (KeyNotFoundException)
        {
            Log?.Invoke($"Missing required field in payload from {clientAddress}: {line.Substring(0, Math.Min(80, line.Length))}");
        }
        catch (Exception ex)
        {
            Log?.Invoke($"Processing error from {clientAddress}: {ex.GetType().Name} - {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (IsRunning)
        {
            StopAsync().GetAwaiter().GetResult();
        }

        _cts?.Dispose();
        _listener?.Stop();
    }
}